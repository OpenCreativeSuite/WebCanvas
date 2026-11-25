using Donkeh.Testing;
using Donkeh.UILab;
using WebCanvasDotNet.Components;
using static Donkeh.UILab._;

namespace WebCanvasDotNet.Tests.Components;

public sealed class UiTests
{
    [Fact]
    public void ButtonRendersKindIconTitleAndRunsItsAction()
    {
        bool clicked = false;
        var page = ComponentTest.Render(
            Ui.Button("Save", () => clicked = true, "primary", Icons.Save, small: true, title: "Save it"));

        var button = page.Find(".btn");
        Assert.True(button.HasClass("primary"));
        Assert.True(button.HasClass("small"));
        Assert.Equal("Save it", button.GetAttribute("title"));
        Assert.Equal("Save", button.Find(".button-label").InnerText);
        Assert.NotNull(button.Find(".fa-floppy-disk"));

        page.Click(".btn");
        Assert.True(clicked);
    }

    [Fact]
    public void InputAndToggleDispatchLiveChangesAndFocusState()
    {
        using var app = new TestApp();
        string? input = null;
        bool? toggled = null;
        var page = ComponentTest.Render(Provider(SessionContext.Value, app.Session, F(
            Ui.Input(app.Session, "Old", value => input = value, "Type here", live: true),
            Ui.Toggle(true, value => toggled = value, "Enabled"))));

        var control = page.Find("input");
        Assert.Equal("Old", control.GetAttribute("value"));

        page.Input(control, "New");
        Assert.Equal("New", input);

        page.Focus(control);
        Assert.True(app.Session.IsTyping);
        page.Blur(control);
        Assert.False(app.Session.IsTyping);

        var toggle = page.Find(".toggle");
        Assert.True(toggle.Find(".switch").HasClass("is-on"));
        Assert.Equal("Enabled", toggle.Find(".toggle-label").InnerText);
        page.Click(".toggle");
        Assert.False(toggled);
    }

    [Fact]
    public void SelectMapsLabelsBackToValues()
    {
        using var app = new TestApp();
        string? selected = null;
        var page = ComponentTest.Render(Provider(SessionContext.Value, app.Session, Ui.Select(
            app.Session,
            [("a", "Alpha"), ("b", "Beta")],
            "b",
            value => selected = value)));

        Assert.Equal(2, page.FindAll("option").Count);
        Assert.Equal("b", page.FindAll("option").First(option => option.HasAttribute("selected")).GetAttribute("value"));

        page.Change(".select-input", "Alpha");

        Assert.Equal("a", selected);
    }

    [Fact]
    public void FieldEmptyStateAndMetaRenderTheirExactParts()
    {
        var field = ComponentTest.Render(Ui.Field("Name", Ui.Empty(Icons.Folder, "Nothing", "Add something", Ui.Button("Add", () => { })), "Help text"));
        Assert.Equal("Name", field.Find(".field-label").InnerText);
        Assert.Equal("Help text", field.Find(".field-hint").InnerText);
        Assert.Equal("Nothing", field.Find(".empty-title").InnerText);
        Assert.Equal("Add", field.Find(".empty-action").InnerText);

        var meta = ComponentTest.Render(Ui.Meta("Pages", "1"));
        Assert.Equal("Pages", meta.Find(".meta-label").InnerText);
        Assert.Equal("1", meta.Find(".meta-value").InnerText);
    }
}

