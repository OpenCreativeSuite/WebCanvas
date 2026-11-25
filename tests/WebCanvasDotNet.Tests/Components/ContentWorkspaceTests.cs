using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ContentWorkspaceTests
{
    [Fact]
    public void NoSchemaShowsTheExactEmptyState()
    {
        using var app = new TestApp();
        app.Session.OpenContentRecords();
        var page = app.Render<ContentWorkspace>();

        Assert.Equal("Your content lives here.", page.Find(".workspace-empty-title").InnerText);
        Assert.Contains("Create a data type", page.Find(".btn.primary").InnerText);
    }

    [Fact]
    public void RecordListAndEditorRenderExactValues()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var entity = app.AddEntity(schema, ("title", "First story"), ("slug", "first-story"), ("body", "Body copy"));
        app.Session.SelectSchema("Article");
        app.Session.SelectEntity(entity.Id);

        var page = app.Render<ContentWorkspace>();

        Assert.Equal("Articles", page.Find(".entity-list-title").InnerText);
        Assert.Equal("1 records", page.Find(".entity-list-count").InnerText);
        Assert.Single(page.FindAll(".entity-row"));
        Assert.Equal("First story", page.Find(".entity-row-title").InnerText);
        Assert.Equal("First story", page.Find(".entity-title-input").Value);
        Assert.Equal(entity.Id, page.Find(".entity-id").InnerText);
        Assert.Contains("Body copy", page.Find(".entity-form").InnerText);
        Assert.NotNull(page.Find(".rich-editor"));
        Assert.Equal(6, page.FindAll(".rich-editor-tool").Count);
    }

    [Fact]
    public void FilterCapsulesSitAboveTheRecordList()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text },
            new DataField { Name = "publishedAt", Type = FieldTypes.Date });
        app.AddEntity(schema, ("title", "First story"));
        app.Session.SelectSchema("Article");
        app.Session.SelectEntity(null);

        var page = app.Render<ContentWorkspace>();
        var filters = page.Find(".filter-row");
        var body = page.Find(".entity-list-body");

        Assert.Equal(3, filters.FindAll(".filter-chip").Count);
        Assert.True(filters.Bounds.Bottom <= body.Bounds.Y + 0.5f);
    }
    [Fact]
    public void SearchFiltersRowsAndMapButtonChangesView()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text },
            new DataField { Name = "publishedAt", Type = FieldTypes.Date });
        app.AddEntity(schema, ("title", "First story"));
        app.AddEntity(schema, ("title", "Second story"));
        app.Session.SelectSchema("Article");
        app.Session.SelectEntity(null);
        app.Session.EntitySearch = "Second";
        app.Session.MarkChanged();

        var page = app.Render<ContentWorkspace>();
        Assert.Single(page.FindAll(".entity-row"));
        Assert.Equal("Second story", page.Find(".entity-row-title").InnerText);
        Assert.Equal("1 matching", page.Find(".entity-list-count").InnerText);

        app.Session.EntitySearch = "nothing";
        app.Session.MarkChanged();
        page = app.Render<ContentWorkspace>();
        Assert.StartsWith("No records match “nothing”.", page.Find(".entity-list-empty").InnerText);

        app.Session.OpenContentRecords();
        page = app.Render<ContentWorkspace>();
        page.Click(page.FindByText("Map"));
        Assert.True(app.Session.ContentMapOpen);
    }
}





