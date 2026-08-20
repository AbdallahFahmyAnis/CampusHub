using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class QuestionModerationRulesTests
{
    [Theory]
    [Trait("Story", SddStories.ChS25Moderation)]
    [Trait("Stage", "Test")]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, true)]
    public void CanModerate_requires_owner_or_admin(
        bool canManage,
        bool isOwner,
        bool isAdmin,
        bool expected) =>
        Assert.Equal(expected, QuestionModerationRules.CanModerate(canManage, isOwner, isAdmin));

    [Fact]
    [Trait("Story", SddStories.ChS25Moderation)]
    [Trait("Stage", "Test")]
    public void OrderForDisplay_puts_pinned_first()
    {
        var now = DateTimeOffset.UtcNow;
        var rows = new[]
        {
            (Id: 1, Pinned: false, At: now),
            (Id: 2, Pinned: true, At: now.AddDays(-1)),
            (Id: 3, Pinned: false, At: now.AddDays(1)),
        };

        var ordered = QuestionModerationRules.OrderForDisplay(rows, r => r.Pinned, r => r.At).Select(r => r.Id).ToList();
        Assert.Equal(new[] { 2, 3, 1 }, ordered);
    }

    [Fact]
    [Trait("Story", SddStories.ChS25Moderation)]
    [Trait("Stage", "Test")]
    public void VisibleToStudents_omits_hidden()
    {
        var rows = new[] { false, true, false };
        Assert.Equal(2, QuestionModerationRules.VisibleToStudents(rows, hidden => hidden).Count());
    }
}
