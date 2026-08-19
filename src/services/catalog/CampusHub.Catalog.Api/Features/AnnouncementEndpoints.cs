using System.Security.Claims;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S14 / MDP-25 — specs/016-announcements. Course announcements.</summary>
public static class AnnouncementEndpoints
{
    public static RouteGroupBuilder MapAnnouncementEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/announcements", ListAnnouncements);
        api.MapPost("/courses/{id:guid}/announcements", CreateAnnouncement).RequireAuthorization("CanManageCatalog");
        return api;
    }

    private static async Task<IResult> ListAnnouncements(
        Guid id,
        CatalogDbContext db,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        List<CourseAnnouncement> rows;
        try
        {
            rows = await db.CourseAnnouncements.AsNoTracking()
                .Where(a => a.CourseId == id)
                .ToListAsync(ct);
        }
        catch
        {
            return Results.Ok(Array.Empty<AnnouncementDto>());
        }

        var result = rows
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.AuthorName, a.CreatedAt))
            .ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAnnouncement(
        Guid id,
        CreateAnnouncementRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "Title and body are required." });
        }

        var item = new CourseAnnouncement
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            AuthorName = CatalogEndpoints.DisplayName(user),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseAnnouncements.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/catalog/courses/{id}/announcements/{item.Id}",
            new AnnouncementDto(item.Id, item.Title, item.Body, item.AuthorName, item.CreatedAt));
    }
}
