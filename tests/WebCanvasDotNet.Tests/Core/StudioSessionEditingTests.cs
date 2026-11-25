using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class StudioSessionEditingTests
{
    [Fact]
    public void CopyPasteDuplicateDeleteAndUndoWorkOnTheSelectedNode()
    {
        using var app = new TestApp();
        var page = app.AddPage("Editing", PageNode.Create("h1", "page-title", "Original"));
        app.Session.SelectPage(page.Id);
        app.Session.SelectNode(page.Nodes[0].Id);

        app.Session.CopyNode();
        app.Session.PasteNode();

        Assert.Equal(2, app.Project.Page(page.Id)!.Nodes.Count);
        Assert.Equal("Original", app.Project.Page(page.Id)!.Nodes[1].Text);

        app.Session.Undo();
        Assert.Single(app.Project.Page(page.Id)!.Nodes);

        app.Session.Redo();
        Assert.Equal(2, app.Project.Page(page.Id)!.Nodes.Count);

        app.Session.SelectNode(app.Project.Page(page.Id)!.Nodes[1].Id);
        app.Session.DeleteNode();
        Assert.Single(app.Project.Page(page.Id)!.Nodes);
    }

    [Fact]
    public void CutAndPasteMoveABlockWithoutDuplicatingIt()
    {
        using var app = new TestApp();
        var page = app.AddPage("Editing", PageNode.Create("p", null, "Keep me"));
        app.Session.SelectPage(page.Id);
        app.Session.SelectNode(page.Nodes[0].Id);

        app.Session.CutNode();
        Assert.Empty(app.Project.Page(page.Id)!.Nodes);

        app.Session.PasteNode();
        Assert.Single(app.Project.Page(page.Id)!.Nodes);
        Assert.Equal("Keep me", app.Project.Page(page.Id)!.Nodes[0].Text);
    }
}
