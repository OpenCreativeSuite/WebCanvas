using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class TitleBarTests
{
    [Fact]
    public void ShowsExactBrandProjectAndPanelState()
    {
        using var app = new TestApp();
        var page = app.Render<TitleBar>();

        Assert.Equal("WebCanvas", page.Find(".brand-name").InnerText);
        Assert.Equal(app.Project.Name, page.Find(".project-name").InnerText);
        Assert.Equal(app.Project.Root, page.Find(".project-pill").GetAttribute("title"));
        Assert.Equal("Search or run a command", page.Find(".command-label").InnerText);
        Assert.Equal(2, page.FindAll(".titlebar-tool.is-active").Count);
        Assert.Equal(3, page.FindAll(".window-btn").Count);
    }

    [Fact]
    public void PanelTogglesUpdateSessionAndActiveClasses()
    {
        using var app = new TestApp();
        var page = app.Render<TitleBar>();

        page.Click(page.Find("[title='Hide sidebar (Ctrl+B)']"));
        Assert.False(app.Session.ShowSidebar);
        Assert.False(app.Session.Preferences.ShowSidebar);

        page = app.Render<TitleBar>();
        Assert.Single(page.FindAll(".titlebar-tool.is-active"));
        Assert.Equal("Show sidebar (Ctrl+B)", page.FindAll(".titlebar-tool")[0].GetAttribute("title"));

        page.Click(page.Find("[title='Hide inspector (Ctrl+Shift+I)']"));
        Assert.False(app.Session.ShowInspector);
        page = app.Render<TitleBar>();
        Assert.Empty(page.FindAll(".titlebar-tool.is-active"));
    }

    [Fact]
    public void ThemeButtonChangesLabelAndPreference()
    {
        using var app = new TestApp();
        bool dark = app.Session.Dark;
        var page = app.Render<TitleBar>();
        var theme = page.FindAll(".titlebar-tool")[^1];

        Assert.Equal(dark ? "Switch to light theme" : "Switch to dark theme", theme.GetAttribute("title"));
        page.Click(theme);
        Assert.Equal(!dark, app.Session.Preferences.Dark);

        page = app.Render<TitleBar>();
        Assert.Equal(dark ? "Switch to dark theme" : "Switch to light theme", page.FindAll(".titlebar-tool")[^1].GetAttribute("title"));
    }

    [Fact]
    public void NoProjectStateUsesExactLabels()
    {
        using var app = new TestApp();
        app.Session.CloseProject();
        var page = app.Render<TitleBar>();

        Assert.Equal("No project open", page.Find(".project-name").InnerText);
        Assert.Equal("Open a project", page.Find(".project-pill").GetAttribute("title"));
    }
}
