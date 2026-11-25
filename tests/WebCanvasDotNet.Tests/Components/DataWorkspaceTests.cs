using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DataWorkspaceTests
{
    [Fact]
    public void NoSchemaShowsTheExactEmptyState()
    {
        using var app = new TestApp();
        var page = app.Render<DataWorkspace>();

        Assert.Equal("Model the things your site is made of.", page.Find(".workspace-empty-title").InnerText);
        Assert.Contains("Create a data type", page.Find(".btn.primary").InnerText);
    }

    [Fact]
    public void SchemaHeaderFieldsAndRoutingRenderExactValues()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article",
            new DataField { Name = "title", Type = FieldTypes.Text, Required = true, Label = "Title" },
            new DataField { Name = "slug", Type = FieldTypes.Text, Label = "Slug" },
            new DataField { Name = "body", Type = FieldTypes.RichText, Label = "Body" });
        var detail = app.AddPage("Article detail", PageNode.Create("h1", null, "{{title}}"));
        schema.DetailPageId = detail.Id;
        app.Project.SaveSchema(schema);

        var page = app.Render<DataWorkspace>();

        Assert.Equal("Article", page.Find(".schema-title").InnerText);
        Assert.Contains("0 records", page.Find(".schema-meta").InnerText);
        Assert.Contains("3 fields", page.Find(".schema-meta").InnerText);
        Assert.Contains("Public", page.Find(".schema-meta").InnerText);
        Assert.Equal(["Title", "Slug", "Body"], page.FindAll(".field-card-title").Select(title => title.FindAll("span")[0].InnerText).ToArray());
        Assert.Contains("Required", page.FindAll(".field-card")[0].InnerText);
        Assert.Equal("title", page.FindAll(".field-card")[0].Find(".mono").InnerText);
        Assert.Equal("Text", page.FindAll(".field-card")[0].FindAll(".field-card-sub span")[^1].InnerText);

        var selectedOption = page.FindAll("option").First(option => option.HasAttribute("selected"));
        Assert.Equal(detail.Id, selectedOption.GetAttribute("value"));
        Assert.Contains("/articles/{slug}", page.FindAll(".field-hint")[^1].InnerText);
    }

    [Fact]
    public void AddFieldMenuAddsTheChosenTypeToTheSchema()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var page = app.Render<DataWorkspace>();

        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "Add field"));
        Assert.True(app.Session.AddFieldMenuOpen);

        page = app.Render<DataWorkspace>();
        Assert.Equal(FieldTypes.All.Length, page.FindAll(".field-type-option").Count);

        page.Click(page.FindAll(".field-type-option")[0]);
        Assert.False(app.Session.AddFieldMenuOpen);
        Assert.Equal(4, schema.Fields.Count);
        Assert.Equal(FieldTypes.Text, schema.Fields[^1].Type);
    }
}

