using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Enrollment.Api.Infrastructure;
using CampusHub.Enrollment.Api.Sagas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Features;

/// <summary>SDD CH-S19 / specs/019-enroll-checkout. Enroll saga (mock pay) for the signed-in student.</summary>
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
        api.MapGet("/internal/stats", InternalStats).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Start(
        StartEnrollmentRequest request,
        EnrollmentSaga saga,
        EnrollmentDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var token = http.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var (id, email, name) = Caller(http.User);
        var tenantId = Tenancy.TenantId(http.User);
        var plan = Tenancy.Plan(http.User);
        var cap = Plans.SeatCap(plan);
        if (cap < int.MaxValue)
        {
            var alreadySeated = await db.Enrollments.AnyAsync(
                e => e.TenantId == tenantId
                     && e.StudentId == id
                     && e.Status != CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Rejected
                     && e.Status != CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Compensated,
                ct);
            if (!alreadySeated)
            {
                var used = await db.Enrollments
                    .Where(e => e.TenantId == tenantId
                                && e.Status != CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Rejected
                                && e.Status != CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Compensated)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync(ct);
                if (used >= cap)
                {
                    return Results.Conflict(new
                    {
                        error = $"This campus plan allows {cap} seats. Upgrade to add more students."
                    });
                }
            }
        }

        var enrollment = await saga.StartAsync(
            request.CourseId,
            id,
            email,
            name,
            tenantId,
            token,
            string.IsNullOrWhiteSpace(request.SimulatePayment) ? "Succeeded" : request.SimulatePayment,
            ct);

        return Results.Accepted($"/api/enrollments/{enrollment.Id}", ToDto(enrollment));
    }

    private static async Task<IResult> Mine(EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var tenantId = Tenancy.TenantId(user);
        var items = (await db.Enrollments.AsNoTracking()
                .Where(e => e.TenantId == tenantId && (e.StudentId == id || e.StudentEmail == email))
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

        var enrollment = await db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.CourseId == courseId
                     && e.StudentId == studentId
                     && e.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Confirmed,
                ct);
        return Results.Ok(new { confirmed = enrollment is not null, enrollmentId = enrollment?.Id });
    }

    private static async Task<IResult> ListAll(EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var tenantId = Tenancy.TenantId(user);
        var items = (await db.Enrollments.AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .ToListAsync(ct))
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
        if (enrollment.TenantId != Tenancy.TenantId(user))
        {
            return Results.NotFound();
        }

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

    private static async Task<IResult> InternalStats(
        Guid courseId,
        HttpContext http,
        IConfiguration config,
        EnrollmentDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var rows = await db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .ToListAsync(ct);

        var confirmed = rows.Where(e => e.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Confirmed).ToList();
        var total = rows.Count;
        var confirmedCount = confirmed.Count;
        var revenue = confirmed.Sum(e => e.Amount);
        var cancelledCount = rows.Count(e =>
            e.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Compensated ||
            e.Status == CampusHub.Enrollment.Api.Domain.EnrollmentStatus.Rejected);

        // Monthly enrollment breakdown (last 6 months)
        var now = DateTimeOffset.UtcNow;
        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-i))
            .Select(d => new { Year = d.Year, Month = d.Month })
            .Reverse()
            .Select(m => new EnrollmentMonthDto(
                $"{m.Year}-{m.Month:D2}",
                rows.Count(e => e.CreatedAt.Year == m.Year && e.CreatedAt.Month == m.Month),
                confirmed.Where(e => e.CreatedAt.Year == m.Year && e.CreatedAt.Month == m.Month).Sum(e => e.Amount)))
            .ToList();

        return Results.Ok(new CourseEnrollmentStatsDto(
            courseId,
            total,
            confirmedCount,
            cancelledCount,
            revenue,
            months));
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

public sealed record EnrollmentMonthDto(string Month, int Count, decimal Revenue);
public sealed record CourseEnrollmentStatsDto(
    Guid CourseId,
    int TotalEnrollments,
    int ConfirmedEnrollments,
    int CancelledEnrollments,
    decimal TotalRevenue,
    IReadOnlyList<EnrollmentMonthDto> MonthlyBreakdown);

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
