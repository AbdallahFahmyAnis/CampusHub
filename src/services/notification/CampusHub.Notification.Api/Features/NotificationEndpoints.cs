using System.Security.Claims;
using CampusHub.Contracts.Events;
using CampusHub.Notification.Api.Domain;
using CampusHub.Notification.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Notification.Api.Features;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notifications/events", Ingest).AllowAnonymous();
        app.MapGet("/api/notifications/mine", Mine).RequireAuthorization();
        app.MapGet("/api/notifications/unread-count", UnreadCount).RequireAuthorization();
        app.MapPost("/api/notifications/{id:guid}/read", MarkRead).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> Ingest(
        IntegrationEventDto envelope,
        HttpContext http,
        IConfiguration config,
        NotificationProcessor processor,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        await processor.HandleAsync(envelope, ct);
        return Results.Accepted();
    }

    private static async Task<IResult> Mine(ClaimsPrincipal user, NotificationDbContext db, CancellationToken ct)
    {
        var userId = UserId(user);
        var items = (await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && n.Channel == NotificationChannels.InApp)
                .ToListAsync(ct))
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.EventType, n.Read, n.CreatedAt, n.Status))
            .ToList();
        return Results.Ok(items);
    }

    private static async Task<IResult> UnreadCount(ClaimsPrincipal user, NotificationDbContext db, CancellationToken ct)
    {
        var userId = UserId(user);
        var count = await db.Notifications.CountAsync(
            n => n.UserId == userId && n.Channel == NotificationChannels.InApp && !n.Read,
            ct);
        return Results.Ok(new { count });
    }

    private static async Task<IResult> MarkRead(
        Guid id,
        ClaimsPrincipal user,
        NotificationDbContext db,
        CancellationToken ct)
    {
        var userId = UserId(user);
        var item = await db.Notifications.SingleOrDefaultAsync(
            n => n.Id == id && n.UserId == userId && n.Channel == NotificationChannels.InApp,
            ct);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.Read = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string EventType,
    bool Read,
    DateTimeOffset CreatedAt,
    string Status);
