using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class WaitlistRulesTests
{
    [Fact]
    [Trait("Story", SddStories.ChS23Waitlist)]
    [Trait("Stage", "Test")]
    public void Position_is_one_based_fifo()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var b = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var queue = new List<(Guid, DateTimeOffset)>
        {
            (a, t0),
            (b, t0.AddMinutes(1)),
        };
        Assert.Equal(1, WaitlistRules.Position(queue, a));
        Assert.Equal(2, WaitlistRules.Position(queue, b));
    }

    [Theory]
    [Trait("Story", SddStories.ChS23Waitlist)]
    [InlineData(true, 0, false, false, true)]
    [InlineData(true, 1, false, false, false)]
    [InlineData(false, 0, false, false, false)]
    [InlineData(true, 0, true, false, false)]
    [InlineData(true, 0, false, true, false)]
    public void CanJoin_requires_published_full_and_eligible(
        bool published, int seats, bool enrolled, bool waitlisted, bool expected) =>
        Assert.Equal(expected, WaitlistRules.CanJoin(published, seats, enrolled, waitlisted));
}
