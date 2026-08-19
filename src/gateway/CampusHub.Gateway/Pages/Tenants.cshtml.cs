using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
[PlatformOnly]
public class TenantsModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsTenant> Tenants { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Tenants = await api.GetInternalAsync<List<OpsTenant>>("identity", "/api/identity/tenants", ct) ?? [];
    }
}

public sealed record OpsTenant(
    Guid Id,
    string Name,
    string Slug,
    string Plan,
    int SeatCap,
    int StudentSeats,
    int MemberCount,
    DateTimeOffset CreatedAt);
