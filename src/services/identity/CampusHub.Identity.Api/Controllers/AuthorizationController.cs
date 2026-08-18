using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CampusHub.Identity.Api.Controllers;

public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityDbContext db) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        var user = await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        var tenantId = user.TenantId == Guid.Empty ? Tenancy.DefaultTenantId : user.TenantId;
        var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == tenantId)
                     ?? await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == Tenancy.DefaultTenantId);

        identity
            .SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(Claims.Name, user.DisplayName)
            .SetClaim(Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
            .SetClaim(Tenancy.TenantIdClaim, tenantId.ToString())
            .SetClaim(Tenancy.TenantNameClaim, tenant?.Name ?? SeedTenants.DefaultName)
            .SetClaim(Tenancy.PlanClaim, tenant?.Plan ?? SeedTenants.DefaultPlan)
            .SetClaims(Claims.Role, [.. await userManager.GetRolesAsync(user)]);

        identity.SetDestinations(GetDestinations);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(GetResources(request.GetScopes()));

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return SignOut(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or Claims.PreferredUsername or Claims.Role or Claims.Email
                or Tenancy.TenantIdClaim or Tenancy.TenantNameClaim or Tenancy.PlanClaim
                => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        };
    }

    private static IEnumerable<string> GetResources(IEnumerable<string> requestedScopes)
    {
        foreach (var scope in requestedScopes)
        {
            var resource = scope switch
            {
                BuildingBlocks.Security.Scopes.CatalogApi => "catalog-api",
                BuildingBlocks.Security.Scopes.EnrollmentApi => "enrollment-api",
                BuildingBlocks.Security.Scopes.PaymentApi => "payment-api",
                BuildingBlocks.Security.Scopes.NotificationApi => "notification-api",
                BuildingBlocks.Security.Scopes.AccessApi => "access-api",
                BuildingBlocks.Security.Scopes.ChatApi => "chat-api",
                _ => null
            };

            if (resource is not null)
            {
                yield return resource;
            }
        }
    }
}
