using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusHub.Enrollment.Api.Infrastructure;

public sealed class CatalogGateway(HttpClient http, IConfiguration configuration)
{
    public async Task<CourseSnapshot?> GetCourseAsync(Guid courseId, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/catalog/courses/{courseId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseSnapshot>(ct);
    }

    public Task<bool> ReserveSeatAsync(Guid courseId, CancellationToken ct)
        => SendReservationAsync(HttpMethod.Post, courseId, ct);

    public Task ReleaseSeatAsync(Guid courseId, CancellationToken ct)
        => SendReservationAsync(HttpMethod.Delete, courseId, ct);

    private async Task<bool> SendReservationAsync(HttpMethod method, Guid courseId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, $"/api/catalog/courses/{courseId}/reservations");
        request.Headers.Add("X-Internal-Key", configuration["Internal:ApiKey"] ?? "campus-dev-internal");
        var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}

public sealed record CourseSnapshot(
    Guid Id,
    string Title,
    decimal Price,
    string Status,
    bool CanEnroll,
    int RemainingSeats);
