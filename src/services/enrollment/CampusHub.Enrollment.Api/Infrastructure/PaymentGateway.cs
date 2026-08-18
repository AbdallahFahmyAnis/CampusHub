using System.Net.Http.Json;

namespace CampusHub.Enrollment.Api.Infrastructure;

public sealed class PaymentGateway(HttpClient http, IConfiguration configuration)
{
    public async Task<PaymentIntentSnapshot> InitiateAsync(
        Guid enrollmentId,
        Guid courseId,
        string studentId,
        decimal amount,
        string simulateOutcome,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/intents")
        {
            Content = JsonContent.Create(new
            {
                enrollmentId,
                courseId,
                studentId,
                amount,
                simulateOutcome
            })
        };
        request.Headers.Add("X-Internal-Key", configuration["Internal:ApiKey"] ?? "campus-dev-internal");
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentIntentSnapshot>(ct))!;
    }
}

public sealed record PaymentIntentSnapshot(Guid PaymentId, Guid EnrollmentId, string Status);
