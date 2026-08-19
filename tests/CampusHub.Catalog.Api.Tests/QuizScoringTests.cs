using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class QuizScoringTests
{
    [Theory]
    [Trait("Story", SddStories.ChS11Quizzes)]
    [Trait("Stage", "Test")]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 50)]
    [InlineData(2, 3, 67)]
    [InlineData(3, 3, 100)]
    public void Percent_rounds_nearest(int score, int total, int expected) =>
        Assert.Equal(expected, QuizScoring.Percent(score, total));

    [Fact]
    [Trait("Story", SddStories.ChS11Quizzes)]
    public void Passed_at_or_above_threshold()
    {
        Assert.True(QuizScoring.Passed(70, 70));
        Assert.False(QuizScoring.Passed(69, 70));
    }
}
