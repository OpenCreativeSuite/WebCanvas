using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ConfirmDialogTests
{
    [Fact]
    public void AcceptRunsTheStoredActionAndClearsTheRequest()
    {
        using var app = new TestApp();
        int calls = 0;
        app.Session.AskConfirm(new ConfirmRequest
        {
            Title = "Delete the thing?",
            Message = "This cannot be undone.",
            Confirm = "Delete it",
            OnConfirm = () => calls++,
        });

        var page = app.Render<ConfirmDialog>();
        Assert.Equal("Delete the thing?", page.Find(".modal-title").InnerText);
        Assert.Equal("This cannot be undone.", page.Find(".modal-sub").InnerText);
        Assert.Equal("Delete it", page.Find(".modal-foot .btn.danger").InnerText);
        Assert.NotNull(page.Find(".modal-icon.is-danger"));
        Assert.Equal(2, page.FindAll(".modal-foot .btn").Count);

        page.Click(page.Find(".modal-foot .btn.danger"));

        Assert.Equal(1, calls);
        Assert.Null(app.Session.Confirm);
    }

    [Fact]
    public void CancelClosesWithoutRunningTheAction()
    {
        using var app = new TestApp();
        int calls = 0;
        app.Session.AskConfirm(new ConfirmRequest
        {
            Title = "Discard?",
            Confirm = "Discard",
            OnConfirm = () => calls++,
        });

        var page = app.Render<ConfirmDialog>();
        page.Click(page.FindByText("Cancel"));

        Assert.Equal(0, calls);
        Assert.Null(app.Session.Confirm);
    }

    [Fact]
    public void InformationalDialogsUsePrimaryStyleAndHideCancel()
    {
        using var app = new TestApp();
        app.Session.AskConfirm(new ConfirmRequest
        {
            Title = "Heads up",
            Message = "Informational only.",
            Confirm = "Got it",
            Danger = false,
            ShowCancel = false,
        });

        var page = app.Render<ConfirmDialog>();

        Assert.Single(page.FindAll(".modal-foot .btn"));
        Assert.NotNull(page.Find(".modal-foot .btn.primary"));
        Assert.Empty(page.FindAll(".modal-icon.is-danger"));
        Assert.Equal("Got it", page.Find(".modal-foot .btn").InnerText);
    }
}
