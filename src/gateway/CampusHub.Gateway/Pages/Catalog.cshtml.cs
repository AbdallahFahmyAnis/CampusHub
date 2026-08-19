using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
[PlatformOnly]
public class CatalogModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsCourse> Courses { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var page = await api.GetAsync<PagedOpsCourses>("catalog", "/api/catalog/courses?page=1&pageSize=48", ct);
        Courses = page?.Items ?? [];
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, CancellationToken ct)
    {
        var ok = await api.PostAsync("catalog", $"/api/catalog/courses/{id}/publish", ct);
        TempData["Message"] = ok ? "Course published." : "Publish failed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id, CancellationToken ct)
    {
        var ok = await api.PostAsync("catalog", $"/api/catalog/courses/{id}/archive", ct);
        TempData["Message"] = ok ? "Course archived." : "Archive failed.";
        return RedirectToPage();
    }
}

public sealed record PagedOpsCourses(List<OpsCourse> Items, int Page, int PageSize, int TotalCount);

public sealed record OpsCourse(
    Guid Id,
    string Title,
    string SubjectCode,
    string TeacherName,
    int Capacity,
    int RemainingSeats,
    decimal Price,
    string Status);
