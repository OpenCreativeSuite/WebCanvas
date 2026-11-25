using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class PrefabKitTests
{
    [Fact]
    public void ListsExactGroupsNamesAndDescriptions()
    {
        using var app = new TestApp();
        var page = app.Render<PrefabKit, PrefabKitProps>(new PrefabKitProps());

        Assert.Equal(["Headers", "Footers"], page.FindAll(".prefab-group-head").Select(group => group.InnerText).ToArray());
        Assert.Equal(
            ["Simple header", "Split header", "Centered header", "Minimal header",
             "Three-column footer", "Simple footer", "Newsletter footer", "Centered footer"],
            page.FindAll(".prefab-name").Select(name => name.InnerText).ToArray());
        Assert.Equal(8, page.FindAll(".prefab-desc").Count);
        Assert.Equal("Create a block", page.Find(".prefab-kit-hint").InnerText);
    }

    [Fact]
    public void CompactModeHidesDescriptions()
    {
        using var app = new TestApp();
        var page = app.Render<PrefabKit, PrefabKitProps>(new PrefabKitProps { Compact = true });

        Assert.True(page.Find(".prefab-kit").HasClass("is-compact"));
        Assert.Empty(page.FindAll(".prefab-desc"));
        Assert.Equal(8, page.FindAll(".prefab-card").Count);
    }

    [Fact]
    public void ApplyingToAnEmptyBlockFillsItWithoutAddingAnotherBlock()
    {
        using var app = new TestApp();
        var block = new BlockDoc { Name = "Site header", Kind = "theme", Category = "Navigation" };
        app.Project.Blocks.Add(block);
        app.Project.SaveBlock(block);
        app.Session.OpenBlock(block);
        int before = app.Project.Blocks.Count;
        var page = app.Render<PrefabKit, PrefabKitProps>(new PrefabKitProps());

        Assert.Equal("Apply to this block", page.Find(".prefab-kit-hint").InnerText);
        page.Click(page.FindByText("Centered header"));

        Assert.Equal(before, app.Project.Blocks.Count);
        Assert.Single(block.Nodes);
        Assert.Equal("header", block.Nodes[0].Tag);
        Assert.Contains("site-header", block.Nodes[0].ClassName);
        Assert.Equal(block.Id, app.Session.EditingBlockId);
    }

    [Fact]
    public void ApplyingFromTheLibraryCreatesANamedThemeBlock()
    {
        using var app = new TestApp();
        int before = app.Project.Blocks.Count;
        var page = app.Render<PrefabKit, PrefabKitProps>(new PrefabKitProps());

        page.Click(page.FindByText("Simple header"));

        Assert.Equal(before + 1, app.Project.Blocks.Count);
        var block = app.Project.Blocks[^1];
        Assert.Equal("Simple header", block.Name);
        Assert.Equal("theme", block.Kind);
        Assert.Equal("Navigation", block.Category);
        Assert.NotNull(app.Session.CurrentBlock);
    }
}
