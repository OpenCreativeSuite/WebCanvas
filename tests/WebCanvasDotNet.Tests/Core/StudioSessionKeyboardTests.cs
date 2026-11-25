using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class StudioSessionKeyboardTests
{
    [Fact]
    public void CopyPasteDuplicateAndDeleteUseTheSelectedNode()
    {
        using var app = new TestApp();
        var page = app.AddPage("Keys", PageNode.Create("h1", null, "Title"));
        app.Session.SelectPage(page.Id);
        app.Session.SelectNode(page.Nodes[0].Id);

        app.Session.HandleKey("Ctrl+C");
        app.Session.HandleKey("Ctrl+V");
        Assert.Equal(2, app.Project.Page(page.Id)!.Nodes.Count);

        app.Session.HandleKey("Ctrl+D");
        Assert.Equal(3, app.Project.Page(page.Id)!.Nodes.Count);

        app.Session.HandleKey("Delete");
        Assert.Equal(2, app.Project.Page(page.Id)!.Nodes.Count);

        app.Session.HandleKey("Ctrl+Z");
        Assert.Equal(3, app.Project.Page(page.Id)!.Nodes.Count);
    }

    [Fact]
    public void CutAndPasteMoveTheNode()
    {
        using var app = new TestApp();
        var page = app.AddPage("Keys", PageNode.Create("p", null, "Keep me"));
        app.Session.SelectPage(page.Id);
        app.Session.SelectNode(page.Nodes[0].Id);

        app.Session.HandleKey("Ctrl+X");
        Assert.Empty(app.Project.Page(page.Id)!.Nodes);

        app.Session.HandleKey("Ctrl+V");
        Assert.Single(app.Project.Page(page.Id)!.Nodes);
        Assert.Equal("Keep me", app.Project.Page(page.Id)!.Nodes[0].Text);
    }

    [Fact]
    public void PaletteShortcutFiltersWithTypedKeysAndRunsOnEnter()
    {
        using var app = new TestApp();

        app.Session.HandleKey("Ctrl+K");
        Assert.True(app.Session.PaletteOpen);

        foreach (char key in "Go to Data")
        {
            app.Session.HandleKey(key == ' ' ? "Space" : key.ToString());
        }

        Assert.Equal("Go to Data", app.Session.PaletteQuery);
        Assert.Single(app.Session.PaletteCommands());

        app.Session.HandleKey("Enter");

        Assert.Equal(StudioMode.Data, app.Session.ActiveMode);
        Assert.False(app.Session.PaletteOpen);
        Assert.Equal("", app.Session.PaletteQuery);
    }

    [Fact]
    public void DeleteWithNoSelectionAndTypingGuardsDoNotMutateThePage()
    {
        using var app = new TestApp();
        int before = app.Project.Page(app.Project.HomePage!.Id)!.Nodes.Count;

        app.Session.HandleKey("Delete");
        app.Session.IsTyping = true;
        app.Session.HandleKey("Ctrl+D");
        app.Session.IsTyping = false;

        Assert.Equal(before, app.Project.Page(app.Project.HomePage!.Id)!.Nodes.Count);
    }
}
