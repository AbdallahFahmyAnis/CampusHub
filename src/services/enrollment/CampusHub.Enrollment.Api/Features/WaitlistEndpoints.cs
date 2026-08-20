using System.Security.Claims;
using CampusHub.BuildingBlocks.Sdd;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Enrollment.Api.Domain;
using CampusHub.Enrollment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Enrollment.Api.Features;

/// <summary>SDD CH-S23 — specs/023-course-waitlist. Join / leave / list waitlist.</summary>
public static class WaitlistEndpoints
{
    public static IEndpointRouteBuilder MapWaitlistEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/enrollments/waitlist").RequireAuthorization();
        api.MapGet("/mine", Mine);
        api.MapGet("/courses/{courseId:guid}", Status);
        api.MapPost("/courses/{courseId:guid}", Join);
        api.MapDelete("/courses/{courseId:guid}", Leave);
        return app;
    }

    private static async Task<IResult> Mine(EnrollmentDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var tenantId = Tenancy.TenantId(user);
        var rows = await db.CourseWaitlists.AsNoTracking()
            .Where(w => w.TenantId == tenantId && (w.StudentId == id || w.StudentEmail == email))
            .ToListAsync(ct);

        var result = new List<WaitlistEntryDto>();
        foreach (var row in rows.OrderBy(w => w.CreatedAt))
        {
            var queue = await QueueForCourse(db, row.CourseId, ct);
            var position = WaitlistRules.Position(queue, row.Id);
            result.Add(new WaitlistEntryDto(row.Id, row.CourseId, row.CourseTitle, position, queue.Count, row.CreatedAt));
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> Status(
        Guid courseId,
        EnrollmentDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var tenantId = Tenancy.TenantId(user);
        var mine = await db.CourseWaitlists.AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.TenantId == tenantId
                     && w.CourseId == courseId
                     && (w.StudentId == id || w.StudentEmail == email),
                ct);
        var queue = await QueueForCourse(db, courseId, ct);
        var position = mine is null ? (int?)null : WaitlistRules.Position(queue, mine.Id);
        return Results.Ok(new WaitlistStatusDto(mine is not null, position, queue.Count));
    }

    private static async Task<IResult> Join(
        Guid courseId,
        EnrollmentDbContext db,
        CatalogGateway catalog,
        ClaimsPrincipal user,
        HttpContext http,
        CancellationToken ct)
    {
        var token = http.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var (id, email, name) = Caller(user);
        var tenantId = Tenancy.TenantId(user);

        var course = await catalog.GetCourseAsync(courseId, token, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        var enrolled = await db.Enrollments.AsNoTracking().AnyAsync(
            e => e.CourseId == courseId
                 && e.StudentId == id
                 && e.Status == EnrollmentStatus.Confirmed,
            ct);
        var existing = await db.CourseWaitlists.AsNoTracking().FirstOrDefaultAsync(
            w => w.TenantId == tenantId && w.CourseId == courseId && w.StudentId == id,
            ct);

        var published = string.Equals(course.Status, "Published", StringComparison.OrdinalIgnoreCase);
        if (!WaitlistRules.CanJoin(published, course.RemainingSeats, enrolled, existing is not null))
        {
            if (existing is not null)
            {
                var q = await QueueForCourse(db, courseId, ct);
                return Results.Ok(new WaitlistEntryDto(
                    existing.Id, existing.CourseId, existing.CourseTitle,
                    WaitlistRules.Position(q, existing.Id), q.Count, existing.CreatedAt));
            }

            return Results.Conflict(new { error = "Waitlist is only for published courses with no remaining seats." });
        }

        var item = new CourseWaitlist
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CourseId = courseId,
            CourseTitle = course.Title,
            StudentId = id,
            StudentEmail = email,
            StudentName = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseWaitlists.Add(item);
        await db.SaveChangesAsync(ct);

        var queue = await QueueForCourse(db, courseId, ct);
        return Results.Created(
            $"/api/enrollments/waitlist/courses/{courseId}",
            new WaitlistEntryDto(item.Id, item.CourseId, item.CourseTitle, WaitlistRules.Position(queue, item.Id), queue.Count, item.CreatedAt));
    }

    private static async Task<IResult> Leave(
        Guid courseId,
        EnrollmentDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (id, email, _) = Caller(user);
        var tenantId = Tenancy.TenantId(user);
        var rows = await db.CourseWaitlists
            .Where(w => w.TenantId == tenantId
                        && w.CourseId == courseId
                        && (w.StudentId == id || w.StudentEmail == email))
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            return Results.Ok(new WaitlistStatusDto(false, null, await CountQueue(db, courseId, ct)));
        }

        db.CourseWaitlists.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new WaitlistStatusDto(false, null, await CountQueue(db, courseId, ct)));
    }

    private static async Task<List<(Guid Id, DateTimeOffset CreatedAt)>> QueueForCourse(
        EnrollmentDbContext db, Guid courseId, CancellationToken ct)
    {
        var rows = await db.CourseWaitlists.AsNoTracking()
            .Where(w => w.CourseId == courseId)
            .Select(w => new { w.Id, w.CreatedAt })
            .ToListAsync(ct);
        return rows.Select(w => (w.Id, w.CreatedAt)).ToList();
    }

    private static Task<int> CountQueue(EnrollmentDbContext db, Guid courseId, CancellationToken ct) =>
        db.CourseWaitlists.AsNoTracking().CountAsync(w => w.CourseId == courseId, ct);

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

public sealed record WaitlistEntryDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    int Position,
    int QueueLength,
    DateTimeOffset JoinedAt);

public sealed record WaitlistStatusDto(bool Waitlisted, int? Position, int QueueLength);
