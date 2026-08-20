using System.Security.Claims;
using CampusHub.BuildingBlocks.Sdd;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S22 — specs/022-course-resources. Syllabus links and extra materials.</summary>
public static class ResourceEndpoints
{
    public static RouteGroupBuilder MapResourceEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/resources", ListResources);
        api.MapPost("/courses/{id:guid}/resources", CreateResource).RequireAuthorization("CanManageCatalog");
        return api;
    }

    private static async Task<IResult> ListResources(
        Guid id,
        CatalogDbContext db,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        List<CourseResource> rows;
        try
        {
            rows = await db.CourseResources.AsNoTracking()
                .Where(r => r.CourseId == id)
                .ToListAsync(ct);
        }
        catch
        {
            return Results.Ok(Array.Empty<CourseResourceDto>());
        }

        var result = rows
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CourseResourceDto(r.Id, r.Title, r.Url, r.Description, r.CreatedAt))
            .ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateResource(
        Guid id,
        CreateCourseResourceRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Title) || !CourseResourceRules.IsAllowedUrl(request.Url))
        {
            return Results.BadRequest(new { error = "Title and an http(s) URL are required." });
        }

        var item = new CourseResource
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            Title = request.Title.Trim(),
            Url = request.Url.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseResources.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/catalog/courses/{id}/resources/{item.Id}",
            new CourseResourceDto(item.Id, item.Title, item.Url, item.Description, item.CreatedAt));
    }
}
