using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
public class UsersModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsUser> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Users = await api.GetInternalAsync<List<OpsUser>>("identity", "/api/identity/users", ct) ?? [];
    }
}

public sealed record OpsUser(string Id, string Email, string DisplayName, string[] Roles);
