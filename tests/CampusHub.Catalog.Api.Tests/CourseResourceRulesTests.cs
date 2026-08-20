using CampusHub.BuildingBlocks.Sdd;
using Xunit;

namespace CampusHub.Catalog.Api.Tests;

public class CourseResourceRulesTests
{
    [Theory]
    [Trait("Story", SddStories.ChS22Resources)]
    [Trait("Stage", "Test")]
    [InlineData("https://example.com/syllabus.pdf", true)]
    [InlineData("http://campus.local/reading", true)]
    [InlineData("ftp://files.example/a", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllowedUrl_accepts_http_https_only(string? url, bool expected) =>
        Assert.Equal(expected, CourseResourceRules.IsAllowedUrl(url));
}
