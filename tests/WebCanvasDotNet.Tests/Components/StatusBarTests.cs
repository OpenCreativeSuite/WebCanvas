using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class StatusBarTests
{
    [Fact]
    public void ShowsExactProjectSaveCountsModeAndViewport()
    {
        using var app = new TestApp();
        var page = app.Render<StatusBar>();
        var items = page.FindAll(".status-item");

        Assert.Equal(app.Project.Name, items[0].InnerText);
        Assert.Equal("All changes saved", items[1].InnerText);
        Assert.Contains("1 page", items[2].InnerText);
        Assert.Contains("0 records", items[2].InnerText);
        Assert.Equal("Design", items[3].InnerText);
        Assert.Equal("Home", items[4].InnerText);
        Assert.Contains("Desktop", items[5].InnerText);
        Assert.Contains("60%", items[5].InnerText);
    }

    [Fact]
    public void CountsAndContextFollowContentAndModeChanges()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        app.AddEntity(schema, ("title", "First story"));
        app.Session.OpenMode(StudioMode.Data);
        var page = app.Render<StatusBar>();
        var items = page.FindAll(".status-item");

        Assert.Contains("1 record", items[2].InnerText);
        Assert.Equal("Data", items[3].InnerText);
        Assert.Equal("Article", items[4].InnerText);
        Assert.Equal(5, items.Count);
    }
}
