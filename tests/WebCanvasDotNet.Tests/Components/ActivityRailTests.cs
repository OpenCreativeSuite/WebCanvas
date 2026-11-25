using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ActivityRailTests
{
    [Fact]
    public void RendersExactModesShortcutsAndActiveState()
    {
        using var app = new TestApp();
        var page = app.Render<ActivityRail>();
        var buttons = page.FindAll(".rail-btn");
        var expectedLabels = ModeInfo.All.Select(ModeInfo.RailLabel).Concat(["Search", "Theme"]).ToArray();

        Assert.Equal(expectedLabels, page.FindAll(".rail-label").Select(label => label.InnerText).ToArray());
        Assert.Equal(ModeInfo.All.Length + 2, buttons.Count);

        for (int i = 0; i < ModeInfo.All.Length; i++)
        {
            Assert.Equal(ModeInfo.Hint(ModeInfo.All[i]), buttons[i].GetAttribute("title"));
        }

        Assert.Equal("Search or run a command (Ctrl+K)", buttons[^2].GetAttribute("title"));
        Assert.Equal("Dark theme", buttons[^1].GetAttribute("title"));
        Assert.Equal(ModeInfo.Hint(StudioMode.Design), page.Find(".rail-btn.is-active").GetAttribute("title"));
    }

    [Fact]
    public void ModeAndThemeClicksChangeSessionState()
    {
        using var app = new TestApp();

        var page = app.Render<ActivityRail>();
        page.Click(page.FindAll(".rail-btn")[0]);
        Assert.Equal(StudioMode.Content, app.Session.ActiveMode);

        page = app.Render<ActivityRail>();
        Assert.Equal(ModeInfo.Hint(StudioMode.Content), page.Find(".rail-btn.is-active").GetAttribute("title"));

        bool dark = app.Session.Dark;
        page.Click(page.FindAll(".rail-btn")[^1]);
        Assert.Equal(!dark, app.Session.Dark);
        Assert.Equal(!dark, app.Session.Preferences.Dark);

        page = app.Render<ActivityRail>();
        Assert.Equal(dark ? "Dark theme" : "Light theme", page.FindAll(".rail-btn")[^1].GetAttribute("title"));
    }

    [Fact]
    public void SearchOpensTheCommandPalette()
    {
        using var app = new TestApp();
        app.Session.ClosePalette();
        var page = app.Render<ActivityRail>();

        page.Click(page.FindAll(".rail-btn")[^2]);

        Assert.True(app.Session.PaletteOpen);
    }
}
