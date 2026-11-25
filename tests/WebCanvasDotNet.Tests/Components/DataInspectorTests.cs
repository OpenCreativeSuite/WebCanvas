using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DataInspectorTests
{
    [Fact]
    public void NoSchemaShowsTheExactEmptyState()
    {
        using var app = new TestApp();
        var page = app.Render<DataInspector>();

        Assert.Equal("No type selected", page.Find(".empty-title").InnerText);
        Assert.Equal("Choose a data type to inspect its fields and relationships.", page.Find(".empty-copy").InnerText);
    }

    [Fact]
    public void OutgoingAndIncomingRelationshipsHaveExactRows()
    {
        using var app = new TestApp();
        app.AddSchema("Author", new DataField { Name = "name", Type = FieldTypes.Text });
        app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text },
            new DataField { Name = "author", Type = FieldTypes.Relation, Target = "Author" });

        var page = app.Render<DataInspector>();
        Assert.Equal("Article", page.Find(".node-summary-title").InnerText);
        Assert.Single(page.FindAll(".rel-row"));
        Assert.Equal("Author", page.Find(".rel-title").InnerText);
        Assert.Equal("author → Author", page.Find(".rel-sub").InnerText);

        app.Session.SelectSchema("Author");
        page = app.Render<DataInspector>();
        Assert.Equal("Article", page.Find(".rel-row .rel-title").InnerText);
        Assert.Equal("1 field points here", page.Find(".rel-row .rel-sub").InnerText);
    }

    [Fact]
    public void SelectingAFieldSwitchesToFieldSettings()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text, Label = "Headline", Required = true },
            new DataField { Name = "author", Type = FieldTypes.Relation, Target = "Article" });
        var page = app.Render<DataInspector>();

        page.Click(page.FindAll(".rel-row")[0]);
        Assert.Equal("author", app.Session.CurrentField!.Name);

        page = app.Render<DataInspector>();
        Assert.Equal("Author", page.Find(".node-summary-title").InnerText);
        Assert.Equal("author", page.Find(".node-summary-sub").InnerText);
        Assert.Contains("Target type", page.Find(".inspector-body").InnerText);
        Assert.NotNull(page.Find(".select-input"));
        Assert.NotNull(page.FindAll(".btn").First(button => button.InnerText.Trim() == "Delete field"));
    }
}
