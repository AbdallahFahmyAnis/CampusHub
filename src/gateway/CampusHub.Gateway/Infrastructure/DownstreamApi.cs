using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace CampusHub.Gateway.Infrastructure;

public sealed class DownstreamApi(IHttpClientFactory httpFactory, IHttpContextAccessor accessor, IConfiguration config)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

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

    private async Task<HttpResponseMessage> SendAsync(
        string clientName,
        HttpMethod method,
        string path,
        CancellationToken ct,
        bool internalKey = false)
    {
        var client = httpFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(method, path);
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
