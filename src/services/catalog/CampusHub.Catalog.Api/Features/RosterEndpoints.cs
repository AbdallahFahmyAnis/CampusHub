using System.Security.Claims;
using CampusHub.BuildingBlocks.Sdd;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S24 — specs/024-course-roster. Confirmed enrollments from Enrollment service.</summary>
public static class RosterEndpoints
{
    public static RouteGroupBuilder MapRosterEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/roster", GetRoster).RequireAuthorization("CanManageCatalog");
        return api;
    }

    private static async Task<IResult> GetRoster(
        Guid id,
        CatalogDbContext db,
        EnrollmentGateway enrollment,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        var roster = await enrollment.GetRosterAsync(id, ct);
        if (roster is null)
        {
            return Results.Ok(new CourseRosterDto(course.Id, course.Title, 0, Array.Empty<RosterRowDto>()));
        }

        var ordered = EnrollmentRosterRules.OrderByEnrolledAt(
            roster.Confirmed,
            r => r.EnrolledAt,
            r => r.StudentName);

        var rows = ordered
            .Select(r => new RosterRowDto(r.StudentId, r.StudentName, r.StudentEmail, r.EnrolledAt))
            .ToList();

        return Results.Ok(new CourseRosterDto(course.Id, course.Title, rows.Count, rows));
    }
}
