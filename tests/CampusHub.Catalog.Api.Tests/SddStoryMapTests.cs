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
        Assert.Equal("specs/017-auth-session", SddStories.SpecPath(SddStories.ChS17Auth));
        Assert.Equal("specs/019-enroll-checkout", SddStories.SpecPath(SddStories.ChS19Enroll));
        Assert.Equal("specs/022-course-resources", SddStories.SpecPath(SddStories.ChS22Resources));
        Assert.Equal("specs/023-course-waitlist", SddStories.SpecPath(SddStories.ChS23Waitlist));
    }
}
