using CampusHub.BuildingBlocks.Security;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Oidc = OpenIddict.Abstractions.OpenIddictConstants;

namespace CampusHub.Identity.Api.Data;

public sealed class IdentitySeeder(
    IdentityDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    IOpenIddictApplicationManager applications,
    IOpenIddictScopeManager scopes,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger)
{
    public const string DevPassword = "CampusHub!123";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(SeedUsers.AdminId, SeedUsers.AdminEmail, "CampusHub Admin", Roles.Administrator);
        await EnsureUserAsync(SeedUsers.TeacherId, SeedUsers.TeacherEmail, "Ava Teacher", Roles.Teacher);
        await EnsureUserAsync(SeedUsers.StudentId, SeedUsers.StudentEmail, "Sam Student", Roles.Student);

        await EnsureScopeAsync(Scopes.CatalogApi, "catalog-api");
        await EnsureScopeAsync(Scopes.EnrollmentApi, "enrollment-api");
        await EnsureScopeAsync(Scopes.PaymentApi, "payment-api");
        await EnsureScopeAsync(Scopes.NotificationApi, "notification-api");
        await EnsureScopeAsync(Scopes.AccessApi, "access-api");
        await EnsureScopeAsync(Scopes.ChatApi, "chat-api");

        var gatewayOrigin = configuration["Gateway:PublicOrigin"] ?? "http://localhost:5000";
        await EnsureGatewayClientAsync(gatewayOrigin);

        await EnsureServiceClientAsync(
            Clients.EnrollmentService,
            "CampusHub Enrollment Service",
            configuration["Identity:ServiceClientSecret"] ?? "enrollment-dev-secret",
            Scopes.CatalogApi,
            Scopes.PaymentApi);

        logger.LogInformation("Identity seed completed. Dev password for all seeded users: {Password}", DevPassword);
    }

    private async Task EnsureUserAsync(string id, string email, string displayName, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var created = await users.CreateAsync(user, DevPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create {email}: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            }
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            await users.AddToRoleAsync(user, role);
        }
    }

    private async Task EnsureScopeAsync(string name, string resource)
    {
        var existing = await scopes.FindByNameAsync(name);
        if (existing is not null)
        {
            return;
        }

        await scopes.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = name,
            Resources = { resource }
        });
    }

    private async Task EnsureGatewayClientAsync(string gatewayOrigin)
    {
        var existing = await applications.FindByClientIdAsync(Clients.Gateway);
        if (existing is not null)
        {
            return;
        }

        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = Clients.Gateway,
            ClientSecret = configuration["Identity:GatewayClientSecret"] ?? "gateway-dev-secret",
            ConsentType = Oidc.ConsentTypes.Implicit,
            DisplayName = "CampusHub Gateway BFF",
            ClientType = Oidc.ClientTypes.Confidential,
            RedirectUris =
            {
                new Uri($"{gatewayOrigin}/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri($"{gatewayOrigin}/signout-callback-oidc")
            },
            Permissions =
            {
                Oidc.Permissions.Endpoints.Authorization,
                Oidc.Permissions.Endpoints.Token,
                Oidc.Permissions.Endpoints.EndSession,
                Oidc.Permissions.GrantTypes.AuthorizationCode,
                Oidc.Permissions.GrantTypes.RefreshToken,
                Oidc.Permissions.ResponseTypes.Code,
                Oidc.Permissions.Scopes.Email,
                Oidc.Permissions.Scopes.Profile,
                Oidc.Permissions.Scopes.Roles,
                Oidc.Permissions.Prefixes.Scope + "offline_access",
                Oidc.Permissions.Prefixes.Scope + Scopes.CatalogApi,
                Oidc.Permissions.Prefixes.Scope + Scopes.EnrollmentApi,
                Oidc.Permissions.Prefixes.Scope + Scopes.PaymentApi,
                Oidc.Permissions.Prefixes.Scope + Scopes.NotificationApi,
                Oidc.Permissions.Prefixes.Scope + Scopes.AccessApi,
                Oidc.Permissions.Prefixes.Scope + Scopes.ChatApi
            },
            Requirements =
            {
                Oidc.Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }

    private async Task EnsureServiceClientAsync(string clientId, string displayName, string secret, params string[] apiScopes)
    {
        var existing = await applications.FindByClientIdAsync(clientId);
        if (existing is not null)
        {
            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = secret,
            DisplayName = displayName,
            ClientType = Oidc.ClientTypes.Confidential,
            Permissions =
            {
                Oidc.Permissions.Endpoints.Token,
                Oidc.Permissions.GrantTypes.ClientCredentials
            }
        };

        foreach (var scope in apiScopes)
        {
            descriptor.Permissions.Add(Oidc.Permissions.Prefixes.Scope + scope);
        }

        await applications.CreateAsync(descriptor);
    }
}
