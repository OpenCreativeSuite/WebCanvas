using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class CommandPaletteTests
{
    [Fact]
    public void RowsExposeTheModeShortcutsInOrder()
    {
        using var app = new TestApp();
        app.Session.OpenPalette();
        var page = app.Render<CommandPalette>();

        var modeRows = page.FindAll(".palette-row").Take(ModeInfo.All.Length).ToArray();
        Assert.Equal(ModeInfo.All.Length, modeRows.Length);

        for (int i = 0; i < ModeInfo.All.Length; i++)
        {
            Assert.Contains("Go to " + ModeInfo.Label(ModeInfo.All[i]), modeRows[i].InnerText);
            Assert.Contains("Ctrl+" + (i + 1), modeRows[i].InnerText);
        }
    }

    [Fact]
    public void SearchFiltersAndEnterRunsTheActiveCommand()
    {
        using var app = new TestApp();
        app.Session.OpenPalette();
        app.Session.SetPaletteQuery("Go to Data");
        var page = app.Render<CommandPalette>();

        Assert.Single(page.FindAll(".palette-row"));
        Assert.Contains("Go to Data", page.Find(".palette-row").InnerText);

        app.Session.HandleKey("Enter");

        Assert.Equal(StudioMode.Data, app.Session.ActiveMode);
        Assert.False(app.Session.PaletteOpen);
        Assert.Equal("", app.Session.PaletteQuery);
    }

    [Fact]
    public void ClickingARowRunsItAndEmptySearchShowsAnEmptyState()
    {
        using var app = new TestApp();
        app.Session.OpenPalette();
        var page = app.Render<CommandPalette>();

        page.Click(page.FindByText("Go to Automation"));
        Assert.Equal(StudioMode.Automation, app.Session.ActiveMode);
        Assert.False(app.Session.PaletteOpen);

        app.Session.OpenPalette();
        app.Session.SetPaletteQuery("zzzz-no-command");
        page = app.Render<CommandPalette>();
        Assert.Equal("No commands match. Try a page title, a data type, or “build”.", page.Find(".palette-empty").InnerText);
    }
}
