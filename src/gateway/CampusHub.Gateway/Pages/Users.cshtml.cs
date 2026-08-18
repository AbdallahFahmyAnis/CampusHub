using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
public class UsersModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsUser> Users { get; private set; } = [];

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string DisplayName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = "CampusHub!123";

    [BindProperty]
    public string Role { get; set; } = Roles.Student;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Users = await api.GetInternalAsync<List<OpsUser>>("identity", "/api/identity/users", ct) ?? [];
    }

    public async Task<IActionResult> OnPostRegisterAsync(CancellationToken ct)
    {
        var (ok, error) = await api.PostJsonAsync(
            "identity",
            "/api/identity/users",
            new { email = Email, displayName = DisplayName, password = Password, role = Role },
            ct,
            internalKey: true);

        if (ok)
        {
            TempData["Message"] = $"{Role} {Email} registered.";
        }
        else
        {
            TempData["Error"] = error ?? "Could not register the user.";
        }

        return RedirectToPage();
    }
}

public sealed record OpsUser(string Id, string Email, string DisplayName, string[] Roles);
