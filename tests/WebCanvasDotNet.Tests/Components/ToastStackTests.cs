using Donkeh.Testing;
using Donkeh.UILab;
using WebCanvasDotNet.Components;
using static Donkeh.UILab._;

namespace WebCanvasDotNet.Tests.Components;

public sealed class ToastStackTests
{
    [Fact]
    public void RendersNewestFirstWithKindClassesAndDismissesOne()
    {
        using var app = new TestApp();
        app.Session.Notify("One", "info");
        app.Session.Notify("Two", "success");
        var page = app.Render<ToastStack>();

        Assert.Equal(["Two", "One"], page.FindAll(".toast-message").Select(message => message.InnerText).ToArray());
        Assert.True(page.FindAll(".toast")[0].HasClass("success"));
        Assert.True(page.FindAll(".toast")[1].HasClass("info"));

        page.Click(".toast-close");

        Assert.Single(app.Session.Toasts);
        Assert.Equal("One", app.Session.Toasts[0].Message);
    }

    [Fact]
    public void KeepsAtMostFourToasts()
    {
        using var app = new TestApp();

        for (int i = 0; i < 6; i++)
        {
            app.Session.Notify("Toast " + i, i % 2 == 0 ? "error" : "warning");
        }

        var page = app.Render<ToastStack>();

        Assert.Equal(4, page.FindAll(".toast").Count);
        Assert.Equal(["Toast 5", "Toast 4", "Toast 3", "Toast 2"],
            page.FindAll(".toast-message").Select(message => message.InnerText).ToArray());
        Assert.True(page.FindAll(".toast")[0].HasClass("warning"));
        Assert.True(page.FindAll(".toast")[1].HasClass("error"));
    }
}
