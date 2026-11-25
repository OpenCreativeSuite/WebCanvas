using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ContentMapTests
{
    [Fact]
    public void EmptyProjectShowsTheExactStartingState()
    {
        using var app = new TestApp();
        var page = app.Render<ContentMap>();

        Assert.Equal("Your content map starts here", page.Find(".empty-title").InnerText);
        Assert.Equal("Create a data type", page.Find(".empty-action").InnerText);
        Assert.Empty(page.FindAll(".graph-node"));
    }

    [Fact]
    public void BuildsNodesAndEdgesFromSchemasAndRelations()
    {
        using var app = new TestApp();
        app.AddSchema("Author", new DataField { Name = "name", Type = FieldTypes.Text });
        app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text },
            new DataField { Name = "author", Type = FieldTypes.Relation, Target = "Author" });

        var page = app.Render<ContentMap>();

        Assert.Equal(2, page.FindAll(".graph-node").Count);
        Assert.Equal(["Article", "Author"], page.FindAll(".graph-node-title").Select(node => node.InnerText).ToArray());
        Assert.Single(page.FindAll(".graph-edge"));
        Assert.Equal("author", page.Find(".graph-edge-label").InnerText);
        Assert.Contains("2 types", page.Find(".map-sub").InnerText);
        Assert.Contains("1 relationships", page.Find(".map-sub").InnerText);

        page.Click(page.FindByText("Author"));
        Assert.Equal("Author", app.Session.SelectedSchemaName);
    }

    [Fact]
    public void RelationBadgesAndRecordCountsAreProjectedOntoNodes()
    {
        using var app = new TestApp();
        var author = app.AddSchema("Author", new DataField { Name = "name", Type = FieldTypes.Text });
        app.AddEntity(author, ("name", "Ada"));
        app.AddSchema("Article", new DataField { Name = "author", Type = FieldTypes.Relation, Target = "Author" });

        var page = app.Render<ContentMap>();
        var node = page.FindAll(".graph-node").First(item => item.InnerText.Contains("Author"));

        Assert.Contains("1 records · 1 fields", node.InnerText);
        Assert.Contains("1 rel", page.FindAll(".graph-node-badge").First(badge => badge.InnerText.Contains("1 rel")).InnerText);
    }
}
