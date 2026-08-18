using System.Net.Http.Json;
using CampusHub.Payment.Api.Domain;
using CampusHub.Payment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Payment.Api.Infrastructure;

public sealed class MockPspProcessor(IServiceScopeFactory scopes, IHttpClientFactory httpFactory, IConfiguration config, ILogger<MockPspProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                var cutoff = DateTimeOffset.UtcNow.AddMilliseconds(-400);
                var pending = (await db.Payments
                    .Where(p => p.Status == PaymentStatus.Pending)
                    .ToListAsync(stoppingToken))
                    .Where(p => p.CreatedAt < cutoff)
                    .OrderBy(p => p.CreatedAt)
                    .Take(10)
                    .ToList();

                foreach (var payment in pending)
                {
                    var succeeded = !string.Equals(payment.SimulateOutcome, "Failed", StringComparison.OrdinalIgnoreCase);
                    payment.Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed;
                    payment.FailureReason = succeeded ? null : "Mock PSP declined the authorization.";
                    payment.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    await NotifyEnrollmentAsync(payment, succeeded, stoppingToken);
                    logger.LogInformation("Mock PSP {Status} for payment {PaymentId} enrollment {EnrollmentId}",
                        payment.Status, payment.Id, payment.EnrollmentId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mock PSP processor failed");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400), stoppingToken);
        }
    }

    private async Task NotifyEnrollmentAsync(PaymentIntent payment, bool succeeded, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("enrollment");
        var path = succeeded ? "/api/enrollments/internal/payments/succeeded" : "/api/enrollments/internal/payments/failed";
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new
            {
                payment.EnrollmentId,
                PaymentId = payment.Id,
                Reason = payment.FailureReason
            })
        };
        request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"] ?? "campus-dev-internal");
        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Enrollment callback failed ({Status}) for {EnrollmentId}", response.StatusCode, payment.EnrollmentId);
        }
    }
}
