using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class WelcomeTests
{
    [Fact]
    public void StartupHubShowsExactActionsAndDecorativePreview()
    {
        using var app = new TestApp();
        app.Session.CloseProject();
        var page = app.Render<Welcome>();

        Assert.Equal("The quiet way to build a website.", page.Find(".welcome-title").InnerText);
        Assert.Equal(["Create project", "Open project"],
            page.FindAll(".welcome-actions .btn").Select(button => button.InnerText.Trim()).ToArray());
        Assert.Contains("Explore the Aster & Oak sample site", page.Find(".welcome-sample").InnerText);
        Assert.NotNull(page.Find(".welcome-preview-window"));
    }

    [Fact]
    public void RecentProjectsOpenTheirFolder()
    {
        using var app = new TestApp();
        string root = app.Project.Root;
        app.Session.CloseProject();
        app.Session.Preferences.RecentProjects = [root];
        var page = app.Render<Welcome>();

        Assert.Single(page.FindAll(".welcome-recent-row"));
        Assert.Equal("Test project", page.Find(".welcome-recent-name").InnerText);
        Assert.Contains(root, page.Find(".welcome-recent-path").InnerText);

        page.Click(".welcome-recent-row");
        Assert.NotNull(app.Session.Project);
        Assert.Equal(root, app.Session.Project!.Root);
    }

    [Fact]
    public void CreateProjectButtonOpensTheDialog()
    {
        using var app = new TestApp();
        app.Session.CloseProject();
        var page = app.Render<Welcome>();

        page.Click(page.FindByText("Create project"));

        Assert.True(app.Session.CreateOpen);
    }
}
