using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class PreviewWorkspaceTests
{
    [Fact]
    public void BuiltPageUsesTheLocalHttpServerAndWebview()
    {
        using var app = new TestApp();
        app.Session.PreviewPage(app.Project.HomePage!);
        var page = app.Render<PreviewWorkspace>();
        var target = app.Session.PreviewTarget(app.Project.HomePage!);
        Assert.NotNull(target);
        var (file, url, address) = target.Value;

        Assert.Equal("/", page.Find(".preview-address").InnerText);
        Assert.Equal("webview", page.Find(".preview-frame").Tag);
        Assert.Equal(url, page.Find(".preview-frame").GetAttribute("src"));
        Assert.StartsWith("http://127.0.0.1:", url);
        Assert.Equal("/", address);
        Assert.Equal("Home", page.Find(".mode-bar-crumb").InnerText.Trim());
        Assert.Equal("Preview · Home", app.Session.ActiveTab!.Title);
        Assert.Equal(app.Project.HomePage!.Id, app.Session.ActiveTab.ItemId);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void EntityTemplatePreviewsItsFirstRecordRoute()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var detail = app.AddPage("Article detail", PageNode.Create("h1", null, "{{title}}"));
        schema.DetailPageId = detail.Id;
        app.Project.SaveSchema(schema);
        var entity = app.AddEntity(schema, ("title", "First story"), ("slug", "First Story"));

        app.Session.PreviewPage(detail);
        var page = app.Render<PreviewWorkspace>();
        var target = app.Session.PreviewTarget(detail);
        Assert.NotNull(target);
        var (file, url, address) = target.Value;

        Assert.Equal("/articles/first-story/", address);
        Assert.Equal("/articles/first-story/", page.Find(".preview-address").InnerText);
        Assert.Contains("articles/first-story/", url);
        Assert.Equal("Preview · Article detail", app.Session.ActiveTab!.Title);
        Assert.Equal(detail.Id, app.Session.ActiveTab.ItemId);
        Assert.True(File.Exists(file));
        Assert.Equal(entity.Id, app.Project.EntityList(schema).Single().Id);
    }

    [Fact]
    public void RebuildReplacesThePreviewTabAndReloadsTheBuild()
    {
        using var app = new TestApp();
        app.Session.PreviewPage(app.Project.HomePage!);
        string firstTab = app.Session.ActiveTabId!;
        int firstBuild = app.Session.BuildResult!.Pages;

        var page = app.Render<PreviewWorkspace>();
        page.Click(page.FindByText("Rebuild"));

        Assert.NotEqual(firstTab, app.Session.ActiveTabId);
        Assert.Single(app.Session.Tabs, tab => tab.Mode == StudioMode.Preview);
        Assert.Equal(firstBuild, app.Session.BuildResult!.Pages);
    }

    [Fact]
    public void WithoutABuildItShowsTheExactEmptyState()
    {
        using var app = new TestApp();
        var page = app.Render<PreviewWorkspace>();

        Assert.Equal("Nothing to preview yet.", page.Find(".workspace-empty-title").InnerText);
        Assert.Equal("Build the site, then open a page preview from Design.", page.Find(".workspace-empty-copy").InnerText);
    }
}
