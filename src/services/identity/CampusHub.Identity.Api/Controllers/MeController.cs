using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace CampusHub.Identity.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MeController(Data.IdentityDbContext db) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        return Ok(new
        {
            sub = User.FindFirst("sub")?.Value,
            name = User.Identity?.Name,
            roles = User.FindAll("role").Select(c => c.Value).ToArray(),
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpGet("ready-info")]
    [AllowAnonymous]
    public IActionResult ReadyInfo()
    {
        return Ok(new
        {
            service = "identity-service",
            database = db.Database.ProviderName,
            time = DateTimeOffset.UtcNow
        });
    }
}
