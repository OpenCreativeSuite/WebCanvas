using WebCanvasDotNet.Components;

namespace WebCanvasDotNet.Tests.Components;

public sealed class RichTextEditorTests
{
    [Fact]
    public void ParsesPlainTextIntoParagraphBlocks()
    {
        using var app = new TestApp();
        var page = app.Render<RichTextEditor, RichTextEditorProps>(new RichTextEditorProps
        {
            Value = "First paragraph.\n\nSecond paragraph.",
        });

        Assert.Equal(2, page.FindAll(".rich-editor-input").Count);
        Assert.Equal(["First paragraph.", "Second paragraph."],
            page.FindAll(".rich-editor-input").Select(input => input.Value).ToArray());
        Assert.Equal("Edits are saved as HTML.", page.Find(".rich-editor-hint").InnerText);
    }

    [Fact]
    public void ParsesHeadingsQuotesListsAndCodeIntoPageLikeBlocks()
    {
        using var app = new TestApp();
        var page = app.Render<RichTextEditor, RichTextEditorProps>(new RichTextEditorProps
        {
            Value = "<h2>Heading</h2><p>Body</p><blockquote>Quote</blockquote><ul><li>A</li><li>B</li></ul><pre>code()</pre>",
        });

        Assert.Equal(5, page.FindAll(".rich-editor-input").Count);
        Assert.Equal(["Heading", "Body", "Quote", "A\nB", "code()"],
            page.FindAll(".rich-editor-input").Select(input => input.Value).ToArray());
        Assert.True(page.FindAll(".rich-editor-input")[0].HasClass("is-h2"));
        Assert.True(page.FindAll(".rich-editor-input")[1].HasClass("is-p"));
        Assert.True(page.FindAll(".rich-editor-input")[2].HasClass("is-quote"));
        Assert.True(page.FindAll(".rich-editor-input")[3].HasClass("is-list"));
        Assert.True(page.FindAll(".rich-editor-input")[4].HasClass("is-code"));
    }

    [Fact]
    public void ToolbarAddsBlocksAndTypingSerialisesHtml()
    {
        using var app = new TestApp();
        string? html = null;
        var page = app.Render<RichTextEditor, RichTextEditorProps>(new RichTextEditorProps
        {
            Value = "<p>Intro</p>",
            OnChange = value => html = value,
        });

        page.Click(page.FindByText("H2"));
        Assert.Equal(2, page.FindAll(".rich-editor-input").Count);
        Assert.True(page.FindAll(".rich-editor-input")[1].HasClass("is-h2"));

        page.Input(page.FindAll(".rich-editor-input")[1], "Title");
        Assert.Contains("<h2>Title</h2>", html);
        Assert.Contains("<p>Intro</p>", html);
    }

    [Fact]
    public void MoveAndDeleteMutateBlockOrder()
    {
        using var app = new TestApp();
        string? html = null;
        var page = app.Render<RichTextEditor, RichTextEditorProps>(new RichTextEditorProps
        {
            Value = "<h2>One</h2><p>Two</p>",
            OnChange = value => html = value,
        });

        page.Click(page.FindAll("[title='Move block down']")[0]);
        Assert.Equal(["Two", "One"],
            page.FindAll(".rich-editor-input").Select(input => input.Value).ToArray());

        page.Click(page.FindAll("[title='Delete block']")[0]);
        Assert.Single(page.FindAll(".rich-editor-input"));
        Assert.Contains("<h2>One</h2>", html);
    }
}
