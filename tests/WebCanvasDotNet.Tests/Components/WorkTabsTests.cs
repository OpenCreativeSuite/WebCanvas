using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class WorkTabsTests
{
    [Fact]
    public void ShowsTheActiveTabWithItsExactTitleAndIcon()
    {
        using var app = new TestApp();
        var page = app.Render<WorkTabs>();

        Assert.Single(page.FindAll(".worktab"));
        Assert.True(page.Find(".worktab").HasClass("is-active"));
        Assert.Equal("Home", page.Find(".tab-title").InnerText);
        Assert.Equal("Design · Home", page.Find(".tab-select").GetAttribute("title"));
        Assert.NotNull(page.Find(".tab-icon.fa-shapes"));
    }

    [Fact]
    public void OpeningAndClosingTabsKeepsTheClickedTabActive()
    {
        using var app = new TestApp();
        app.Session.OpenTab(StudioMode.Data, "Article", "Article", "fa-database");
        var page = app.Render<WorkTabs>();

        Assert.Equal(2, page.FindAll(".worktab").Count);
        Assert.Equal("Article", page.Find(".worktab.is-active .tab-title").InnerText);
        Assert.Equal("Data · Article", page.Find(".worktab.is-active .tab-select").GetAttribute("title"));

        page.Click(page.FindAll(".tab-select")[0]);
        Assert.Equal("Home", app.Session.ActiveTab!.Title);

        page = app.Render<WorkTabs>();
        page.Click(page.FindAll(".tab-close")[^1]);
        Assert.Single(app.Session.Tabs);
        Assert.Equal("Home", app.Session.ActiveTab!.Title);
    }

    [Fact]
    public void AddButtonOpensTheModeMenu()
    {
        using var app = new TestApp();
        var page = app.Render<WorkTabs>();

        page.Click(".tab-add");

        Assert.True(app.Session.ShowModeMenu);
    }
}
