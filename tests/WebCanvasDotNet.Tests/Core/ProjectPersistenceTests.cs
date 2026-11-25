using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class ProjectPersistenceTests
{
    [Fact]
    public void SavesAndReloadsPagesSchemasAndEntities()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var entity = app.AddEntity(schema, ("title", "First story"), ("slug", "first-story"));
        var page = app.AddPage("About", PageNode.Create("h1", null, "About"));
        page.Slug = "about/team";
        app.Project.SavePage(page);
        app.Project.SaveEverything();

        var reloaded = WcProject.Open(app.Project.Root);

        Assert.NotNull(reloaded.Page(page.Id));
        Assert.Equal("about/team", reloaded.Page(page.Id)!.Slug);
        Assert.Equal("Article", reloaded.Schema("Article")!.Name);
        Assert.Single(reloaded.EntityList("Article"));
        Assert.Equal("First story", reloaded.TitleOf(schema, reloaded.Entity(schema, entity.Id)));
    }

    [Fact]
    public void MigratesLegacyIframeAttributesOutOfStyle()
    {
        using var app = new TestApp();
        var iframe = PageNode.Create("iframe", "embed-video");
        iframe.Style = new Dictionary<string, string>
        {
            ["src"] = "https://www.youtube.com/embed/abc",
            ["title"] = "Video",
            ["allowfullscreen"] = "true",
        };
        app.Project.HomePage!.Nodes.Add(iframe);
        app.Project.SavePage(app.Project.HomePage);
        app.Project.SaveEverything();

        var reloaded = WcProject.Open(app.Project.Root);
        var migrated = reloaded.HomePage!.Find(iframe.Id)!;

        Assert.Equal("https://www.youtube.com/embed/abc", migrated.Attrs!["src"]);
        Assert.Equal("Video", migrated.Attrs!["title"]);
        Assert.False(migrated.Style?.ContainsKey("src") == true);
    }
}
