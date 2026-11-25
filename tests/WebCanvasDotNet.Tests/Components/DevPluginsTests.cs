using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class DevPluginsTests
{
    [Fact]
    public void EmptyCatalogueShowsTheExactFolderContract()
    {
        using var app = new TestApp();
        var page = app.Render<DevPlugins>();

        Assert.Equal("Plugins", page.Find(".dev-title").InnerText);
        Assert.Empty(page.FindAll(".plugin-row"));
        Assert.Equal(4, page.FindAll(".folder-row").Count);
        Assert.Equal(["plugin.json", "Components/", "Scripts/", "Styles/"],
            page.FindAll(".folder-row .mono").Select(name => name.InnerText).ToArray());
    }

    [Fact]
    public void PluginRowsExposeVersionCapabilitiesAndDescription()
    {
        using var app = new TestApp();
        app.Project.Plugins.Add(new PluginInfo
        {
            Name = "SEO Toolkit",
            Vendor = "OpenCreative",
            Version = "2.1.0",
            Description = "Sitemap helpers and meta tags.",
            Path = Path.Combine(app.Project.CodeFolder, "seo"),
            Capabilities = ["components", "styles"],
            HasComponentFolder = true,
            HasStyleFolder = true,
        });

        var page = app.Render<DevPlugins>();

        Assert.Single(page.FindAll(".plugin-row"));
        Assert.Equal("SEO Toolkit", page.Find(".plugin-title").InnerText);
        Assert.Contains("v2.1.0", page.Find(".plugin-row").InnerText);
        Assert.Contains("Components", page.Find(".plugin-row").InnerText);
        Assert.Equal("Sitemap helpers and meta tags.", page.Find(".plugin-desc").InnerText);
        Assert.Equal(2, page.FindAll(".plugin-chips .chip").Count);
        Assert.Contains("OpenCreative", page.Find(".plugin-sub").InnerText);
    }
}
