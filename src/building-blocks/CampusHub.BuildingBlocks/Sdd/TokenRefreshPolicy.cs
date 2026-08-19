namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S17 — specs/017-auth-session. When the BFF must call the refresh-token grant.</summary>
public static class TokenRefreshPolicy
{
    public static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public static bool NeedsRefresh(DateTimeOffset expiresAt, DateTimeOffset? now = null) =>
        expiresAt <= (now ?? DateTimeOffset.UtcNow).Add(RefreshSkew);
}
