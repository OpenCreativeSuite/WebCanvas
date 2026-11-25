using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DesignWorkspaceTests
{
    [Fact]
    public void ViewportAndZoomControlsExposeAndChangeExactState()
    {
        using var app = new TestApp();
        var page = app.Render<DesignWorkspace>();

        Assert.Equal("Desktop - 1200 px", page.Find(".viewport-btn.is-active").GetAttribute("title"));
        Assert.Equal("60%", page.Find(".zoom-value").InnerText);

        page.Click(page.FindAll(".viewport-btn")[1]);
        Assert.Equal("tablet", app.Session.Preferences.Viewport);
        page = app.Render<DesignWorkspace>();
        Assert.Equal("Tablet - 834 px", page.Find(".viewport-btn.is-active").GetAttribute("title"));

        Assert.Equal(80, app.Session.Preferences.DesignZoom);
        page.Click(page.FindAll(".zoom-step")[1]);
        Assert.Equal(105, app.Session.Preferences.DesignZoom);
        page.Click(page.Find(".zoom-value"));
        Assert.Equal(100, app.Session.Preferences.DesignZoom);
        page = app.Render<DesignWorkspace>();
        Assert.Equal("100%", page.Find(".zoom-value").InnerText);
    }

    [Fact]
    public void UndoRedoAndCanvasPreviewFollowEditingState()
    {
        using var app = new TestApp();
        var heading = app.Project.HomePage!.Walk().First(node => node.Tag == "h1");
        app.Session.SelectNode(heading.Id);
        app.Session.SetNodeText("Changed headline");

        var page = app.Render<DesignWorkspace>();
        Assert.False(page.Find("[title='Undo (Ctrl+Z)']").HasAttribute("disabled"));

        page.Click(page.Find("[title='Undo (Ctrl+Z)']"));
        Assert.Equal("Start with something worth reading.", app.Project.HomePage!.Find(heading.Id)!.Text);

        page.Click(page.FindByText("Canvas"));
        Assert.False(app.Session.CanvasEditing);
        page = app.Render<DesignWorkspace>();
        Assert.Equal("Edit", page.Find(".design-bar-right .btn").InnerText.Trim());
        Assert.Equal("Preview — switch to Edit to change the page", page.Find(".canvas-preview-badge").InnerText);
    }

    [Fact]
    public void PreviewButtonBuildsAndOpensAPreviewTab()
    {
        using var app = new TestApp();
        var page = app.Render<DesignWorkspace>();

        page.Click(page.FindByText("Preview"));

        Assert.Equal(StudioMode.Preview, app.Session.ActiveMode);
        Assert.True(app.Session.BuildResult!.Success);
        Assert.Single(app.Session.Tabs, tab => tab.Mode == StudioMode.Preview);
        Assert.StartsWith("Preview · Home", app.Session.ActiveTab!.Title);
    }
}


