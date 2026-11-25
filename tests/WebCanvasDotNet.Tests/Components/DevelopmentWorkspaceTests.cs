using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevelopmentWorkspaceTests
{
    [Theory]
    [InlineData(DevSection.Overview, "Overview")]
    [InlineData(DevSection.Theme, "Theme")]
    [InlineData(DevSection.Plugins, "Plugins")]
    [InlineData(DevSection.Api, "APIs")]
    [InlineData(DevSection.Build, "Build")]
    public void EachSectionMountsItsOwnSubview(DevSection section, string title)
    {
        using var app = new TestApp();
        app.Session.SelectDevSection(section);
        var page = app.Render<DevelopmentWorkspace>();

        Assert.Equal("Development", page.Find(".mode-bar-title").InnerText);
        Assert.Equal(ModeInfo.DevLabel(section), page.Find(".mode-bar-crumb").InnerText.Trim());
        Assert.Single(page.FindAll(".dev-page"));

        if (section == DevSection.Overview)
        {
            Assert.Equal(app.Project.Name, page.Find(".dev-title").InnerText);
        }
        else
        {
            Assert.Equal(title, page.Find(".dev-title").InnerText);
        }
    }
}
