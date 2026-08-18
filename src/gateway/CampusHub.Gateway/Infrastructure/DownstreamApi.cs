using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace CampusHub.Gateway.Infrastructure;

public sealed class DownstreamApi(IHttpClientFactory httpFactory, IHttpContextAccessor accessor, IConfiguration config)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string clientName, string path, CancellationToken ct = default)
    {
        using var response = await SendAsync(clientName, HttpMethod.Get, path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, Json, ct);
    }

    public async Task<bool> PostAsync(string clientName, string path, CancellationToken ct = default)
    {
        using var response = await SendAsync(clientName, HttpMethod.Post, path, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<T?> GetInternalAsync<T>(string clientName, string path, CancellationToken ct = default)
    {
        using var response = await SendAsync(clientName, HttpMethod.Get, path, ct, internalKey: true);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, Json, ct);
    }

    public Task<(bool Ok, string? Error)> PostJsonAsync<T>(
        string clientName,
        string path,
        T body,
        CancellationToken ct = default,
        bool internalKey = false) =>
        SendJsonAsync(clientName, HttpMethod.Post, path, body, ct, internalKey);

    public Task<(bool Ok, string? Error)> PutJsonAsync<T>(
        string clientName,
        string path,
        T body,
        CancellationToken ct = default,
        bool internalKey = false) =>
        SendJsonAsync(clientName, HttpMethod.Put, path, body, ct, internalKey);

    private async Task<(bool Ok, string? Error)> SendJsonAsync<T>(
        string clientName,
        HttpMethod method,
        string path,
        T body,
        CancellationToken ct,
        bool internalKey)
    {
        using var response = await SendAsync(clientName, method, path, ct, internalKey, body);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var text = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.GetString() is { Length: > 0 } message)
            {
                return (false, message);
            }
        }
        catch (JsonException)
        {
            // Fall through to the status-code message.
        }

        return (false, $"Request failed ({(int)response.StatusCode}).");
    }

    private async Task<HttpResponseMessage> SendAsync(
        string clientName,
        HttpMethod method,
        string path,
        CancellationToken ct,
        bool internalKey = false,
        object? body = null)
    {
        var client = httpFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        if (internalKey)
        {
            request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"] ?? "campus-dev-internal");
        }
        else if (accessor.HttpContext is not null)
        {
            var token = await accessor.HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await client.SendAsync(request, ct);
    }
}

public sealed class HealthProbe(IHttpClientFactory httpFactory)
{
    public async Task<IReadOnlyList<ServiceHealth>> ProbeAsync(IEnumerable<HealthTarget> targets, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("ops-health");
        var results = new List<ServiceHealth>();
        foreach (var target in targets)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                using var response = await client.GetAsync(target.Url, cts.Token);
                results.Add(new ServiceHealth(target.Name, (int)response.StatusCode, response.IsSuccessStatusCode));
            }
            catch
            {
                results.Add(new ServiceHealth(target.Name, 0, false));
            }
        }

        return results;
    }
}

public sealed record HealthTarget(string Name, string Url);
public sealed record ServiceHealth(string Name, int StatusCode, bool Ready);
