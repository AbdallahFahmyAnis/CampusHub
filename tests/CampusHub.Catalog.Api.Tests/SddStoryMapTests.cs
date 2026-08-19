using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class SddStoryMapTests
{
    [Fact]
    [Trait("Stage", "Done")]
    public void Every_shipped_story_has_a_spec_folder()
    {
        Assert.Equal("specs/013-quizzes", SddStories.SpecPath(SddStories.ChS11Quizzes));
        Assert.Equal("specs/002-assignment-due-dates", SddStories.SpecPath(SddStories.ChS16DueDates));
        Assert.Equal("specs/001-course-gradebook", SddStories.SpecPath(SddStories.ChS15Gradebook));
        Assert.Equal("specs/003-tenants-plans", SddStories.SpecPath(SddStories.ChS01Tenants));
    }
}
