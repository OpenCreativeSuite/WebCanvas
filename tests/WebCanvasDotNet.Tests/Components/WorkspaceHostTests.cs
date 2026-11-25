using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class WorkspaceHostTests
{
    [Theory]
    [InlineData(StudioMode.Design, ".design-bar")]
    [InlineData(StudioMode.Blocks, ".blocks-page")]
    [InlineData(StudioMode.Automation, ".automation-split")]
    [InlineData(StudioMode.Development, ".dev-page")]
    public void MountsTheMatchingWorkspace(StudioMode mode, string selector)
    {
        using var app = new TestApp();
        app.Session.OpenMode(mode);

        var page = app.Render<WorkspaceHost>();

        Assert.NotNull(page.Find(".workspace-host"));
        Assert.NotNull(page.Find(selector));
    }

    [Fact]
    public void ContentAndPreviewModesMountTheirOwnSurfaces()
    {
        using var app = new TestApp();
        app.Session.OpenContentMap();
        var page = app.Render<WorkspaceHost>();
        Assert.NotNull(page.Find(".content-map"));

        app.Session.PreviewPage(app.Project.HomePage!);
        page = app.Render<WorkspaceHost>();
        Assert.NotNull(page.Find(".preview-workspace"));
        Assert.NotNull(page.Find(".preview-frame"));
    }

    [Fact]
    public void ClosingEveryTabShowsTheEmptyWorkspaceAndReopensAMode()
    {
        using var app = new TestApp();

        while (app.Session.Tabs.Count > 0)
        {
            app.Session.CloseActiveTab();
        }

        var page = app.Render<WorkspaceHost>();
        Assert.Equal("Everything is closed.", page.Find(".workspace-empty-title").InnerText);
        Assert.Equal(ModeInfo.All.Length, page.FindAll(".workspace-empty-mode").Count);

        page.Click(page.FindAll(".workspace-empty-mode")[0]);
        Assert.Equal(StudioMode.Content, app.Session.ActiveMode);
        Assert.Single(app.Session.Tabs);
    }
}
