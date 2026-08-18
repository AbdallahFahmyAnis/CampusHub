using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
public class IndexModel(HealthProbe probe, IConfiguration config) : PageModel
{
    public IReadOnlyList<ServiceHealth> Services { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var targets = config.GetSection("Ops:Services").Get<HealthTarget[]>() ?? [];
        Services = await probe.ProbeAsync(targets, ct);
    }
}
