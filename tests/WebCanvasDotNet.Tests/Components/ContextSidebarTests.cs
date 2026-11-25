using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ContextSidebarTests
{
    [Fact]
    public void DesignSidebarListsExactPagesAndSelectsThem()
    {
        using var app = new TestApp();
        var team = app.AddPage("Team", PageNode.Create("h1", null, "Team"));
        var page = app.Render<ContextSidebar>();

        Assert.Equal("Design", page.Find(".sidebar-title").InnerText);
        Assert.Equal(["Home", "Team"], page.FindAll(".side-row-label").Select(row => row.InnerText).ToArray());
        Assert.Equal("Home", page.Find(".side-row.is-active .side-row-label").InnerText);

        page.Click(page.FindByText("Team"));
        Assert.Equal(team.Id, app.Session.SelectedPageId);

        page = app.Render<ContextSidebar>();
        Assert.Equal("Team", page.Find(".side-row.is-active .side-row-label").InnerText);
    }

    [Fact]
    public void DesignSidebarNewPageAndCollapseUseSessionActions()
    {
        using var app = new TestApp();
        var page = app.Render<ContextSidebar>();

        page.Click(page.Find("[title='New page']"));
        Assert.NotNull(app.Session.Prompt);
        Assert.Equal("New page", app.Session.Prompt!.Title);

        page.Click(page.Find("[title='Hide sidebar (Ctrl+B)']"));
        Assert.False(app.Session.ShowSidebar);
        Assert.False(app.Session.Preferences.ShowSidebar);
    }

    [Fact]
    public void DataAndBlocksModesExposeTheirExactRows()
    {
        using var app = new TestApp();
        var author = app.AddSchema("Author");
        var article = app.AddSchema("Article");
        app.Session.OpenMode(StudioMode.Data);
        var page = app.Render<ContextSidebar>();

        Assert.Equal("Data model", page.Find(".sidebar-title").InnerText);
        Assert.Equal(app.Project.Schemas.Select(schema => schema.Name), page.FindAll(".side-row-label").Select(row => row.InnerText).ToArray());

        page.Click(page.FindByText("Author"));
        Assert.Equal(author.Name, app.Session.SelectedSchemaName);

        var block = new BlockDoc { Name = "Promo", Kind = "content", Category = "Section" };
        app.Project.Blocks.Add(block);
        app.Project.SaveBlock(block);
        app.Session.OpenMode(StudioMode.Blocks);
        page = app.Render<ContextSidebar>();

        Assert.Contains("Promo", page.FindAll(".side-row-label").Select(row => row.InnerText));
        page.Click(page.FindByText("Promo"));
        Assert.Equal(block.Id, app.Session.EditingBlockId);
    }
}

