using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevOverviewTests
{
    [Fact]
    public void HealthRowsAndMetaGridReflectProjectState()
    {
        using var app = new TestApp();
        var page = app.Render<DevOverview>();

        var rows = page.FindAll(".health-row");
        Assert.Equal(6, rows.Count);
        Assert.Equal(["Pages", "Data model", "Automation", "Plugins", "Site URL", "Output"],
            rows.Select(row => row.Find(".health-label").InnerText).ToArray());
        Assert.Contains("1 pages", rows[0].Find(".health-detail").InnerText);
        Assert.Contains("1 in navigation", rows[0].Find(".health-detail").InnerText);
        Assert.Contains("0 types", rows[1].Find(".health-detail").InnerText);
        Assert.Contains("0 records", rows[1].Find(".health-detail").InnerText);
        Assert.Equal("No handlers yet", rows[2].Find(".health-detail").InnerText);
        Assert.Equal("None installed", rows[3].Find(".health-detail").InnerText);
        Assert.Equal("Set it before publishing a sitemap", rows[4].Find(".health-detail").InnerText);
        Assert.Equal("Build/site is ready when you are", rows[5].Find(".health-detail").InnerText);

        var meta = page.FindAll(".meta-value").Select(value => value.InnerText).ToArray();
        Assert.Equal(app.Project.Root, meta[0]);
        Assert.Equal("blank", meta[1]);
        Assert.Equal("Not set", meta[3]);
        Assert.Contains("0 types", meta[4]);
        Assert.Contains("0 records", meta[4]);
        Assert.Equal("None", meta[5]);
    }

    [Fact]
    public void ScaleButtonsReflectPreferenceAndUpdateIt()
    {
        using var app = new TestApp();
        var page = app.Render<DevOverview>();

        Assert.Equal("Comfortable", page.Find(".scale-btn.is-active").InnerText);
        page.Click(page.FindByText("Large"));
        Assert.Equal(130, app.Session.Preferences.UiScale);

        page = app.Render<DevOverview>();
        Assert.Equal("Large", page.Find(".scale-btn.is-active").InnerText);

        page.Click(page.FindByText("Default"));
        Assert.Equal(100, app.Session.Preferences.UiScale);
    }
}

