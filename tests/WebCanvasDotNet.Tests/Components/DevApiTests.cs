using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevApiTests
{
    [Fact]
    public void EndpointRowsHaveExactPathsTypesAndDescriptions()
    {
        using var app = new TestApp();
        app.AddSchema("Article");
        var team = app.AddPage("Team", PageNode.Create("h1", null, "Team"));
        team.Slug = "about/team";
        app.Project.SavePage(team);

        var page = app.Render<DevApi>();

        Assert.Equal("APIs", page.Find(".dev-title").InnerText);
        Assert.Equal(4, page.FindAll(".api-row").Count);
        Assert.Equal(
            ["/data/articles/index.json", "/data/articles/{id}.json", "/", "/about/team/"],
            page.FindAll(".api-row .api-path").Select(path => path.InnerText).ToArray());
        Assert.All(page.FindAll(".api-row .api-method"), method => Assert.Equal("GET", method.InnerText));
        Assert.Equal(["Article", "Article", "Page", "Page"], page.FindAll(".api-row .api-type").Select(type => type.InnerText).ToArray());
        Assert.Equal(["0 records", "One record", "Home", "Team"], page.FindAll(".api-row .api-desc").Select(desc => desc.InnerText).ToArray());
        Assert.Equal("4 routes", page.Find(".dev-section-note").InnerText);
        Assert.Equal(2, page.FindAll(".inline-actions .btn").Count);
    }
}
