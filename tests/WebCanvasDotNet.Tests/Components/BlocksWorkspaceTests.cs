using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class BlocksWorkspaceTests
{
    [Fact]
    public void LibraryShowsPrefabGroupsAndExistingThemeBlocks()
    {
        using var app = new TestApp();
        var page = app.Render<BlocksWorkspace>();

        Assert.Equal("Blocks", page.Find(".mode-bar-title").InnerText);
        Assert.Equal("Library", page.Find(".mode-bar-crumb").InnerText.Trim());
        Assert.Equal(2, page.FindAll(".prefab-group").Count);
        Assert.Equal(8, page.FindAll(".prefab-card").Count);
        Assert.Equal(["Site footer", "Site header"], page.FindAll(".block-card-title").Select(title => title.InnerText).ToArray());
        Assert.All(page.FindAll(".block-card-badge"), badge => Assert.Equal("Theme", badge.InnerText));
        Assert.Empty(page.FindAll(".blocks-empty"));
    }

    [Fact]
    public void EmptyLibraryShowsTheCreateBlockState()
    {
        using var app = new TestApp();
        app.Project.Blocks.Clear();
        var page = app.Render<BlocksWorkspace>();

        Assert.Equal("No blocks yet", page.Find(".blocks-empty .empty-title").InnerText);
        Assert.Equal("Create a block", page.Find(".blocks-empty .empty-action").InnerText);
    }

    [Fact]
    public void ExistingBlocksRenderWithKindUsageAndSelection()
    {
        using var app = new TestApp();
        app.Project.Blocks.Clear();
        var block = new BlockDoc
        {
            Name = "Promo strip",
            Description = "A loud little banner.",
            Kind = "content",
            Category = "Section",
            Nodes = { PageNode.Create("section", "site-band", "Promo") },
        };
        app.Project.Blocks.Add(block);
        app.Project.SaveBlock(block);

        var page = app.Render<BlocksWorkspace>();
        Assert.Single(page.FindAll(".block-card"));
        Assert.Equal("Promo strip", page.Find(".block-card-title").InnerText);
        Assert.Equal("Section · 0 uses", page.Find(".block-card-sub").InnerText);
        Assert.Equal("Content", page.Find(".block-card-badge").InnerText);

        page.Click(".block-card");
        Assert.Equal(block.Id, app.Session.EditingBlockId);
        page = app.Render<BlocksWorkspace>();
        Assert.NotNull(page.Find(".design-bar"));
    }

    [Fact]
    public void PrefabCreatesAThemeBlockAndNewBlockOpensPrompt()
    {
        using var app = new TestApp();
        int before = app.Project.Blocks.Count;
        var page = app.Render<BlocksWorkspace>();

        page.Click(page.FindByText("Simple header"));

        Assert.Equal(before + 1, app.Project.Blocks.Count);
        var block = app.Project.Blocks[^1];
        Assert.Equal("Simple header", block.Name);
        Assert.Equal("theme", block.Kind);
        Assert.Equal("Navigation", block.Category);
        Assert.Equal("header", block.Nodes[0].Tag);
        Assert.Contains("site-header", block.Nodes[0].ClassName);
        Assert.Equal(block.Id, app.Session.EditingBlockId);
        Assert.True(File.Exists(Path.Combine(app.Project.BlocksFolder, Ids.Slug(block.Id) + ".json")));

        app.Session.CloseBlock();
        page = app.Render<BlocksWorkspace>();
        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "New block"));
        Assert.NotNull(app.Session.Prompt);
    }
}
