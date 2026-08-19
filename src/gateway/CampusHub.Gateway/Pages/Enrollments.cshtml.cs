using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[Authorize(Roles = Roles.Administrator)]
[PlatformOnly]
public class EnrollmentsModel(DownstreamApi api) : PageModel
{
    public IReadOnlyList<OpsEnrollment> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Items = await api.GetAsync<List<OpsEnrollment>>("enrollment", "/api/enrollments", ct) ?? [];
    }
}

public sealed record OpsEnrollment(
    Guid Id,
    string CourseTitle,
    string StudentName,
    string StudentEmail,
    decimal Amount,
    string Status,
    string? FailureReason,
    DateTimeOffset UpdatedAt);
