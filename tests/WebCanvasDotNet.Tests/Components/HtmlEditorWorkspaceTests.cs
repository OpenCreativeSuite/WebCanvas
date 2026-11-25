using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class HtmlEditorWorkspaceTests
{
    [Fact]
    public void RendersHtmlCssAndLiveWebviewForTheBlock()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block",
            "<div class=\"custom\"><h2>Hello</h2></div>",
            new Dictionary<string, string>
            {
                ["css"] = ".custom { color: red; }",
                ["width"] = "640",
                ["height"] = "220",
            });
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.OpenHtmlEditor(node);

        var page = app.Render<HtmlEditorWorkspace>();

        Assert.Equal(StudioMode.Html, app.Session.ActiveMode);
        Assert.Contains("HTML block", page.Find(".mode-bar-title").InnerText);
        Assert.Contains("640 x 220 px", page.Find(".preview-address").InnerText);
        Assert.Equal(2, page.FindAll(".html-code-input").Count);
        Assert.Equal("<div class=\"custom\"><h2>Hello</h2></div>", page.FindAll(".html-code-input")[0].Value);
        Assert.Equal(".custom { color: red; }", page.FindAll(".html-code-input")[1].Value);
        Assert.Equal("webview", page.Find(".html-preview-frame").Tag);
        Assert.StartsWith("data:text/html", page.Find(".html-preview-frame").GetAttribute("src"));
    }

    [Fact]
    public void EditingHtmlAndCssUpdatesTheNodeAndRevisionChangesThePreview()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Old</p>",
            new Dictionary<string, string> { ["css"] = "p { color: red; }" });
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.OpenHtmlEditor(node);
        var page = app.Render<HtmlEditorWorkspace>();
        string first = page.Find(".html-preview-frame").GetAttribute("src")!;

        page.Change(page.FindAll(".html-code-input")[0], "<p>New</p>");
        Assert.Equal("<p>New</p>", node.Text);

        page.Change(page.FindAll(".html-code-input")[1], "p { color: blue; }");
        Assert.Equal("p { color: blue; }", StudioSession.HtmlBlockCss(node));

        page.Click(page.FindByText("Update preview"));
        page = app.Render<HtmlEditorWorkspace>();
        Assert.NotEqual(first, page.Find(".html-preview-frame").GetAttribute("src"));
    }

    [Fact]
    public void HtmlBlockInsideAReusableBlockOpensAndSaves()
    {
        using var app = new TestApp();
        var htmlNode = PageNode.CreateWithAttrs("html", "html-block",
            "<div class=\"inside\">Block HTML</div>",
            new Dictionary<string, string> { ["css"] = ".inside { color: teal; }" });
        var block = new BlockDoc
        {
            Name = "HTML block host",
            Kind = "content",
            Category = "Section",
            Nodes = { htmlNode },
        };
        app.Project.Blocks.Add(block);
        app.Project.SaveBlock(block);
        app.Session.OpenBlock(block);
        app.Session.OpenHtmlEditor(htmlNode);

        Assert.Equal(StudioMode.Html, app.Session.ActiveMode);
        Assert.Equal(block.Id, app.Session.EditingBlockId);
        Assert.Same(htmlNode, app.Session.CurrentHtmlNode);

        var page = app.Render<HtmlEditorWorkspace>();
        Assert.Equal("HTML block host", page.Find(".mode-bar-crumb").InnerText.Trim());
        Assert.Equal("Back to block", page.FindByText("Back to block").InnerText);

        page.Change(page.FindAll(".html-code-input")[0], "<div class=\"inside\">Saved HTML</div>");
        page.Change(page.FindAll(".html-code-input")[1], ".inside { color: navy; }");
        page.Click(page.FindByText("Save block"));

        var reloaded = WcProject.Open(app.Project.Root);
        var savedBlock = reloaded.Block(block.Id)!;
        var savedNode = savedBlock.Nodes.Single();

        Assert.Equal("<div class=\"inside\">Saved HTML</div>", savedNode.Text);
        Assert.Equal(".inside { color: navy; }", StudioSession.HtmlBlockCss(savedNode));
    }

    [Fact]
    public void PageHtmlBlockEditsPersistToThePageFile()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Before</p>",
            new Dictionary<string, string> { ["css"] = "p { color: red; }" });
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.OpenHtmlEditor(node);

        app.Session.SetHtmlBlockHtml("<p>After</p>");
        app.Session.SetHtmlBlockCss("p { color: green; }");
        app.Session.SaveHtmlBlock();

        var saved = WcProject.Open(app.Project.Root).HomePage!.Find(node.Id)!;

        Assert.Equal("<p>After</p>", saved.Text);
        Assert.Equal("p { color: green; }", StudioSession.HtmlBlockCss(saved));
    }

    [Fact]
    public void BackToBlockReturnsToTheReusableBlockEditor()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Block</p>",
            new Dictionary<string, string>());
        var block = new BlockDoc { Name = "Host", Kind = "content", Nodes = { node } };
        app.Project.Blocks.Add(block);
        app.Project.SaveBlock(block);
        app.Session.OpenBlock(block);
        app.Session.OpenHtmlEditor(node);
        var page = app.Render<HtmlEditorWorkspace>();

        page.Click(page.FindByText("Back to block"));

        Assert.Equal(StudioMode.Blocks, app.Session.ActiveMode);
        Assert.Equal(block.Id, app.Session.EditingBlockId);
    }
    [Fact]
    public void BackToPageActivatesTheDesignTab()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Block</p>",
            new Dictionary<string, string>());
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.OpenHtmlEditor(node);
        var page = app.Render<HtmlEditorWorkspace>();

        page.Click(page.FindByText("Back to page"));

        Assert.Equal(StudioMode.Design, app.Session.ActiveMode);
        Assert.Equal(app.Project.HomePage.Id, app.Session.SelectedPageId);
    }

    [Fact]
    public void SetHtmlBlockSizeReservesTheMeasuredBox()
    {
        using var app = new TestApp();
        var node = PageNode.CreateWithAttrs("html", "html-block", "<p>Block</p>",
            new Dictionary<string, string>());
        app.Project.HomePage!.Nodes.Add(node);
        app.Project.SavePage(app.Project.HomePage);
        app.Session.OpenHtmlEditor(node);

        app.Session.SetHtmlBlockSize(node.Id, 512, 180);

        Assert.Equal("512", node.Attrs!["width"]);
        Assert.Equal("180", node.Attrs["height"]);
        Assert.Equal(512, StudioSession.HtmlBlockWidth(node));
        Assert.Equal(180, StudioSession.HtmlBlockHeight(node));
    }
}


