using CampusHub.BuildingBlocks.Security;
using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Identity.Api.Features;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/users", List).AllowAnonymous();
        app.MapGet("/api/identity/users/{id}", GetById).AllowAnonymous();
        app.MapPost("/api/identity/users", Create).AllowAnonymous();
        app.MapPut("/api/identity/users/{id}", Update).AllowAnonymous();
        app.MapPost("/api/identity/users/{id}/password", ChangePassword).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> List(
        HttpContext http,
        IConfiguration config,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        if (!http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) ||
            !string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        var list = await users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(ct);
        var result = new List<IdentityUserDto>();
        foreach (var user in list)
        {
            result.Add(new IdentityUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                [.. await users.GetRolesAsync(user)]));
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> Create(
        CreateIdentityUserRequest request,
        HttpContext http,
        IConfiguration config,
        UserManager<ApplicationUser> users)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        if (!http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) ||
            !string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        var email = request.Email?.Trim() ?? string.Empty;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();
        var role = request.Role?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        if (!Roles.All.Contains(role, StringComparer.Ordinal))
        {
            return Results.BadRequest(new { error = "Role must be Student, Teacher, or Administrator." });
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            return Results.Conflict(new { error = "A user with that email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Results.BadRequest(new { error = string.Join(" ", created.Errors.Select(e => e.Description)) });
        }

        await users.AddToRoleAsync(user, role);
        return Results.Created($"/api/identity/users/{user.Id}",
            new IdentityUserDto(user.Id, user.Email ?? email, user.DisplayName, [role]));
    }

    private static async Task<IResult> GetById(
        string id,
        HttpContext http,
        IConfiguration config,
        UserManager<ApplicationUser> users)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var user = await users.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new IdentityUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            [.. await users.GetRolesAsync(user)]));
    }

    private static async Task<IResult> Update(
        string id,
        UpdateIdentityUserRequest request,
        HttpContext http,
        IConfiguration config,
        UserManager<ApplicationUser> users)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var user = await users.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Results.BadRequest(new { error = "Display name is required." });
        }

        user.DisplayName = displayName;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return Results.BadRequest(new { error = string.Join(" ", updated.Errors.Select(e => e.Description)) });
        }

        return Results.Ok(new IdentityUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            [.. await users.GetRolesAsync(user)]));
    }

    private static async Task<IResult> ChangePassword(
        string id,
        ChangePasswordRequest request,
        HttpContext http,
        IConfiguration config,
        UserManager<ApplicationUser> users)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var user = await users.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest(new { error = "Current and new passwords are required." });
        }

        var changed = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
        {
            return Results.BadRequest(new { error = string.Join(" ", changed.Errors.Select(e => e.Description)) });
        }

        return Results.NoContent();
    }

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}

public sealed record IdentityUserDto(string Id, string Email, string DisplayName, string[] Roles);

public sealed record CreateIdentityUserRequest(string Email, string DisplayName, string Password, string Role);

public sealed record UpdateIdentityUserRequest(string DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
