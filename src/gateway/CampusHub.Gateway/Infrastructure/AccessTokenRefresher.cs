using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CampusHub.Gateway.Infrastructure;

public sealed class AccessTokenRefresher(IHttpClientFactory httpFactory, IConfiguration config, ILogger<AccessTokenRefresher> logger)
{
    public async Task RefreshIfNeededAsync(CookieValidatePrincipalContext context)
    {
        var expiresAtValue = context.Properties.GetTokenValue("expires_at");
        if (string.IsNullOrEmpty(expiresAtValue) ||
            !DateTimeOffset.TryParse(expiresAtValue, out var expiresAt) ||
            expiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var authority = (config["Identity:InternalAuthority"] ?? config["Identity:Authority"] ?? "http://localhost:5101").TrimEnd('/');
        var client = httpFactory.CreateClient("oidc-token");
        using var response = await client.PostAsync($"{authority}/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = config["Identity:ClientId"] ?? "campushub-gateway",
            ["client_secret"] = config["Identity:ClientSecret"] ?? "gateway-dev-secret"
        }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Refresh token request failed with {Status}", (int)response.StatusCode);
            context.RejectPrincipal();
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString();
        var newRefresh = root.TryGetProperty("refresh_token", out var refreshEl) ? refreshEl.GetString() : refreshToken;
        var expiresIn = root.TryGetProperty("expires_in", out var expiresEl) ? expiresEl.GetInt32() : 3600;

        if (string.IsNullOrEmpty(accessToken))
        {
            context.RejectPrincipal();
            return;
        }

        context.Properties.UpdateTokenValue("access_token", accessToken);
        if (!string.IsNullOrEmpty(newRefresh))
        {
            context.Properties.UpdateTokenValue("refresh_token", newRefresh);
        }

        context.Properties.UpdateTokenValue("expires_at", DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o"));
        context.ShouldRenew = true;
    }
}
