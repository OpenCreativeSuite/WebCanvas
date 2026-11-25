using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ModeMenuTests
{
    [Fact]
    public void ListsEveryModeWithItsShortcutInOrder()
    {
        using var app = new TestApp();
        app.Session.ToggleModeMenu();
        var page = app.Render<ModeMenu>();

        Assert.Equal(ModeInfo.All.Select(ModeInfo.Label), page.FindAll(".side-row-label").Select(label => label.InnerText));
        Assert.Equal(ModeInfo.All.Select((_, index) => "Ctrl+" + (index + 1)), page.FindAll(".mode-menu-hint").Select(hint => hint.InnerText));
    }

    [Fact]
    public void ChoosingAModeSwitchesWorkspaceAndClosesTheMenu()
    {
        using var app = new TestApp();
        app.Session.ToggleModeMenu();
        var page = app.Render<ModeMenu>();

        page.Click(page.FindByText("Data"));

        Assert.Equal(StudioMode.Data, app.Session.ActiveMode);
        Assert.False(app.Session.ShowModeMenu);
    }

    [Fact]
    public void OverlayClickClosesWithoutSwitching()
    {
        using var app = new TestApp();
        StudioMode before = app.Session.ActiveMode;
        app.Session.ToggleModeMenu();
        var page = app.Render<ModeMenu>();

        page.Click(".mode-menu-overlay");

        Assert.False(app.Session.ShowModeMenu);
        Assert.Equal(before, app.Session.ActiveMode);
    }
}
