using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CampusHub.BuildingBlocks.Diagnostics;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);
        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("campushub.correlation_id", correlationId);

        await next(context);
    }

    private static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationId.HeaderName, out StringValues header) &&
            !StringValues.IsNullOrEmpty(header))
        {
            return header.ToString();
        }

        return System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
