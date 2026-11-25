using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class StaticSiteBuilderTests
{
    [Fact]
    public void PublishesNestedPagesEntityRoutesLinksSearchAndSitemap()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var detail = app.AddPage("Article detail",
            PageNode.Create("h1", "page-title", "{{title}}"),
            PageNode.Create("p", "page-lead", "{{summary}}"),
            PageNode.Create("p", null, "{{body}}"));
        schema.DetailPageId = detail.Id;
        app.Project.SaveSchema(schema);

        var firstEntity = app.AddEntity(schema, ("title", "First story"), ("slug", "First Story"),
            ("summary", "Short summary"), ("body", "First body"));
        var secondEntity = app.AddEntity(schema, ("title", "Second story"), ("summary", "No slug"));

        var nested = app.AddPage("Team", PageNode.Create("h1", "page-title", "Our team"));
        nested.Slug = "about/team";
        app.Project.SavePage(nested);

        app.Project.HomePage!.Nodes.Add(PageNode.CreateWithAttrs("collection", "collection-grid", null,
            new Dictionary<string, string> { ["type"] = "Article" }));
        app.Project.SavePage(app.Project.HomePage);

        var result = StaticSiteBuilder.Build(app.Project, new BuildOptions());

        Assert.True(result.Success);
        Assert.Equal(2, result.EntityPages);
        Assert.Equal(2, result.Entities);
        Assert.Equal(app.Project.Pages.Count + result.EntityPages, result.Pages);

        Assert.True(File.Exists(Path.Combine(result.OutputPath, "about", "team", "index.html")));

        var first = File.ReadAllText(Path.Combine(result.OutputPath, "articles", "first-story", "index.html"));
        Assert.Contains("First story", first);
        Assert.Contains("Short summary", first);
        Assert.Contains("First body", first);
        Assert.DoesNotContain("{{title}}", first);
        Assert.Contains("https://example.com/articles/first-story/", File.ReadAllText(Path.Combine(result.OutputPath, "sitemap.xml")));

        var secondPath = Path.Combine(result.OutputPath, "articles", SiteRenderer.EntitySegment(schema, secondEntity), "index.html");
        Assert.True(File.Exists(secondPath));
        Assert.Contains("Second story", File.ReadAllText(secondPath));

        var index = File.ReadAllText(Path.Combine(result.OutputPath, "index.html"));
        Assert.Contains("href=\"articles/first-story/\"", index);
        Assert.Contains("href=\"articles/" + SiteRenderer.EntitySegment(schema, secondEntity) + "/\"", index);

        var search = File.ReadAllText(Path.Combine(result.OutputPath, "search.json"));
        Assert.Contains("/articles/first-story/", search);
        Assert.Contains("/articles/" + SiteRenderer.EntitySegment(schema, secondEntity) + "/", search);
    }

    [Fact]
    public void NonPrettyUrlsWriteHtmlFilesAndNoDirectoryIndexes()
    {
        using var app = new TestApp();
        var schema = app.AddSchema("Article");
        var detail = app.AddPage("Detail", PageNode.Create("h1", null, "{{title}}"));
        schema.DetailPageId = detail.Id;
        app.Project.SaveSchema(schema);
        var entity = app.AddEntity(schema, ("title", "First story"), ("slug", "first-story"));

        var nested = app.AddPage("Team", PageNode.Create("h1", null, "Team"));
        nested.Slug = "about/team";
        app.Project.SavePage(nested);

        var result = StaticSiteBuilder.Build(app.Project, new BuildOptions { PrettyUrls = false });

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(result.OutputPath, "about", "team.html")));
        Assert.True(File.Exists(Path.Combine(result.OutputPath, "articles", SiteRenderer.EntitySegment(schema, entity) + ".html")));
        Assert.False(Directory.Exists(Path.Combine(result.OutputPath, "about", "team")));
        Assert.Contains("/about/team.html", File.ReadAllText(Path.Combine(result.OutputPath, "sitemap.xml")));
    }

    [Fact]
    public void CurrentBaseCssAndIframeDefaultsReachThePublishedSite()
    {
        using var app = new TestApp();
        app.Project.HomePage!.Nodes.Add(PageNode.CreateWithAttrs("iframe", "embed-video", null,
            new Dictionary<string, string> { ["src"] = "https://www.youtube.com/embed/abc" }));
        app.Project.SavePage(app.Project.HomePage);

        var result = StaticSiteBuilder.Build(app.Project, new BuildOptions());

        Assert.True(result.Success);
        var html = File.ReadAllText(Path.Combine(result.OutputPath, "index.html"));
        var css = File.ReadAllText(Path.Combine(result.OutputPath, "site.css"));

        Assert.Contains("allowfullscreen=\"true\"", html);
        Assert.Contains("referrerpolicy=\"strict-origin-when-cross-origin\"", html);
        Assert.Contains("allow=\"accelerometer;", html);
        Assert.Contains(".embed-video", css);
        Assert.Contains("aspect-ratio: 16 / 9", css);
    }
}

