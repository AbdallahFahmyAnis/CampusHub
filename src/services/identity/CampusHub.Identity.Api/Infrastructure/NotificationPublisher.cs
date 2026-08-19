using System.Net.Http.Json;
using System.Text.Json;
using CampusHub.Contracts.Events;

namespace CampusHub.Identity.Api.Infrastructure;

/// <summary>
/// Fire-and-forget helper that sends an integration event to the notification service.
/// Failures are logged but never bubble up to the calling handler.
/// </summary>
public sealed class NotificationPublisher(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<NotificationPublisher> logger)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task PublishAsync<T>(string eventType, T payload, CancellationToken ct = default)
    {
        try
        {
            var envelope = new IntegrationEventDto(
                Guid.NewGuid(),
                eventType,
                DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(payload, Json));

            var client = httpFactory.CreateClient("notification");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/events")
            {
                Content = JsonContent.Create(envelope)
            };
            request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"] ?? "campus-dev-internal");
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Notification publish returned {Status} for {Type}", (int)response.StatusCode, eventType);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification publish failed for {Type}", eventType);
        }
    }
}
