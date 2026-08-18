using Microsoft.AspNetCore.Builder;

namespace CampusHub.BuildingBlocks.Diagnostics;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
