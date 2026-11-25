using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class IdsTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Aster & Oak  ", "aster-oak")]
    [InlineData("Already-Slugged", "already-slugged")]
    [InlineData("", "")]
    public void SlugProducesUrlSafeSegments(string input, string expected)
    {
        Assert.Equal(expected, Ids.Slug(input));
    }

    [Theory]
    [InlineData("about/team", "about/team")]
    [InlineData("/about/team/", "about/team")]
    [InlineData("About Us / Our Team", "about-us/our-team")]
    [InlineData("///", "")]
    public void SlugPathNormalisesNestedAddresses(string input, string expected)
    {
        Assert.Equal(expected, Ids.SlugPath(input));
    }

    [Fact]
    public void PluralAndHumanizeStayReadable()
    {
        Assert.Equal("Articles", Ids.Plural("Article"));
        Assert.Equal("Published At", Ids.Humanize("publishedAt"));
    }
}
