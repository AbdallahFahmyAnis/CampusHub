using System.Security.Claims;
using CampusHub.Access.Api.Domain;
using CampusHub.Access.Api.Infrastructure;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Access.Api.Features;

/// <summary>SDD CH-S06 / MDP-17 — specs/008-certificates. Credentials, QR, completion certs.</summary>
public static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/access/events", Ingest).AllowAnonymous();
        app.MapGet("/api/access/credentials/mine", Mine).RequireAuthorization();
        app.MapGet("/api/access/credentials/{id:guid}/qr", Qr).RequireAuthorization();
        app.MapPost("/api/access/scans", Scan).RequireAuthorization(policy =>
            policy.RequireRole(Roles.Teacher, Roles.Administrator));
        app.MapGet("/api/access/scans", ListScans).RequireAuthorization(policy =>
            policy.RequireRole(Roles.Teacher, Roles.Administrator));
        return app;
    }

    private static async Task<IResult> Ingest(
        IntegrationEventDto envelope,
        HttpContext http,
        IConfiguration config,
        AccessEventProcessor processor,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        await processor.HandleAsync(envelope, ct);
        return Results.Accepted();
    }

    private static async Task<IResult> Mine(ClaimsPrincipal user, AccessDbContext db, CancellationToken ct)
    {
        var userId = UserId(user);
        var items = (await db.Credentials
                .AsNoTracking()
                .Where(c => c.StudentId == userId)
                .ToListAsync(ct))
            .OrderByDescending(c => c.IssuedAt)
            .Select(ToDto)
            .ToList();
        return Results.Ok(items);
    }

    private static async Task<IResult> Qr(
        Guid id,
        ClaimsPrincipal user,
        AccessDbContext db,
        CancellationToken ct)
    {
        var userId = UserId(user);
        var isStaff = user.IsInRole(Roles.Teacher) || user.IsInRole(Roles.Administrator);
        var credential = await db.Credentials.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (credential is null)
        {
            return Results.NotFound();
        }

        if (!isStaff && credential.StudentId != userId)
        {
            return Results.Forbid();
        }

        if (credential.Status != CredentialStatus.Active)
        {
            return Results.BadRequest(new { error = "Credential is not active." });
        }

        return Results.File(QrPng.FromText(credential.Token), "image/png");
    }

    private static async Task<IResult> Scan(
        ScanRequest request,
        ClaimsPrincipal user,
        AccessDbContext db,
        CredentialSigner signer,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            !signer.TryVerify(request.Token.Trim(), out var credentialId, out _, out _, out _))
        {
            return Results.BadRequest(new { error = "Invalid or expired pass." });
        }

        var credential = await db.Credentials.SingleOrDefaultAsync(c => c.Id == credentialId, ct);
        if (credential is null || credential.Status != CredentialStatus.Active)
        {
            return Results.BadRequest(new { error = "Pass is unknown or revoked." });
        }

        if (!string.Equals(credential.Token, request.Token.Trim(), StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "Pass does not match the issued credential." });
        }

        var scan = new AttendanceScan
        {
            Id = Guid.NewGuid(),
            CredentialId = credential.Id,
            EnrollmentId = credential.EnrollmentId,
            CourseId = credential.CourseId,
            StudentId = credential.StudentId,
            StudentName = credential.StudentName,
            CourseTitle = credential.CourseTitle,
            ScannedBy = user.Identity?.Name ?? UserId(user),
            ScannedAt = DateTimeOffset.UtcNow
        };
        db.Scans.Add(scan);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new ScanDto(
            scan.Id,
            scan.StudentName,
            scan.CourseTitle,
            scan.ScannedBy,
            scan.ScannedAt));
    }

    private static async Task<IResult> ListScans(AccessDbContext db, Guid? courseId, CancellationToken ct)
    {
        var query = db.Scans.AsNoTracking().AsQueryable();
        if (courseId is not null)
        {
            query = query.Where(s => s.CourseId == courseId);
        }

        var items = (await query.ToListAsync(ct))
            .OrderByDescending(s => s.ScannedAt)
            .Take(100)
            .Select(s => new ScanDto(s.Id, s.StudentName, s.CourseTitle, s.ScannedBy, s.ScannedAt))
            .ToList();
        return Results.Ok(items);
    }

    private static CredentialDto ToDto(AccessCredential c) =>
        new(c.Id, c.EnrollmentId, c.CourseId, c.CourseTitle, c.Kind, c.Status, c.Token, c.IssuedAt, c.ExpiresAt);

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}

public sealed record ScanRequest(string Token);

public sealed record CredentialDto(
    Guid Id,
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string Kind,
    string Status,
    string Token,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

public sealed record ScanDto(Guid Id, string StudentName, string CourseTitle, string ScannedBy, DateTimeOffset ScannedAt);
