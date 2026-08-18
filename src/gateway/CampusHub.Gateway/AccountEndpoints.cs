using System.Security.Claims;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CampusHub.Gateway;

public static class AccountEndpoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/account").RequireAuthorization().DisableAntiforgery();
        api.MapGet("/me", GetMe);
        api.MapPut("/me", UpdateMe);
        api.MapPost("/password", ChangePassword);
        return app;
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal user, DownstreamApi api, CancellationToken ct)
    {
        var id = UserId(user);
        if (string.IsNullOrEmpty(id))
        {
            return Results.Unauthorized();
        }

        var profile = await api.GetInternalAsync<IdentityProfile>(
            "identity",
            $"/api/identity/users/{Uri.EscapeDataString(id)}",
            ct);
        if (profile is null)
        {
            return Results.Ok(new IdentityProfile(
                id,
                user.FindFirstValue("email") ?? user.Identity?.Name ?? string.Empty,
                user.Identity?.Name ?? string.Empty,
                user.FindAll("role").Select(c => c.Value).ToArray()));
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal user,
        DownstreamApi api,
        CancellationToken ct)
    {
        var id = UserId(user);
        if (string.IsNullOrEmpty(id))
        {
            return Results.Unauthorized();
        }

        var (ok, error) = await api.PutJsonAsync(
            "identity",
            $"/api/identity/users/{Uri.EscapeDataString(id)}",
            new { displayName = request.DisplayName },
            ct,
            internalKey: true);

        return ok ? Results.Ok() : Results.BadRequest(new { error });
    }

    private static async Task<IResult> ChangePassword(
        [FromBody] ChangePasswordBody request,
        ClaimsPrincipal user,
        DownstreamApi api,
        CancellationToken ct)
    {
        var id = UserId(user);
        if (string.IsNullOrEmpty(id))
        {
            return Results.Unauthorized();
        }

        var (ok, error) = await api.PostJsonAsync(
            "identity",
            $"/api/identity/users/{Uri.EscapeDataString(id)}/password",
            new { currentPassword = request.CurrentPassword, newPassword = request.NewPassword },
            ct,
            internalKey: true);

        return ok ? Results.NoContent() : Results.BadRequest(new { error });
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private sealed record IdentityProfile(string Id, string Email, string DisplayName, string[] Roles);
    private sealed record UpdateProfileRequest(string DisplayName);
    private sealed record ChangePasswordBody(string CurrentPassword, string NewPassword);
}
