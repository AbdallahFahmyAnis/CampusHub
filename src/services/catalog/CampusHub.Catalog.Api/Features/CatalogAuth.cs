using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Domain;

namespace CampusHub.Catalog.Api.Features;

internal static class CatalogAuth
{
    public static bool CanManage(ClaimsPrincipal user) =>
        user.IsInRole(Roles.Teacher) || user.IsInRole(Roles.Administrator);

    public static bool IsOwner(Course course, ClaimsPrincipal user)
    {
        var (id, email) = Caller(user);
        return course.TeacherId == id || string.Equals(course.TeacherEmail, email, StringComparison.OrdinalIgnoreCase);
    }

    public static (string Id, string Email, string Name) CallerFull(ClaimsPrincipal user)
    {
        var (id, email) = Caller(user);
        var name = user.FindFirstValue("name") ?? user.Identity?.Name ?? email;
        return (id, email, name);
    }

    public static (string Id, string Email) Caller(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = user.FindFirstValue("email")
                    ?? user.FindFirstValue("preferred_username")
                    ?? user.Identity?.Name
                    ?? string.Empty;
        return (id, email);
    }
}
