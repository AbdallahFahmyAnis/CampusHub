using System.Security.Claims;
using CampusHub.Enrollment.Api.Infrastructure;
using CampusHub.Enrollment.Api.Sagas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Features;

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/enrollments");
        api.MapPost(string.Empty, Start).RequireAuthorization();
        api.MapGet("/mine", Mine).RequireAuthorization();
        api.MapGet("/mine/courses/{courseId:guid}", MineCourse).RequireAuthorization();
        api.MapGet("/internal/confirmed", InternalConfirmed).AllowAnonymous();
        api.MapGet(string.Empty, ListAll).RequireAuthorization(policy => policy.RequireRole(CampusHub.BuildingBlocks.Security.Roles.Administrator));
        api.MapGet("/{id:guid}", Get).RequireAuthorization();
        api.MapPost("/internal/payments/succeeded", PaymentSucceeded).AllowAnonymous();
        api.MapPost("/internal/payments/failed", PaymentFailed).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Start(
        StartEnrollmentRequest request,
        EnrollmentSaga saga,
        HttpContext http,
        CancellationToken ct)
    {
        var token = http.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var (id, email, name) = Caller(http.User);
        var enrollment = await saga.StartAsync(
            request.CourseId,
            id,
            email,
            name,
            token,
            string.IsNullOrWhiteSpace(request.SimulatePayment) ? "Succeeded" : request.SimulatePayment,
            ct);

        return Results.Accepted($"/api/enrollments/{enrollment.Id}", ToDto(enrollment));
    }

    private static async Task<IResult> Mine(EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var items = (await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentId == id || e.StudentEmail == email)
                .ToListAsync(ct))
            .OrderByDescending(e => e.CreatedAt)
            .Select(ToDto);
        return Results.Ok(items);
    }

    private static async Task<IResult> MineCourse(Guid courseId, EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var enrollment = await db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId && (e.StudentId == id || e.StudentEmail == email))
            .OrderByDescending(e => e.UpdatedAt)
            .FirstOrDefaultAsync(ct);
        return Results.Ok(new
        {
            confirmed = enrollment?.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Confirmed,
            status = enrollment?.Status
        });
    }

    private static async Task<IResult> InternalConfirmed(
        Guid courseId,
        string studentId,
        HttpContext http,
        IConfiguration config,
        EnrollmentDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var confirmed = await db.Enrollments.AsNoTracking().AnyAsync(
            e => e.CourseId == courseId
                 && e.StudentId == studentId
                 && e.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Confirmed,
            ct);
        return Results.Ok(new { confirmed });
    }

    private static async Task<IResult> ListAll(EnrollmentDbContext db, CancellationToken ct)
    {
        var items = (await db.Enrollments.AsNoTracking().ToListAsync(ct))
            .OrderByDescending(e => e.CreatedAt)
            .Take(200)
            .Select(ToDto);
        return Results.Ok(items);
    }

    private static async Task<IResult> Get(Guid id, EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null)
        {
            return Results.NotFound();
        }

        var (userId, email, _) = Caller(user);
        if (enrollment.StudentId != userId &&
            !string.Equals(enrollment.StudentEmail, email, StringComparison.OrdinalIgnoreCase) &&
            !user.IsInRole(CampusHub.BuildingBlocks.Security.Roles.Administrator))
        {
            return Results.Forbid();
        }

        return Results.Ok(ToDto(enrollment));
    }

    private static async Task<IResult> PaymentSucceeded(
        PaymentCallbackRequest request,
        HttpContext http,
        IConfiguration config,
        EnrollmentSaga saga,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        await saga.HandlePaymentSucceededAsync(request.EnrollmentId, request.PaymentId, ct);
        return Results.Ok();
    }

    private static async Task<IResult> PaymentFailed(
        PaymentCallbackRequest request,
        HttpContext http,
        IConfiguration config,
        EnrollmentSaga saga,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        await saga.HandlePaymentFailedAsync(request.EnrollmentId, request.PaymentId, request.Reason ?? "Payment failed", ct);
        return Results.Ok();
    }

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }

    private static EnrollmentDto ToDto(EnrollmentEntity e) =>
        new(e.Id, e.CourseId, e.CourseTitle, e.StudentName, e.StudentEmail, e.Amount, e.Status, e.PaymentId, e.FailureReason, e.CreatedAt, e.UpdatedAt);

    private static (string Id, string Email, string Name) Caller(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = user.FindFirstValue("email")
                    ?? user.FindFirstValue("preferred_username")
                    ?? string.Empty;
        var name = user.FindFirstValue("name") ?? user.Identity?.Name ?? email;
        return (id, email, name);
    }
}

public sealed record StartEnrollmentRequest(Guid CourseId, string? SimulatePayment);
public sealed record PaymentCallbackRequest(Guid EnrollmentId, Guid PaymentId, string? Reason);
public sealed record EnrollmentDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string StudentName,
    string StudentEmail,
    decimal Amount,
    string Status,
    Guid? PaymentId,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
