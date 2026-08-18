using Microsoft.AspNetCore.Identity;

namespace CampusHub.Identity.Api.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
