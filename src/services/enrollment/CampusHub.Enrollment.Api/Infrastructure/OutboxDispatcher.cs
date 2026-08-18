using System.Net.Http.Json;
using CampusHub.Contracts.Events;
using CampusHub.Enrollment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Enrollment.Api.Infrastructure;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopes,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
                var pending = (await db.Outbox
                    .Where(m => m.ProcessedAt == null)
                    .ToListAsync(stoppingToken))
                    .OrderBy(m => m.OccurredAt)
                    .Take(20)
                    .ToList();

                foreach (var message in pending)
                {
                    var envelope = new IntegrationEventDto(message.Id, message.Type, message.OccurredAt, message.Payload);
                    await PublishAsync("notification", "/api/notifications/events", envelope, stoppingToken);

                    if (message.Type is EventTypes.EnrollmentConfirmed or EventTypes.EnrollmentCancelled)
                    {
                        await PublishAsync("access", "/api/access/events", envelope, stoppingToken);
                    }

                    logger.LogInformation("Outbox published {Type} {Id}", message.Type, message.Id);
                    message.ProcessedAt = DateTimeOffset.UtcNow;
                }

                if (pending.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task PublishAsync(string clientName, string path, IntegrationEventDto envelope, CancellationToken ct)
    {
        var client = httpFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(envelope)
        };
        request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"] ?? "campus-dev-internal");
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
