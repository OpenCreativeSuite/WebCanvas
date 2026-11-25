using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebCanvasDotNet.Core;

/// <summary>Generates a plain static site from the project's pages and data.</summary>
public static class StaticSiteBuilder
{
    public static BuildResult Build(WcProject project, BuildOptions options)
    {
        var watch = Stopwatch.StartNew();
        var log = new List<string>();
        var warnings = new List<string>();
        var result = new BuildResult();

        var output = System.IO.Path.IsPathRooted(options.OutputFolder)
            ? options.OutputFolder
            : System.IO.Path.Combine(project.Root, options.OutputFolder);

        output = System.IO.Path.GetFullPath(output);

        try
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            Directory.CreateDirectory(output);
            log.Add("Created " + Relative(project.Root, output));

            WriteStylesheet(project, options, output, log, warnings);
            WritePages(project, options, output, log, warnings);
            result.EntityPages = WriteEntityPages(project, options, output, log, warnings);
            WriteData(project, options, output, log, warnings);
            result.Assets = CopyAssets(project, options, output, log, warnings);
            WriteSearchIndex(project, options, output, log);
            WriteRobotsAndSitemap(project, options, output, log, warnings);
            WriteRuntime(output, log);

            result.Success = true;
        }
        catch (Exception exception)
        {
            result.Success = false;
            log.Add("Build failed: " + exception.Message);
            warnings.Add(exception.Message);
        }

