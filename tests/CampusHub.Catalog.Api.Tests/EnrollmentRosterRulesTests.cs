using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class EnrollmentRosterRulesTests
{
    private sealed record Row(string Name, DateTimeOffset EnrolledAt);

    [Fact]
    [Trait("Story", SddStories.ChS24Roster)]
    [Trait("Stage", "Test")]
    public void OrderByEnrolledAt_sorts_oldest_first_then_name()
    {
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var rows = new[]
        {
            new Row("Zoe", t0.AddDays(2)),
            new Row("Amy", t0),
            new Row("Ben", t0),
        };
        var ordered = EnrollmentRosterRules.OrderByEnrolledAt(rows, r => r.EnrolledAt, r => r.Name);
        Assert.Equal(["Amy", "Ben", "Zoe"], ordered.Select(r => r.Name));
    }
}
