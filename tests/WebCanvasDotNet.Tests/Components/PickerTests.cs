using WebCanvasDotNet.Components;

namespace WebCanvasDotNet.Tests.Components;

public sealed class PickerTests
{
    [Fact]
    public void ShowsSelectedValueAndAriaState()
    {
        using var app = new TestApp();
        var page = app.Render<Picker, PickerProps>(new PickerProps
        {
            Options = new()
            {
                new PickerOption { Value = "a", Label = "Alpha" },
                new PickerOption { Value = "b", Label = "Beta" },
            },
            SelectedValue = "a",
        });

        Assert.Equal("Alpha", page.Find(".picker-value").InnerText);
        Assert.Equal("false", page.Find(".picker-trigger").GetAttribute("aria-expanded"));

        page.Click(".picker-trigger");
        Assert.Equal("true", page.Find(".picker-trigger").GetAttribute("aria-expanded"));
        Assert.Equal("true", page.FindAll(".picker-item")[0].GetAttribute("aria-selected"));
        Assert.Equal("false", page.FindAll(".picker-item")[1].GetAttribute("aria-selected"));
        Assert.Single(page.FindAll(".picker-check"));
    }

    [Fact]
    public void SelectingAnOptionUpdatesAriaAndClosesThePopup()
    {
        using var app = new TestApp();
        string? selected = null;
        var page = app.Render<Picker, PickerProps>(new PickerProps
        {
            Options = new()
            {
                new PickerOption { Value = "a", Label = "Alpha" },
                new PickerOption { Value = "b", Label = "Beta" },
            },
            SelectedValue = "a",
            OnSelect = value => selected = value,
        });

        page.Click(".picker-trigger");
        Assert.Equal("true", page.Find(".picker-trigger").GetAttribute("aria-expanded"));
        Assert.Equal(2, page.FindAll(".picker-item").Count);

        page.Click(page.FindByText("Beta"));

        Assert.Equal("b", selected);
        Assert.Empty(page.FindAll(".picker-pop"));
    }

    [Fact]
    public void EmptyOptionsUseThePlaceholderAndEmptyMessage()
    {
        using var app = new TestApp();
        var page = app.Render<Picker, PickerProps>(new PickerProps { PlaceholderText = "Pick a type" });

        Assert.Equal("Pick a type", page.Find(".picker-value").InnerText);
        page.Click(".picker-trigger");
        Assert.Equal("No options", page.Find(".picker-empty").InnerText);
    }
}

