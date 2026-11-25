using System.Collections.Generic;

namespace WebCanvasDotNet.Core;

/// <summary>One ready-made core block template.</summary>
public sealed class CoreBlockPrefab
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    /// <summary>"Navigation" for headers, "Footer" for footers.</summary>
    public string Category { get; set; } = "Navigation";

    public string Icon { get; set; } = "fa-layer-group";

    public Func<string, PageNode> Build { get; set; } = _ => PageNode.Create("div");
}

/// <summary>
/// The built-in prefab kit: full-width headers and footers that can be applied
/// to the current theme block or used to create a new one.
/// </summary>
public static class CorePrefabs
{
    public static readonly CoreBlockPrefab[] All =
    [
        new()
        {
            Id = "header-simple",
            Name = "Simple header",
            Description = "Brand on the left, navigation on the right.",
            Category = "Navigation",
            Icon = "fa-window-maximize",
            Build = brand => PageNode.Create("header", "site-header", null, null,
                PageNode.Create("div", "site-shell site-header-inner", null, null,
                    Brand(brand),
                    MainNav())),
        },
        new()
        {
            Id = "header-split",
            Name = "Split header",
            Description = "Brand left, centred navigation, action right.",
            Category = "Navigation",
            Icon = "fa-table-columns",
            Build = brand => PageNode.Create("header", "site-header", null, null,
                PageNode.Create("div", "site-shell site-header-inner header-split", null, null,
                    Brand(brand),
                    MainNav(),
                    PageNode.Create("div", "site-header-actions", null, null,
                        Linked("a", "site-btn", "Start a project", "/contact/")))),
        },
        new()
        {
            Id = "header-centered",
            Name = "Centered header",
            Description = "Centered brand with navigation underneath.",
            Category = "Navigation",
            Icon = "fa-align-center",
            Build = brand => PageNode.Create("header", "site-header header-centered", null, null,
                PageNode.Create("div", "site-shell header-centered-inner", null, null,
                    Brand(brand),
                    MainNav())),
        },
        new()
        {
            Id = "header-minimal",
            Name = "Minimal header",
            Description = "Brand and a single contact action.",
            Category = "Navigation",
            Icon = "fa-minus",
            Build = brand => PageNode.Create("header", "site-header", null, null,
                PageNode.Create("div", "site-shell site-header-inner", null, null,
                    Brand(brand),
                    PageNode.Create("div", "site-header-actions", null, null,
                        Linked("a", "site-btn ghost", "Contact", "/contact/")))),
        },
        new()
        {
            Id = "footer-columns",
            Name = "Three-column footer",
            Description = "Brand, navigation and contact details.",
            Category = "Footer",
            Icon = "fa-table-columns",
            Build = brand => PageNode.Create("footer", "site-footer", null, null,
                PageNode.Create("div", "site-shell", null, null,
                    PageNode.Create("div", "footer-grid", null, null,
                        PageNode.Create("div", "footer-col", null, null,
                            PageNode.Create("div", "footer-brand", brand),
                            PageNode.Create("div", null, "A short sentence about the studio or site.")),
                        PageNode.Create("div", "footer-col", null, null,
                            PageNode.Create("div", "detail-label", "Explore"),
                            FooterNav()),
                        PageNode.Create("div", "footer-col", null, null,
                            PageNode.Create("div", "detail-label", "Say hello"),
                            PageNode.Create("div", null, "hello@example.com"),
                            PageNode.Create("div", null, "City, country"))),
                    PageNode.Create("div", "footer-fine", "© " + DateTime.UtcNow.Year + " " + brand + ". Built with WebCanvas."))),
        },
        new()
        {
            Id = "footer-simple",
            Name = "Simple footer",
            Description = "Brand and links on one line.",
            Category = "Footer",
            Icon = "fa-minus",
            Build = brand => PageNode.Create("footer", "site-footer footer-simple", null, null,
                PageNode.Create("div", "site-shell", null, null,
                    PageNode.Create("div", "footer-simple-inner", null, null,
                        Brand(brand),
                        FooterNav()),
                    PageNode.Create("div", "footer-fine", "© " + DateTime.UtcNow.Year + " " + brand + "."))),
        },
        new()
        {
            Id = "footer-newsletter",
            Name = "Newsletter footer",
            Description = "Brand, short pitch and a subscribe action.",
            Category = "Footer",
            Icon = "fa-paper-plane",
            Build = brand => PageNode.Create("footer", "site-footer footer-newsletter", null, null,
                PageNode.Create("div", "site-shell", null, null,
                    PageNode.Create("div", "footer-newsletter-inner", null, null,
                        PageNode.Create("div", "footer-newsletter-copy", null, null,
                            PageNode.Create("div", "footer-brand", brand),
                            PageNode.Create("p", null, "One short letter each month: what we're making, reading and learning.")),
                        Linked("a", "site-btn", "Subscribe", "/contact/")),
                    PageNode.Create("div", "footer-fine", "© " + DateTime.UtcNow.Year + " " + brand + ". Unsubscribe any time."))),
        },
        new()
        {
            Id = "footer-centered",
            Name = "Centered footer",
            Description = "Centered brand, links and fine print.",
            Category = "Footer",
            Icon = "fa-align-center",
            Build = brand => PageNode.Create("footer", "site-footer footer-centered", null, null,
                PageNode.Create("div", "site-shell", null, null,
                    Brand(brand),
                    FooterNav(),
                    PageNode.Create("div", "footer-fine", "© " + DateTime.UtcNow.Year + " " + brand + "."))),
        },
    ];

    public static CoreBlockPrefab? Find(string id) =>
        All.FirstOrDefault(prefab => prefab.Id == id);

    private static PageNode Brand(string name) =>
        Linked("a", "site-brand", null, "/",
            PageNode.Create("span", "site-mark"),
            PageNode.Create("span", null, string.IsNullOrWhiteSpace(name) ? "Your site" : name));

    private static PageNode MainNav() =>
        Nav("main", "site-nav");

    private static PageNode FooterNav() =>
        Nav("footer", "footer-nav");

    private static PageNode Nav(string scope, string className)
    {
        var nav = PageNode.Create("nav", className);
        nav.Attrs = new Dictionary<string, string> { ["nav"] = scope };
        return nav;
    }

    private static PageNode Linked(string tag, string? className, string? text, string href, params PageNode[] children)
    {
        var node = PageNode.Create(tag, className, text, null, children);
        node.Attrs = new Dictionary<string, string> { ["href"] = href };
        return node;
    }
}
