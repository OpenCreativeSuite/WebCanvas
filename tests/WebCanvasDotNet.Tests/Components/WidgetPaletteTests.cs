using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class WidgetPaletteTests
{
    [Fact]
    public void ShowsSavedBlocksAndEveryWidgetGroup()
    {
        using var app = new TestApp();
        var page = app.Render<WidgetPalette, WidgetPaletteProps>(new WidgetPaletteProps());

        Assert.Equal("Widgets", page.Find(".widgets-title").InnerText);
        Assert.Contains("Saved blocks", page.FindAll(".widgets-group-head").Select(head => head.InnerText));
        Assert.Equal(StudioSession.Presets.Length + app.Project.Blocks.Count, page.FindAll(".widget-row").Count);
        Assert.Equal(app.Project.Blocks.Count, page.FindAll(".widget-row.is-block").Count);
    }

    [Fact]
    public void SearchFiltersExactlyAndEmptySearchHasAMessage()
    {
        using var app = new TestApp();
        app.Session.SetWidgetSearch("hero");
        var page = app.Render<WidgetPalette, WidgetPaletteProps>(new WidgetPaletteProps());

        Assert.Equal(["Hero with text"], page.FindAll(".widget-label").Select(label => label.InnerText).ToArray());
        Assert.Single(page.FindAll(".widgets-group"));

        app.Session.SetWidgetSearch("zzz-nothing");
        page = app.Render<WidgetPalette, WidgetPaletteProps>(new WidgetPaletteProps());
        Assert.Equal("No widgets match “zzz-nothing”.", page.Find(".widgets-empty").InnerText);
    }

    [Fact]
    public void ClickingAWidgetAddsAndSelectsTheNode()
    {
        using var app = new TestApp();
        int before = app.Project.HomePage!.Nodes.Count;
        var page = app.Render<WidgetPalette, WidgetPaletteProps>(new WidgetPaletteProps());

        page.Click(page.FindByText("Hero with text"));

        Assert.Equal(before + 1, app.Project.HomePage.Nodes.Count);
        var added = app.Project.HomePage.Nodes.First(node => node.ClassName == "site-hero");
        Assert.Equal(added.Id, app.Session.SelectedNodeId);
    }

    [Fact]
    public void HideSavedRemovesTheSavedGroup()
    {
        using var app = new TestApp();
        var page = app.Render<WidgetPalette, WidgetPaletteProps>(new WidgetPaletteProps { HideSaved = true });

        Assert.Empty(page.FindAll(".widget-row.is-block"));
        Assert.DoesNotContain("Saved blocks", page.FindAll(".widgets-group-head").Select(head => head.InnerText));
    }
}
