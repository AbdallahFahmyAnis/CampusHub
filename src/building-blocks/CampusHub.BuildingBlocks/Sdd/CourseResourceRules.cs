namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S22 — specs/022-course-resources. Validate external resource URLs.</summary>
public static class CourseResourceRules
{
    public static bool IsAllowedUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
