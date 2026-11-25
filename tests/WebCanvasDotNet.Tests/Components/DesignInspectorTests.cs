using Donkeh.Testing;
using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DesignInspectorTests
{
    [Fact]
    public void PageSettingsExposeExactValuesAndStats()
    {
        using var app = new TestApp();
        var home = app.Project.HomePage!;
        var page = app.Render<DesignInspector>();

        Assert.Equal("Home", page.Find(".inspector-page-title").InnerText);
        Assert.Equal("/", page.Find(".inspector-page-address").InnerText);
        Assert.Equal("Home", FieldInput(page, "Title").Value);
        Assert.Equal("", FieldInput(page, "Address").Value);
        Assert.Equal("", FieldInput(page, "Menu label").Value);
        Assert.Equal("", FieldControl(page, "Description").Value);
        Assert.True(page.Find(".toggle .switch").HasClass("is-on"));

        var stats = page.FindAll(".meta-pair");
        Assert.Equal(home.Walk().Count().ToString(), stats[0].Find(".meta-value").InnerText);
        Assert.Equal(home.Nodes.Count.ToString(), stats[1].Find(".meta-value").InnerText);
        Assert.Equal("index.html", stats[2].Find(".meta-value").InnerText);
    }

    [Fact]
    public void EditingTitleAndNodeTextPersistsToTheProject()
    {
        using var app = new TestApp();
        var home = app.Project.HomePage!;
        var heading = home.Walk().First(node => node.Tag == "h1");
        app.Session.SelectNode(heading.Id);
        var page = app.Render<DesignInspector>();
        var textArea = FieldControl(page, "Text");

        Assert.Equal(heading.Text, textArea.Value);
        page.Input(textArea, "A different headline");
        Assert.Equal("A different headline", heading.Text);

        app.Session.SelectNode(null);
        page = app.Render<DesignInspector>();
        page.Input(FieldInput(page, "Title"), "Start");
        Assert.Equal("Start", home.Title);

        var saved = WcProject.Open(app.Project.Root).HomePage!;
        Assert.Equal("Start", saved.Title);
        Assert.Equal("A different headline", saved.Walk().First(node => node.Id == heading.Id).Text);
    }

    [Fact]
    public void HtmlNodeOffersTheDedicatedEditor()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Custom</p>",
            new Dictionary<string, string>());
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.SelectNode(node.Id);
        var page = app.Render<DesignInspector>();

        Assert.Contains("HTML block", page.Find(".inspector-body").InnerText);

        page.Click(page.FindByText("Open HTML editor"));

        Assert.Equal(StudioMode.Html, app.Session.ActiveMode);
        Assert.Single(app.Session.Tabs, tab => tab.Mode == StudioMode.Html);
    }
    [Fact]
    public void ContainerChildListSelectsTheClickedChild()
    {
        using var app = new TestApp();
        var main = app.Project.HomePage!.Nodes.First(node => node.Tag == "main");
        app.Session.SelectNode(main.Id);
        var page = app.Render<DesignInspector>();

        Assert.Equal(main.Children!.Count, page.FindAll(".node-child-row").Count);

        page.Click(page.FindAll(".node-child-main")[0]);
        Assert.Equal(main.Children[0].Id, app.Session.SelectedNodeId);
    }

    private static TestElement FieldControl(ComponentTestHarness page, string label) =>
        page.FindAll(".field").First(field => field.Find(".field-label").InnerText == label).Find(".text-input");

    private static TestElement FieldInput(ComponentTestHarness page, string label) => FieldControl(page, label);
}


