using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class AssignmentDueRulesTests
{
    [Fact]
    [Trait("Story", SddStories.ChS16DueDates)]
    [Trait("Stage", "Test")]
    public void Overdue_when_due_passed_and_not_submitted()
    {
        var due = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        Assert.True(AssignmentDueRules.Overdue(due, submitted: false, now));
    }

    [Fact]
    [Trait("Story", SddStories.ChS16DueDates)]
    public void Not_overdue_when_submitted()
    {
        var due = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        Assert.False(AssignmentDueRules.Overdue(due, submitted: true, now));
    }

    [Fact]
    [Trait("Story", SddStories.ChS16DueDates)]
    public void Late_when_submitted_after_due()
    {
        var due = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var submitted = due.AddHours(1);
        Assert.True(AssignmentDueRules.Late(due, submitted));
    }

    [Fact]
    [Trait("Story", SddStories.ChS16DueDates)]
    public void No_due_date_is_never_overdue_or_late()
    {
        Assert.False(AssignmentDueRules.Overdue(null, submitted: false));
        Assert.False(AssignmentDueRules.Late(null, DateTimeOffset.UtcNow));
    }
}
