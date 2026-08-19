using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class TokenRefreshPolicyTests
{
    [Fact]
    [Trait("Story", SddStories.ChS17Auth)]
    [Trait("Stage", "Test")]
    public void Needs_refresh_when_expiry_is_within_two_minutes()
    {
        var now = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
        var expires = now.AddMinutes(1);
        Assert.True(TokenRefreshPolicy.NeedsRefresh(expires, now));
    }

    [Fact]
    [Trait("Story", SddStories.ChS17Auth)]
    public void Skips_refresh_when_token_has_more_than_two_minutes()
    {
        var now = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
        var expires = now.AddMinutes(5);
        Assert.False(TokenRefreshPolicy.NeedsRefresh(expires, now));
    }
}