        watch.Stop();
        result.OutputPath = output;
        result.Pages = project.Pages.Count + result.EntityPages;
        result.Entities = project.TotalEntities;
        result.Milliseconds = watch.ElapsedMilliseconds;
        result.Log = log;
        result.Warnings = warnings;
        return result;
    }

    // ------------------------------------------------------------- stylesheet

    private static void WriteStylesheet(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        var css = new StringBuilder();

        if (File.Exists(project.StyleFile))
        {
            css.AppendLine(File.ReadAllText(project.StyleFile));
        }
        else
        {
            css.AppendLine(":root{" + project.Theme.CssVariables() + "}");
        }

        // Always append the current base site styles. Saved theme files may
        // contain an older copy from an earlier version of the app.
        css.AppendLine(SiteBlocks.Css);

        css.AppendLine("body{margin:0;padding:0;}");
        css.AppendLine("html{scroll-behavior:smooth;}");

        // Plugin stylesheets are appended after the theme so they can refine it.
        foreach (var file in Directory.Exists(project.CodeFolder)
                     ? Directory.GetFiles(project.CodeFolder, "*.css", SearchOption.AllDirectories)
                     : Array.Empty<string>())
        {
            if (!file.Replace('\\', '/').Contains("/Styles/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            css.AppendLine("/* " + System.IO.Path.GetFileName(file) + " */");
            css.AppendLine(File.ReadAllText(file));
            log.Add("Included plugin stylesheet " + System.IO.Path.GetFileName(file));
        }

        File.WriteAllText(System.IO.Path.Combine(output, "site.css"), css.ToString(), Encoding.UTF8);
        log.Add("Wrote site.css");
    }

    // ------------------------------------------------------------------ pages

    private static void WritePages(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        HashSet<string> templates = project.Schemas
            .Where(schema => !string.IsNullOrWhiteSpace(schema.DetailPageId))
            .Select(schema => schema.DetailPageId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var page in project.Pages)
        {
            if (templates.Contains(page.Id))
            {
                log.Add("Skipped template page " + page.Title + " (used for entity routes)");
                continue;
            }

            var html = RenderPage(project, page, options);

            var relative = options.PrettyUrls
                ? page.OutputPath
                : (page.IsHome ? "index.html" : page.Slug.Trim('/') + ".html");

            var path = System.IO.Path.Combine(output, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, html, Encoding.UTF8);
            log.Add("Rendered /" + relative.Replace('\\', '/'));

            if (!page.IsHome && string.IsNullOrWhiteSpace(page.Description))
            {
                warnings.Add("Page “" + page.Title + "” has no description for search engines.");
            }
        }

        var notFound = RenderNotFound(project, options);
        File.WriteAllText(System.IO.Path.Combine(output, "404.html"), notFound, Encoding.UTF8);
        log.Add("Rendered /404.html");
    }

    /// <summary>
    /// Renders only the page body. The live WebView preview uses this exact path,
    /// so the HTML it shows is the same HTML the publisher writes.
    /// </summary>
    public static string RenderBodyHtml(WcProject project, PageDoc page, BuildOptions? options = null)
    {
        var body = new StringBuilder();
        var resolved = options ?? new BuildOptions();

        foreach (var node in page.Nodes)
        {
            RenderNode(body, node, project, page, resolved);
        }

        return body.ToString();
    }

    private static string RenderPage(
        WcProject project,
        PageDoc page,
        BuildOptions options,
        DataSchema? schema = null,
        EntityDoc? entity = null)
    {
        var body = new StringBuilder();

        foreach (var node in page.Nodes)
        {
            RenderNode(body, node, project, page, options, entity: entity, schema: schema);
        }

        var title = entity is not null && schema is not null
            ? project.TitleOf(schema, entity) + " · " + project.Manifest.Name
            : page.IsHome
                ? project.Manifest.Name
                : page.Title + " · " + project.Manifest.Name;

        var description = entity is not null ? EntitySummary(entity) : page.Description;

        if (string.IsNullOrWhiteSpace(description))
        {
            description = page.Description;
        }

        return Skeleton(
            project,
            title,
            description,
            "/" + page.Slug.Trim('/') + (page.Slug.Length > 0 ? "/" : ""),
            body.ToString(),
            options,
            AssetPrefix(page, options));
    }

    /// <summary>
    /// Writes one HTML page per record for every data type that has a detail
    /// page assigned. The detail page becomes the route template and is skipped
    /// by <see cref="WritePages"/> so it has no address of its own.
    /// </summary>
    private static int WriteEntityPages(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        var written = 0;

        foreach (var schema in project.Schemas)
        {
            if (string.IsNullOrWhiteSpace(schema.DetailPageId))
            {
                continue;
            }

            var template = project.Page(schema.DetailPageId);

            if (template is null)
            {
                warnings.Add(schema.Name + " points at a missing detail page.");
                continue;
            }

            var entities = project.EntityList(schema);

            if (entities.Count == 0)
            {
                log.Add("Skipped routes for " + schema.Name + " (no records)");
                continue;
            }

            foreach (var entity in entities)
            {
                var segment = SiteRenderer.EntitySegment(schema, entity);
                var route = Ids.Slug(schema.Plural) + "/" + segment;
                var relative = options.PrettyUrls
                    ? route + "/index.html"
                    : route + ".html";

                var routePage = new PageDoc
                {
                    Id = template.Id,
                    Title = project.TitleOf(schema, entity),
                    Slug = route,
                    Description = EntitySummary(entity),
                    NavLabel = template.NavLabel,
                    ShowInNav = false,
                    Nodes = template.Nodes,
                };

                var html = RenderPage(project, routePage, options, schema, entity);
                var path = System.IO.Path.Combine(output, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                File.WriteAllText(path, html, Encoding.UTF8);
                written++;
                log.Add("Rendered /" + relative);
            }
        }

        return written;
    }

    private static string EntitySummary(EntityDoc entity)
    {
        foreach (var field in new[] { "excerpt", "summary", "description", "intro", "lead" })
        {
            var value = entity.Get(field);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }

    private static string RenderNotFound(WcProject project, BuildOptions options)
    {
        var body = "<section class=\"page-head site-shell\"><span class=\"eyebrow\">404</span>"
                   + "<h1 class=\"page-title\">That page isn't here.</h1>"
                   + "<p class=\"page-lead\">The link may be old, or the page may have moved. "
                   + "<a class=\"accent\" href=\"/\">Return to the home page</a>.</p></section>";

        return Skeleton(project, "Page not found · " + project.Manifest.Name, "", "/404", body, options, "");
    }

    private static string Skeleton(
        WcProject project,
        string title,
        string description,
        string path,
        string body,
        BuildOptions options,
        string assetPrefix)
    {
        var encodedTitle = WebUtility.HtmlEncode(title);
        var encodedDescription = WebUtility.HtmlEncode(description);
        var baseUrl = project.Manifest.BaseUrl.TrimEnd('/');
        var cssHref = assetPrefix + "site.css";
        var scriptHref = assetPrefix + "runtime.js";
        var canonical = string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl + path;
        var inlineStyle = WebUtility.HtmlEncode(project.Theme.CssVariables());

        return $"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{encodedTitle}</title>
<meta name="description" content="{encodedDescription}">
{(canonical.Length > 0 ? "<link rel=\"canonical\" href=\"" + WebUtility.HtmlEncode(canonical) + "\">" : "")}
<meta property="og:title" content="{encodedTitle}">
<meta property="og:description" content="{encodedDescription}">
<meta property="og:type" content="website">
<link rel="stylesheet" href="{cssHref}">
</head>
<body class="wc-site" style="{inlineStyle}">
{body}
<script src="{scriptHref}" defer></script>
</body>
</html>
""";
    }

    private static void RenderNode(
        StringBuilder html,
        PageNode node,
        WcProject project,
        PageDoc page,
        BuildOptions options,
        HashSet<string>? blockStack = null,
        EntityDoc? entity = null,
        DataSchema? schema = null)
    {
        if (node.Tag.Equals("block", StringComparison.OrdinalIgnoreCase))
        {
            string? blockId = node.Attrs is not null && node.Attrs.TryGetValue("block", out string? id) ? id : null;
            BlockDoc? block = project.Block(blockId);

            if (block is null)
            {
                html.Append("<div class=\"wc-block-missing\">Missing block</div>");
                return;
            }

            blockStack ??= new HashSet<string>(StringComparer.Ordinal);

            if (!blockStack.Add(block.Id))
            {
                html.Append("<div class=\"wc-block-missing\">Recursive block</div>");
                return;
            }

            foreach (PageNode child in block.Nodes)
            {
                RenderNode(html, child, project, page, options, blockStack, entity, schema);
            }

            blockStack.Remove(block.Id);
            return;
        }

        if (node.Tag.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            RenderHtmlBlock(html, node);
            return;
        }

        var classes = new List<string>();

        if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            classes.Add(node.ClassName!);
        }

        if (node.Tag.Equals("collection", StringComparison.OrdinalIgnoreCase))
        {
            RenderCollection(html, node, project, page, options, classes);
            return;
        }

        if (node.Tag.Equals("nav", StringComparison.OrdinalIgnoreCase)
            || (node.Attrs is not null && node.Attrs.ContainsKey("nav")))
        {
            RenderNav(html, node, project, page, options, classes);
            return;
        }

        if (node.Tag.Equals("img", StringComparison.OrdinalIgnoreCase)
            && (node.Attrs is null
                || !node.Attrs.TryGetValue("src", out var imageSource)
                || string.IsNullOrWhiteSpace(imageSource)))
        {
            html.Append("<div class=\"wc-media");
            html.Append(classes.Count > 0 ? " " + WebUtility.HtmlEncode(string.Join(' ', classes)) : "");
            html.Append("\">Image</div>");
            return;
        }

        var isVoid = SiteRenderer.VoidTags.Contains(node.Tag);
        var tag = WebUtility.HtmlEncode(node.Tag);
        html.Append('<').Append(tag);

        if (classes.Count > 0)
        {
            html.Append(" class=\"").Append(WebUtility.HtmlEncode(string.Join(' ', classes))).Append('"');
        }

        if (node.Style is { Count: > 0 })
        {
            html.Append(" style=\"")
                .Append(WebUtility.HtmlEncode(string.Join(";", node.Style.Select(pair => pair.Key + ":" + pair.Value))))
                .Append('"');
        }

        Dictionary<string, string>? attrs = node.Attrs;

        if (node.Tag.Equals("iframe", StringComparison.OrdinalIgnoreCase))
        {
            attrs = new Dictionary<string, string>(attrs ?? new Dictionary<string, string>());
            attrs.TryAdd("allow",
                "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share");
            attrs.TryAdd("referrerpolicy", "strict-origin-when-cross-origin");

            if (node.ClassName?.Contains("embed-video", StringComparison.OrdinalIgnoreCase) == true)
            {
                attrs.TryAdd("allowfullscreen", "true");
            }
        }

        if (attrs is not null)
        {
            foreach (var (key, value) in attrs)
            {
                if (key is "nav" or "active" or "limit" or "type")
                {
                    continue;
                }

                var resolved = ResolveTokens(value, project, schema, entity);

                if (key == "href" && value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal))
                {
                    var prefix = AssetPrefix(page, options);
                    resolved = value == "/" ? (prefix.Length == 0 ? "./" : prefix) : prefix + value.TrimStart('/');
                }
                else if (key == "src"
                         && !value.StartsWith("/", StringComparison.Ordinal)
                         && !value.Contains("://", StringComparison.Ordinal)
                         && !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    resolved = AssetPrefix(page, options) + value;
                }

                html.Append(' ').Append(WebUtility.HtmlEncode(key)).Append("=\"")
                    .Append(WebUtility.HtmlEncode(resolved)).Append('"');
            }
        }

        if (isVoid)
        {
            html.Append('>');
            return;
        }

        html.Append('>');

        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            if (TryResolveRichToken(node.Text, project, schema, entity, out string richHtml))
            {
                html.Append(richHtml);
            }
            else
            {
                html.Append(WebUtility.HtmlEncode(ResolveTokens(node.Text, project, schema, entity)));
            }
        }

        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                RenderNode(html, child, project, page, options, blockStack, entity, schema);
            }
        }

        html.Append("</").Append(tag).Append('>');
    }

    private static void RenderNav(
        StringBuilder html,
        PageNode node,
        WcProject project,
        PageDoc page,
        BuildOptions options,
        List<string> classes)
    {
        var tag = node.Tag.Equals("nav", StringComparison.OrdinalIgnoreCase) ? "nav" : "div";
        html.Append('<').Append(tag);

        if (classes.Count > 0)
        {
            html.Append(" class=\"").Append(WebUtility.HtmlEncode(string.Join(' ', classes))).Append('"');
        }

        html.Append('>');

        var active = node.Attrs is not null && node.Attrs.TryGetValue("active", out var slug) ? slug : page.Slug;

        foreach (var target in SiteRenderer.NavPages(project))
        {
            var isActive = string.Equals(target.Slug, active, StringComparison.OrdinalIgnoreCase);
            html.Append("<a href=\"").Append(RelativeHref(page, target, options)).Append('"');

            if (isActive)
            {
                html.Append(" class=\"is-active\"");
            }

            html.Append('>').Append(WebUtility.HtmlEncode(target.NavText)).Append("</a>");
        }

        html.Append("</").Append(tag).Append('>');
    }

    /// <summary>Writes a custom HTML block with its scoped stylesheet.</summary>
    private static void RenderHtmlBlock(StringBuilder html, PageNode node)
    {
        string css = node.Attrs is not null && node.Attrs.TryGetValue("css", out string? styles) ? styles : "";

        if (!string.IsNullOrWhiteSpace(css))
        {
            html.Append("<style>").Append(css).Append("</style>");
        }

        var style = new List<string> { "max-width:100%" };

        if (node.Attrs is not null
            && node.Attrs.TryGetValue("height", out string? height)
            && int.TryParse(height, out int reserved)
            && reserved > 0)
        {
            style.Add("min-height:" + reserved + "px");
        }

        html.Append("<div class=\"wc-html-block");

        if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            html.Append(' ').Append(WebUtility.HtmlEncode(node.ClassName));
        }

        html.Append("\" style=\"").Append(WebUtility.HtmlEncode(string.Join(';', style))).Append("\">")
            .Append(node.Text ?? "")
            .Append("</div>");
    }

    private static void RenderCollection(        StringBuilder html,
        PageNode node,
        WcProject project,
        PageDoc page,
        BuildOptions options,
        List<string> classes)
    {
        html.Append("<div");

        if (classes.Count > 0)
        {
            html.Append(" class=\"").Append(WebUtility.HtmlEncode(string.Join(' ', classes))).Append('"');
        }

        html.Append('>');

        var schema = CollectionSchema(project, node);
        var entities = SiteRenderer.CollectionEntities(project, node);

        if (schema is null)
        {
            html.Append("<div class=\"post-card\"><div class=\"post-body\"><div class=\"post-meta\">Collection</div>")
                .Append("<div class=\"post-title\">Add a matching data type to show this list.</div></div></div>");
        }
        else if (entities.Count == 0)
        {
            html.Append("<div class=\"post-card\"><div class=\"post-body\"><div class=\"post-meta\">")
                .Append(WebUtility.HtmlEncode(schema.Name))
                .Append("</div><div class=\"post-title\">Nothing here yet.</div>")
                .Append("<div class=\"post-excerpt\">Add a record in Content and it will appear here.</div></div></div>");
        }
        else
        {
            foreach (var entity in entities)
            {
                if (schema.Name == "Author")
                {
                    RenderAuthorCard(html, schema, entity, project);
                }
                else
                {
                    var href = string.IsNullOrWhiteSpace(schema.DetailPageId)
                        ? null
                        : EntityHref(schema, entity, page, options);
                    RenderEntityCard(html, schema, entity, project, href);
                }
            }
        }

        html.Append("</div>");
    }

    private static DataSchema? CollectionSchema(WcProject project, PageNode node)
    {
        var name = node.Attrs is not null && node.Attrs.TryGetValue("type", out var type) ? type : "Post";

        return project.Schema(name)
               ?? project.Schemas.FirstOrDefault(schema =>
                   schema.Plural.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static void RenderEntityCard(
        StringBuilder html,
        DataSchema schema,
        EntityDoc entity,
        WcProject project,
        string? href = null)
    {
        var title = project.TitleOf(schema, entity);
        var summary = entity.Get("excerpt");

        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = entity.Get("summary");
        }

        var meta = new List<string>();

        if (schema.Name == "Post")
        {
            if (DateTimeOffset.TryParse(entity.Get("publishedAt"), out var date))
            {
                meta.Add(Ids.Clock(date));
            }

            if (schema.Field("author") is { } author)
            {
                var label = project.RelationLabel(schema, author, entity.Get("author"));

                if (!string.IsNullOrWhiteSpace(label))
                {
                    meta.Add(label);
                }
            }
        }
        else if (schema.Name == "Project")
        {
            var client = entity.Get("client");

            if (!string.IsNullOrWhiteSpace(client))
            {
                meta.Add(client);
            }

            if (!string.IsNullOrWhiteSpace(entity.Get("year")))
            {
                meta.Add(entity.Get("year"));
            }
        }

        var thumb = (Math.Abs(entity.Id.GetHashCode()) % 3) switch
        {
            1 => "post-thumb is-warm",
            2 => "post-thumb is-slate",
            _ => "post-thumb",
        };

        if (href is not null)
        {
            html.Append("<a class=\"post-card\" href=\"").Append(WebUtility.HtmlEncode(href)).Append("\">");
        }
        else
        {
            html.Append("<article class=\"post-card\">");
        }

        html.Append("<div class=\"").Append(thumb).Append("\"></div>")
            .Append("<div class=\"post-body\">");

        if (meta.Count > 0)
        {
            html.Append("<div class=\"post-meta\">").Append(WebUtility.HtmlEncode(string.Join(" · ", meta))).Append("</div>");
        }

        html.Append("<h3 class=\"post-title\">").Append(WebUtility.HtmlEncode(title)).Append("</h3>");

        if (!string.IsNullOrWhiteSpace(summary))
        {
            html.Append("<p class=\"post-excerpt\">").Append(WebUtility.HtmlEncode(summary)).Append("</p>");
        }

        html.Append("</div>").Append(href is not null ? "</a>" : "</article>");
    }

    private static void RenderAuthorCard(
        StringBuilder html,
        DataSchema schema,
        EntityDoc entity,
        WcProject project)
    {
        var name = project.TitleOf(schema, entity);
        var initial = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[0].ToString().ToUpperInvariant();

        html.Append("<div class=\"team-card\">")
            .Append("<div class=\"avatar\">").Append(WebUtility.HtmlEncode(initial)).Append("</div>")
            .Append("<div class=\"team-copy\"><div class=\"team-name\">").Append(WebUtility.HtmlEncode(name)).Append("</div>")
            .Append("<div class=\"team-role\">").Append(WebUtility.HtmlEncode(entity.Get("role"))).Append("</div></div></div>");
    }

    // ------------------------------------------------------------------- data

    private static void WriteData(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        foreach (var schema in project.Schemas)
        {
            if (!schema.Public)
            {
                log.Add("Skipped data/" + Ids.Slug(schema.Plural) + " (private type)");
                continue;
            }

            var folder = System.IO.Path.Combine(output, "data", Ids.Slug(schema.Plural));
            Directory.CreateDirectory(folder);
            var records = new List<object>();

            foreach (var entity in project.EntityList(schema))
            {
                var record = ProjectEntity(schema, entity);

                foreach (var field in schema.Relations())
                {
                    if (!string.IsNullOrWhiteSpace(entity.Get(field.Name))
                        && field.Target is not null
                        && project.Schema(field.Target) is { } target
                        && project.Entity(target, entity.Get(field.Name)) is null)
                    {
                        warnings.Add(schema.Name + " “" + project.TitleOf(schema, entity) + "” points at a missing " + target.Name + ".");
                    }
                }

                records.Add(record);
                var json = JsonSerializer.Serialize(record, WcJson.Options);
                File.WriteAllText(System.IO.Path.Combine(folder, Ids.Slug(entity.Id) + ".json"), json, Encoding.UTF8);
            }

            File.WriteAllText(
                System.IO.Path.Combine(folder, "index.json"),
                JsonSerializer.Serialize(records, WcJson.Options),
                Encoding.UTF8);

            log.Add("Wrote data/" + Ids.Slug(schema.Plural) + " (" + records.Count + " records)");
        }
    }

    private static Dictionary<string, object?> ProjectEntity(DataSchema schema, EntityDoc entity)
    {
        var record = new Dictionary<string, object?>
        {
            ["id"] = entity.Id,
            ["createdAt"] = entity.CreatedAt,
            ["updatedAt"] = entity.UpdatedAt,
        };

        foreach (var field in schema.Fields)
        {
            var raw = entity.Get(field.Name);

            record[field.Name] = field.Type switch
            {
                FieldTypes.Number => decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : null,
                FieldTypes.Boolean => raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1",
                _ => raw,
            };
        }

        return record;
    }

    // ----------------------------------------------------------------- assets

    private static int CopyAssets(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        if (!options.CopyAssets)
        {
            log.Add("Skipped assets");
            return 0;
        }

        var copied = 0;

        if (Directory.Exists(project.AssetsFolder))
        {
            var target = System.IO.Path.Combine(output, "assets");

            foreach (var file in Directory.GetFiles(project.AssetsFolder, "*", SearchOption.AllDirectories))
            {
                var relative = System.IO.Path.GetRelativePath(project.AssetsFolder, file);
                var destination = System.IO.Path.Combine(target, relative);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
                copied++;
            }
        }

        // Plugin script bundles are published alongside assets so pages can opt in.
        if (Directory.Exists(project.CodeFolder))
        {
            foreach (var file in Directory.GetFiles(project.CodeFolder, "*.js", SearchOption.AllDirectories))
            {
                if (!file.Replace('\\', '/').Contains("/Scripts/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var directory = new DirectoryInfo(file).Parent?.Name ?? "plugin";
                var target = System.IO.Path.Combine(output, "assets", "plugins", directory);
                Directory.CreateDirectory(target);
                File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)), true);
                copied++;
            }
        }

        log.Add(copied == 0 ? "No assets to copy" : "Copied " + copied + " asset files");
        return copied;
    }

    // ------------------------------------------------------------ search, meta

    private static void WriteSearchIndex(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log)
    {
        if (!options.SearchIndex)
        {
            log.Add("Skipped search index");
            return;
        }

        var documents = new List<object>();
        var templates = project.Schemas
            .Where(schema => !string.IsNullOrWhiteSpace(schema.DetailPageId))
            .Select(schema => schema.DetailPageId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var page in project.Pages)
        {
            if (templates.Contains(page.Id))
            {
                continue;
            }

            documents.Add(new
            {
                kind = "page",
                title = page.Title,
                url = options.PrettyUrls
                    ? (page.IsHome ? "/" : "/" + page.Slug.Trim('/') + "/")
                    : (page.IsHome ? "/index.html" : "/" + page.Slug.Trim('/') + ".html"),
                text = page.Description,
            });
        }

        foreach (var schema in project.Schemas.Where(schema => schema.Public))
        {
            var hasDetailPage = !string.IsNullOrWhiteSpace(schema.DetailPageId)
                                && project.Page(schema.DetailPageId) is not null;

            foreach (var entity in project.EntityList(schema))
            {
                var title = project.TitleOf(schema, entity);
                var text = EntitySummary(entity);

                documents.Add(new
                {
                    kind = schema.Name,
                    title,
                    url = hasDetailPage
                        ? SiteRenderer.EntityPath(schema, entity) + (options.PrettyUrls ? "/" : ".html")
                        : "/data/" + Ids.Slug(schema.Plural) + "/" + Ids.Slug(entity.Id) + ".json",
                    text,
                });
            }
        }

        File.WriteAllText(
            System.IO.Path.Combine(output, "search.json"),
            JsonSerializer.Serialize(documents, WcJson.Options),
            Encoding.UTF8);

        log.Add("Wrote search.json (" + documents.Count + " entries)");
    }

    private static void WriteRobotsAndSitemap(
        WcProject project,
        BuildOptions options,
        string output,
        List<string> log,
        List<string> warnings)
    {
        if (!options.Sitemap)
        {
            log.Add("Skipped sitemap");
            return;
        }

        var baseUrl = project.Manifest.BaseUrl.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            warnings.Add("Set a site URL in the project to generate absolute sitemap links.");
            baseUrl = "https://example.com";
        }

        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        var templates = project.Schemas
            .Where(schema => !string.IsNullOrWhiteSpace(schema.DetailPageId))
            .Select(schema => schema.DetailPageId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var page in project.Pages)
        {
            if (templates.Contains(page.Id))
            {
                continue;
            }

            var path = options.PrettyUrls
                ? (page.IsHome ? "/" : "/" + page.Slug.Trim('/') + "/")
                : (page.IsHome ? "/" : "/" + page.Slug.Trim('/') + ".html");

            builder.Append("  <url><loc>").Append(WebUtility.HtmlEncode(baseUrl + path)).Append("</loc></url>").AppendLine();
        }

        foreach (var schema in project.Schemas.Where(schema =>
                     schema.Public && !string.IsNullOrWhiteSpace(schema.DetailPageId)))
        {
            foreach (var entity in project.EntityList(schema))
            {
                var path = SiteRenderer.EntityPath(schema, entity) + (options.PrettyUrls ? "/" : ".html");
                builder.Append("  <url><loc>").Append(WebUtility.HtmlEncode(baseUrl + path)).Append("</loc></url>").AppendLine();
            }
        }

        builder.AppendLine("</urlset>");

        File.WriteAllText(System.IO.Path.Combine(output, "sitemap.xml"), builder.ToString(), Encoding.UTF8);
        File.WriteAllText(
            System.IO.Path.Combine(output, "robots.txt"),
            "User-agent: *\nAllow: /\n\nSitemap: " + baseUrl + "/sitemap.xml\n",
            Encoding.UTF8);

        log.Add("Wrote sitemap.xml and robots.txt");
    }

    private static void WriteRuntime(string output, List<string> log)
    {
        const string runtime = """
// WebCanvas static runtime. Small, dependency-free and progressive.
(function () {
    var current = window.location.pathname.replace(/\/$/, "") || "/";
    document.querySelectorAll(".site-nav a").forEach(function (link) {
        var href = link.getAttribute("href") || "";
        var path = href.replace(/\/$/, "") || "/";
        if (path === current) { link.classList.add("is-active"); }
    });
    var year = new Date().getFullYear();
    document.querySelectorAll("[data-year]").forEach(function (node) {
        node.textContent = String(year);
    });
})();
""";

        File.WriteAllText(System.IO.Path.Combine(output, "runtime.js"), runtime, Encoding.UTF8);
        log.Add("Wrote runtime.js");
    }

    /// <summary>
    /// A text node that is exactly one rich-text token is raw HTML, not text.
    /// This lets detail pages render rich-text fields without escaping.
    /// </summary>
    private static bool TryResolveRichToken(
        string text,
        WcProject project,
        DataSchema? schema,
        EntityDoc? entity,
        out string html)
    {
        html = "";

        if (schema is null || entity is null)
        {
            return false;
        }

        string trimmed = text.Trim();

        if (!trimmed.StartsWith("{{", StringComparison.Ordinal)
            || !trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return false;
        }

        string token = trimmed[2..^2].Trim();

        if (schema.Field(token) is not { Type: FieldTypes.RichText } field)
        {
            return false;
        }

        html = entity.Get(field.Name);
        return true;
    }

    /// <summary>Replaces <c>{{field}}</c> tokens with values from the current record.</summary>
    private static string ResolveTokens(        string value,
        WcProject project,
        DataSchema? schema,
        EntityDoc? entity)
    {
        if (entity is null
            || schema is null
            || value.Length == 0
            || value.IndexOf("{{", StringComparison.Ordinal) < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 32);
        var index = 0;

        while (index < value.Length)
        {
            var open = value.IndexOf("{{", index, StringComparison.Ordinal);

            if (open < 0)
            {
                builder.Append(value, index, value.Length - index);
                break;
            }

            var close = value.IndexOf("}}", open + 2, StringComparison.Ordinal);

            if (close < 0)
            {
                builder.Append(value, index, value.Length - index);
                break;
            }

            builder.Append(value, index, open - index);
            builder.Append(TokenValue(value[(open + 2)..close].Trim(), project, schema, entity));
            index = close + 2;
        }

        return builder.ToString();
    }

    private static string TokenValue(
        string token,
        WcProject project,
        DataSchema schema,
        EntityDoc entity)
    {
        if (token.Equals("title", StringComparison.OrdinalIgnoreCase)
            || token.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            return project.TitleOf(schema, entity);
        }

        if (token.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return entity.Id;
        }

        if (token.Equals("slug", StringComparison.OrdinalIgnoreCase))
        {
            return SiteRenderer.EntitySegment(schema, entity);
        }

        if (token.Equals("url", StringComparison.OrdinalIgnoreCase))
        {
            return SiteRenderer.EntityPath(schema, entity);
        }

        if (token.Equals("type", StringComparison.OrdinalIgnoreCase))
        {
            return schema.Name;
        }

        var field = schema.Field(token);

        if (field is null)
        {
            return "";
        }

        var raw = entity.Get(field.Name);

        if (field.Type == FieldTypes.Relation
            && field.Target is not null
            && project.Schema(field.Target) is not null)
        {
            return project.RelationLabel(schema, field, raw);
        }

        if (field.Type == FieldTypes.Date
            && DateTimeOffset.TryParse(raw, out var when))
        {
            return Ids.Clock(when);
        }

        return raw;
    }

    private static string EntityHref(
        DataSchema schema,
        EntityDoc entity,
        PageDoc from,
        BuildOptions options)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", from.SlugDepth));
        var route = Ids.Slug(schema.Plural) + "/" + SiteRenderer.EntitySegment(schema, entity);
        return prefix + route + (options.PrettyUrls ? "/" : ".html");
    }

    /// <summary>
    /// Links are relative to the page being written, so the output works from a
    /// domain root, a project subpath or a local file without configuration.
    /// </summary>
    private static string RelativeHref(PageDoc from, PageDoc to, BuildOptions options)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", from.SlugDepth));

        if (!options.PrettyUrls)
        {
            return prefix + (to.IsHome ? "index.html" : to.Slug.Trim('/') + ".html");
        }

        if (to.IsHome)
        {
            return prefix.Length == 0 ? "./" : prefix;
        }

        return prefix + to.Slug.Trim('/') + "/";
    }

    private static string AssetPrefix(PageDoc page, BuildOptions options) =>
        string.Concat(Enumerable.Repeat("../", page.SlugDepth));

    private static string Relative(string root, string path) =>
        System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
}
