using System.Net.Http.Json;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class EnrollmentGateway(HttpClient http, IConfiguration configuration)
{
    public async Task<bool> IsConfirmedAsync(string studentId, Guid courseId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(studentId) || courseId == Guid.Empty)
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/enrollments/internal/confirmed?studentId={Uri.EscapeDataString(studentId)}&courseId={courseId}");
            request.Headers.Add("X-Internal-Key", configuration["Internal:ApiKey"] ?? "campus-dev-internal");
            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<ConfirmedResponse>(ct);
            return body?.Confirmed == true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record ConfirmedResponse(bool Confirmed);
}
