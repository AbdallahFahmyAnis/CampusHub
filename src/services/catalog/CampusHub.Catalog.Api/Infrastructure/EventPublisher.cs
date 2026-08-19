using System.Net.Http.Json;
using System.Text.Json;
using CampusHub.Contracts.Events;

namespace CampusHub.Catalog.Api.Infrastructure;

/// <summary>
/// Fires integration events to the notification and access services.
/// Failures are swallowed so they never fail the calling request.
/// </summary>
public sealed class EventPublisher(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<EventPublisher> logger)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task PublishAsync<T>(string eventType, T payload, CancellationToken ct = default)
    {
        var envelope = new IntegrationEventDto(
            Guid.NewGuid(),
            eventType,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(payload, Json));

        await TrySendAsync("notification", "/api/notifications/events", envelope, ct);
        await TrySendAsync("access", "/api/access/events", envelope, ct);
    }

    private async Task TrySendAsync(string clientName, string path, IntegrationEventDto envelope, CancellationToken ct)
    {
        try
        {
            var client = httpFactory.CreateClient(clientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(envelope)
            };
            request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"] ?? "campus-dev-internal");
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Event publish to {Client}{Path} returned {Status}", clientName, path, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Event publish to {Client}{Path} failed", clientName, path);
        }
    }
}
