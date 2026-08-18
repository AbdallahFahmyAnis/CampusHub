using System.Text;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Identity.Api.Features;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/tenants/register", Register).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Register(
        RegisterCampusRequest request,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        UserManager<ApplicationUser> users)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        if (!http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) ||
            !string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        var campusName = request.CampusName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();
        var password = request.Password ?? string.Empty;
        if (campusName.Length < 3 || string.IsNullOrWhiteSpace(email) || password.Length < 8)
        {
            return Results.BadRequest(new { error = "Campus name, email, and a password of at least 8 characters are required." });
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            return Results.Conflict(new { error = "A user with that email already exists." });
        }

        var slug = await UniqueSlugAsync(db, Slugify(campusName));
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = campusName,
            Slug = slug,
            Plan = Plans.Free,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.Add(tenant);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            TenantId = tenant.Id
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            db.Tenants.Remove(tenant);
            return Results.BadRequest(new { error = string.Join(" ", created.Errors.Select(e => e.Description)) });
        }

        await users.AddToRoleAsync(user, Roles.Administrator);
        await db.SaveChangesAsync();
        return Results.Created($"/api/identity/tenants/{tenant.Id}", new
        {
            tenantId = tenant.Id,
            name = tenant.Name,
            slug = tenant.Slug,
            plan = tenant.Plan,
            email
        });
    }

    private static async Task<string> UniqueSlugAsync(IdentityDbContext db, string slug)
    {
        var candidate = slug;
        var n = 2;
        while (await db.Tenants.AnyAsync(t => t.Slug == candidate))
        {
            candidate = $"{slug}-{n++}";
        }

        return candidate;
    }

    private static string Slugify(string name)
    {
        var builder = new StringBuilder();
        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (ch is ' ' or '-' or '_' && builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length < 3 ? "campus" : slug[..Math.Min(slug.Length, 60)];
    }
}

public sealed record RegisterCampusRequest(string CampusName, string Email, string DisplayName, string Password);
