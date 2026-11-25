using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class PromptDialogTests
{
    [Fact]
    public void SubmitsTheEditedValueAndCloses()
    {
        using var app = new TestApp();
        string? submitted = null;
        app.Session.OpenPrompt(new PromptRequest
        {
            Title = "Rename page",
            Label = "Page title",
            Value = "Old name",
            Confirm = "Rename",
            OnConfirm = value => submitted = value,
        });

        var page = app.Render<PromptDialog>();
        Assert.Equal("Rename page", page.Find(".modal-title").InnerText);
        Assert.Equal("Page title", page.Find(".modal-sub").InnerText);
        Assert.Equal("Old name", page.Find("input").GetAttribute("value"));
        Assert.Equal("Rename", page.Find(".modal-foot .btn.primary").InnerText);

        page.Input(page.Find("input"), "New name");
        Assert.Equal("New name", app.Session.Prompt!.Value);

        page.Click(page.Find(".modal-foot .btn.primary"));
        Assert.Equal("New name", submitted);
        Assert.Null(app.Session.Prompt);
    }

    [Fact]
    public void CancelDoesNotInvokeTheCallback()
    {
        using var app = new TestApp();
        int calls = 0;
        app.Session.OpenPrompt(new PromptRequest
        {
            Title = "Rename",
            Value = "Old",
            OnConfirm = _ => calls++,
        });

        var page = app.Render<PromptDialog>();
        page.Click(page.FindByText("Cancel"));

        Assert.Equal(0, calls);
        Assert.Null(app.Session.Prompt);
    }

    [Fact]
    public void MultilinePromptsRenderATextArea()
    {
        using var app = new TestApp();
        app.Session.OpenPrompt(new PromptRequest
        {
            Title = "Notes",
            Value = "Line one",
            Multiline = true,
        });

        var page = app.Render<PromptDialog>();

        Assert.Empty(page.FindAll("input"));
        Assert.Equal("Line one", page.Find("textarea").Value);
    }
}
