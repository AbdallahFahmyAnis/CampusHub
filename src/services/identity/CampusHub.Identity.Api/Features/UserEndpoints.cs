using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Identity.Api.Features;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/users", List).AllowAnonymous();
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
}

public sealed record IdentityUserDto(string Id, string Email, string DisplayName, string[] Roles);
