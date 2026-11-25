using Donkeh.Testing;
using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevThemeTests
{
    [Fact]
    public void ThemeFieldsAndPreviewUseTheExactTokens()
    {
        using var app = new TestApp();
        var page = app.Render<DevTheme>();

        Assert.Equal("Studio", ThemeInput(page, "Theme name").Value);
        Assert.Equal("#2456c7", page.Find(".accent-input").Value);
        Assert.Equal("Ink", page.Find(".accent-swatch.is-active").GetAttribute("title"));
        Assert.Contains("--site-accent:#2456c7", page.Find(".theme-preview").GetAttribute("style"));
        Assert.Equal("Interfaces that feel inevitable.", page.Find(".theme-preview .hero-title").InnerText);
        Assert.Equal(2, page.FindAll(".theme-chip").Count);
    }

    [Fact]
    public void AccentButtonsAndInputsPersistTheTheme()
    {
        using var app = new TestApp();
        var page = app.Render<DevTheme>();

        page.Click(page.FindAll(".accent-swatch")[0]);
        Assert.Equal("#2f6b4f", app.Project.Theme.Accent);

        page = app.Render<DevTheme>();
        Assert.Equal("Forest", page.Find(".accent-swatch.is-active").GetAttribute("title"));
        Assert.Contains("--site-accent:#2f6b4f", page.Find(".theme-preview").GetAttribute("style"));
        Assert.Contains("--site-accent:#2f6b4f", File.ReadAllText(app.Project.StyleFile));

        page.Change(page.Find(".accent-input"), "#112233");
        Assert.Equal("#112233", app.Project.Theme.Accent);
        Assert.Contains("--site-accent:#112233", File.ReadAllText(app.Project.StyleFile));
    }

    [Fact]
    public void ThemeNameEditsAreSavedAndReflectedInThePreview()
    {
        using var app = new TestApp();
        var page = app.Render<DevTheme>();

        page.Change(ThemeInput(page, "Theme name"), "Atelier");
        Assert.Equal("Atelier", app.Project.Theme.Name);

        page = app.Render<DevTheme>();
        Assert.Equal("Atelier", page.Find(".theme-preview .site-brand").InnerText);
    }

    private static TestElement ThemeInput(ComponentTestHarness page, string label) =>
        page.FindAll(".field").First(field => field.Find(".field-label").InnerText == label).Find(".text-input");
}
