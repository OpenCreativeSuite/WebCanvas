using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class CreateProjectDialogTests
{
    [Fact]
    public void InitialStateReflectsSessionDefaults()
    {
        using var app = new TestApp();
        app.Session.OpenCreateDialog();
        var page = app.Render<CreateProjectDialog>();

        Assert.Equal("Create a project", page.Find(".modal-title").InnerText);
        Assert.Equal("", page.Find(".modal-body .text-input").Value);
        Assert.Equal(app.Session.CreateParent, page.Find(".readonly-field").InnerText);
        Assert.True(page.FindAll(".template-option")[0].HasClass("is-selected"));
        Assert.False(page.FindAll(".template-option")[1].HasClass("is-selected"));
    }

    [Fact]
    public void TemplateChoiceAndNameInputUpdateSessionState()
    {
        using var app = new TestApp();
        app.Session.OpenCreateDialog();
        var page = app.Render<CreateProjectDialog>();

        page.Click(page.FindAll(".template-option")[1]);
        Assert.False(app.Session.CreateStarter);

        page = app.Render<CreateProjectDialog>();
        Assert.True(page.FindAll(".template-option")[1].HasClass("is-selected"));
        Assert.Empty(page.FindAll(".template-option")[0].FindAll(".template-check"));

        page.Input(page.Find(".modal-body .text-input"), "Aster Two");
        Assert.Equal("Aster Two", app.Session.CreateName);

        page.Click(page.FindByText("Cancel"));
        Assert.False(app.Session.CreateOpen);
    }

    [Fact]
    public void CreateProjectWritesANewProjectAndClosesTheDialog()
    {
        using var app = new TestApp();
        app.Session.OpenCreateDialog();
        app.Session.CreateName = "Second Site";
        app.Session.CreateParent = app.Root;
        app.Session.CreateStarter = false;
        var page = app.Render<CreateProjectDialog>();

        page.Click(page.FindByText("Create project"));

        Assert.False(app.Session.CreateOpen);
        Assert.Equal("Second Site", app.Session.Project!.Name);
        Assert.True(Directory.Exists(Path.Combine(app.Root, "Second Site")));
        Assert.Equal("blank", app.Session.Project.Manifest.Template);
    }
}
