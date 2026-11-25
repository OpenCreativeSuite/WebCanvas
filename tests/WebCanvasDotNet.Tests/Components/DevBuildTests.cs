using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevBuildTests
{
    [Fact]
    public void InitialOptionsAndEmptyConsoleReflectSession()
    {
        using var app = new TestApp();
        var page = app.Render<DevBuild>();

        Assert.Equal(4, page.FindAll(".option-row").Count);
        Assert.All(page.FindAll(".option-row .switch"), toggle => Assert.True(toggle.HasClass("is-on")));
        Assert.Equal("build/site", page.Find(".build-options .text-input").Value);
        Assert.Equal("Build output will appear here. The first build usually takes under a second.", page.Find(".build-console.is-empty").InnerText);
        Assert.Empty(page.FindAll(".build-result"));
    }

    [Fact]
    public void OptionControlsMutateBuildOptions()
    {
        using var app = new TestApp();
        var page = app.Render<DevBuild>();

        page.Click(page.FindAll(".option-row .toggle")[0]);
        Assert.False(app.Session.BuildOptions.PrettyUrls);

        page = app.Render<DevBuild>();
        Assert.False(page.FindAll(".option-row .switch")[0].HasClass("is-on"));

        page.Change(page.Find(".build-options .text-input"), "publish");
        Assert.Equal("publish", app.Session.BuildOptions.OutputFolder);
    }

    [Fact]
    public void BuildRendersResultStatsConsoleAndWarnings()
    {
        using var app = new TestApp();
        var about = app.AddPage("About", PageNode.Create("h1", null, "About"));
        var page = app.Render<DevBuild>();

        page.Click(page.Find(".inline-actions .btn.primary"));
        page = app.Render<DevBuild>();

        var result = app.Session.BuildResult!;
        Assert.True(result.Success);
        Assert.StartsWith("Built " + result.Pages + " pages in", page.Find(".build-result-title").InnerText);
        Assert.Contains(result.Entities + " records", page.Find(".build-result-stats").InnerText);
        Assert.Contains(result.Assets + " assets", page.Find(".build-result-stats").InnerText);
        Assert.Equal(result.Log.Count, page.FindAll(".console-line").Count);
        Assert.Equal(result.OutputPath, page.Find(".build-result-sub").InnerText);
        Assert.Contains(about.Title, page.Find(".notice-list").InnerText);
        Assert.NotNull(page.Find(".inline-actions .btn.secondary"));
    }
}


