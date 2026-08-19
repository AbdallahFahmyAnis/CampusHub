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

    public async Task<Guid?> GetEnrollmentIdAsync(string studentId, Guid courseId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(studentId) || courseId == Guid.Empty)
        {
            return null;
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
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ConfirmedResponse>(ct);
            return body?.Confirmed == true ? body.EnrollmentId : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<EnrollmentStatsDto?> GetStatsAsync(Guid courseId, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/enrollments/internal/stats?courseId={courseId}");
            request.Headers.Add("X-Internal-Key", configuration["Internal:ApiKey"] ?? "campus-dev-internal");
            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EnrollmentStatsDto>(ct);
        }
        catch
        {
            return null;
        }
    }

    private sealed record ConfirmedResponse(bool Confirmed, Guid? EnrollmentId);
}

public sealed record EnrollmentMonthPoint(string Month, int Count, decimal Revenue);
public sealed record EnrollmentStatsDto(
    Guid CourseId,
    int TotalEnrollments,
    int ConfirmedEnrollments,
    int CancelledEnrollments,
    decimal TotalRevenue,
    IReadOnlyList<EnrollmentMonthPoint> MonthlyBreakdown);
