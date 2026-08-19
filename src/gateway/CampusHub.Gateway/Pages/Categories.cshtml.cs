using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
public class CategoriesModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsSubject> Subjects { get; private set; } = [];

    public string CampusName => Tenancy.TenantName(User);

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Subjects = await api.GetAsync<List<OpsSubject>>("catalog", "/api/catalog/subjects", ct) ?? [];
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        var (ok, error) = await api.PostJsonAsync(
            "catalog",
            "/api/catalog/subjects",
            new { code = Code, name = Name, description = Description },
            ct);

        if (ok)
        {
            TempData["Message"] = $"Category {Code.Trim().ToUpperInvariant()} added.";
        }
        else
        {
            TempData["Error"] = error ?? "Could not add the category.";
        }

        return RedirectToPage();
    }
}

public sealed record OpsSubject(Guid Id, string Code, string Name, string? Description);
