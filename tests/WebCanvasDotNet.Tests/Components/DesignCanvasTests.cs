using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DesignCanvasTests
{
    [Fact]
    public void CaptionAndPaperReflectViewportAndZoom()
    {
        using var app = new TestApp();
        var page = app.Render<DesignCanvas>();

        Assert.Equal("Home", page.Find(".canvas-caption-title").InnerText);
        Assert.Equal("/", page.Find(".canvas-caption-url").InnerText);
        Assert.Equal("1200 px", page.Find(".canvas-caption-size").InnerText);
        Assert.Equal("width:720px", page.Find(".canvas-slot").GetAttribute("style"));
        Assert.Contains("width:1200px", page.Find(".canvas-paper").GetAttribute("style"));
        Assert.Contains("min-height:1367px", page.Find(".canvas-paper").GetAttribute("style"));
        Assert.Contains("transform:scale(0.6)", page.Find(".canvas-paper").GetAttribute("style"));

        app.Session.SetViewport("tablet");
        app.Session.SetZoom(100);
        page = app.Render<DesignCanvas>();

        Assert.Equal("834 px", page.Find(".canvas-caption-size").InnerText);
        Assert.Equal("width:834px", page.Find(".canvas-slot").GetAttribute("style"));
        Assert.Contains("min-height:820px", page.Find(".canvas-paper").GetAttribute("style"));
        Assert.Contains("transform:scale(1)", page.Find(".canvas-paper").GetAttribute("style"));
    }

    [Fact]
    public void SelectedNodeGetsTheSelectedClassAndPreviewModeAddsTheBadge()
    {
        using var app = new TestApp();
        var heading = app.Project.HomePage!.Walk().First(node => node.Tag == "h1");
        app.Session.SelectNode(heading.Id);

        var page = app.Render<DesignCanvas>();
        var selected = page.Find(".wc-node.is-selected");
        Assert.Equal("h1", selected.Tag);
        Assert.Contains(heading.Text!, selected.InnerText);

        app.Session.ToggleCanvasEditing();
        page = app.Render<DesignCanvas>();

        Assert.Empty(page.FindAll(".wc-node.is-selected"));
        Assert.Equal("Preview — switch to Edit to change the page", page.Find(".canvas-preview-badge").InnerText);
    }

    [Fact]
    public void CustomHtmlBlockReservesItsMeasuredBox()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<div>Custom</div>",
            new Dictionary<string, string>
            {
                ["css"] = ".nothing { color: red; }",
                ["width"] = "512",
                ["height"] = "180",
            });
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.SelectNode(node.Id);

        var page = app.Render<DesignCanvas>();
        var block = page.Find(".wc-html-block");

        Assert.Contains("width:512px", block.GetAttribute("style"));
        Assert.Contains("min-height:180px", block.GetAttribute("style"));
        Assert.Contains("HTML block", block.InnerText);
        Assert.True(block.HasClass("is-selected"));
    }
    [Fact]
    public void DragStartsTheBannerWithTheWidgetLabel()
    {
        using var app = new TestApp();
        app.Session.BeginWidgetDrag("preset:heading");

        var page = app.Render<DesignCanvas>();
        Assert.Equal("Drop “Heading” into the page", page.Find(".canvas-drag-banner").InnerText);

        app.Session.EndWidgetDrag();
        page = app.Render<DesignCanvas>();
        Assert.Empty(page.FindAll(".canvas-drag-banner"));
    }
}


