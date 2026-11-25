using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebCanvasDotNet.Core;

/// <summary>Shared JSON settings for every file WebCanvas reads or writes.</summary>
public static class WcJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

/// <summary>Small identifier and slug helpers shared by the model and the UI.</summary>
public static class Ids
{
    public static string New(string prefix = "n") =>
        prefix + "_" + Guid.NewGuid().ToString("n")[..8];

    public static string Slug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var buffer = new System.Text.StringBuilder(text.Length);
        var dash = false;

        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (dash && buffer.Length > 0)
                {
                    buffer.Append('-');
                }

                buffer.Append(ch);
                dash = false;
            }
            else
            {
                dash = true;
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Normalises a URL path such as <c>about/team</c>. Each segment is slugged,
    /// empty segments are dropped, and the result never starts or ends with a slash.
    /// </summary>
    public static string SlugPath(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var segments = new List<string>();

        foreach (string segment in text.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string slug = Slug(segment);

            if (slug.Length > 0)
            {
                segments.Add(slug);
            }
        }

        return string.Join('/', segments);
    }

    public static string Humanize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        var buffer = new System.Text.StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];

            if (i > 0 && char.IsUpper(ch) && !char.IsUpper(name[i - 1]))
            {
                buffer.Append(' ');
            }

            buffer.Append(i == 0 ? char.ToUpperInvariant(ch) : ch);
        }

        return buffer.ToString();
    }

    public static string Plural(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "items";
        }

        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase)
            && name.Length > 1
            && !"aeiou".Contains(char.ToLowerInvariant(name[^2])))
        {
            return name[..^1] + "ies";
        }

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("x", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("ch", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return name + "es";
        }

        return name + "s";
    }

    public static string Now() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    public static string Clock(DateTimeOffset when) => when.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    public static string Relative(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso) || !DateTimeOffset.TryParse(iso, out var when))
        {
            return "—";
        }

        var span = DateTimeOffset.UtcNow - when;

        if (span.TotalMinutes < 1)
        {
            return "just now";
        }

        if (span.TotalHours < 1)
        {
            return (int)span.TotalMinutes + "m ago";
        }

        if (span.TotalDays < 1)
        {
            return (int)span.TotalHours + "h ago";
        }

        if (span.TotalDays < 30)
        {
            return (int)span.TotalDays + "d ago";
        }

        return Clock(when);
    }
}

/// <summary>The contents of a project's <c>.wc</c> file.</summary>
public sealed class ProjectManifest
{
    public string Name { get; set; } = "Untitled site";

    public int Version { get; set; } = 1;

    public string Description { get; set; } = "";

    public string BaseUrl { get; set; } = "";

    public string Created { get; set; } = Ids.Now();

    public string Template { get; set; } = "blank";
}

/// <summary>Where a dragged widget lands relative to the node under the pointer.</summary>
public enum DropPosition
{
    Before,
    Inside,
    After,
}

/// <summary>
/// The live drop preview: the target node, the insertion position and the
/// screen-space box used to paint the insertion line.
/// </summary>
public sealed class DropHint
{
    public string TargetId { get; set; } = "";

    public DropPosition Position { get; set; } = DropPosition.After;

    /// <summary>Child insertion index when dropping inside a container, or -1.</summary>
    public int Index { get; set; } = -1;

    public string Label { get; set; } = "";

    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    /// <summary>Pointer position in window CSS pixels, used to anchor the preview.</summary>
    public float PointerX { get; set; }

    public float PointerY { get; set; }
}

/// <summary>
/// A captured page box used by the drag wireframe. Coordinates are relative to
/// the page root and already in the canvas's unscaled layout space.
/// </summary>
public sealed class DropBound
{
    public string NodeId { get; set; } = "";

    public string Label { get; set; } = "";

    public string Tag { get; set; } = "";

    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public int Depth { get; set; }

    public bool IsContainer { get; set; }

    public bool IsBlock { get; set; }

    public bool IsPage { get; set; }

    public bool ShowLabel => IsContainer || IsBlock || (Width >= 110 && Height >= 30);
}

/// <summary>One element in a page's JSON component tree.</summary>
public sealed class PageNode
{
    public string Id { get; set; } = Ids.New();

    public string Tag { get; set; } = "div";

    /// <summary>Optional text content. Leaf blocks normally use this.</summary>
    public string? Text { get; set; }

    public string? ClassName { get; set; }

    public Dictionary<string, string>? Style { get; set; }

    public Dictionary<string, string>? Attrs { get; set; }

    public List<PageNode>? Children { get; set; }

    public static PageNode Create(
        string tag,
        string? className = null,
        string? text = null,
        Dictionary<string, string>? style = null,
        params PageNode[] children)
    {
        return new PageNode
        {
            Tag = tag,
            ClassName = className,
            Text = text,
            Style = style,
            Children = children.Length == 0 ? null : children.ToList(),
        };
    }

    /// <summary>
    /// Creates a node with HTML attributes. Use this instead of passing a
    /// dictionary as <c>style</c>: a link's <c>href</c> is not a CSS property.
    /// </summary>
    public static PageNode CreateWithAttrs(
        string tag,
        string? className = null,
        string? text = null,
        Dictionary<string, string>? attrs = null,
        params PageNode[] children)
    {
        return new PageNode
        {
            Tag = tag,
            ClassName = className,
            Text = text,
            Attrs = attrs,
            Children = children.Length == 0 ? null : children.ToList(),
        };
    }

    public PageNode Clone()
    {
        return new PageNode
        {
            Id = Ids.New(),
            Tag = Tag,
            Text = Text,
            ClassName = ClassName,
            Style = Style is null ? null : new Dictionary<string, string>(Style),
            Attrs = Attrs is null ? null : new Dictionary<string, string>(Attrs),
            Children = Children?.Select(child => child.Clone()).ToList(),
        };
    }

    public bool HasChildren => Children is { Count: > 0 };

    /// <summary>True when the tag accepts child nodes and can host a dropped widget.</summary>
    public bool IsContainer => IsContainerTag(Tag);

    public static bool IsContainerTag(string tag) =>
        tag.ToLowerInvariant() is
            "section" or "div" or "main" or "header" or "footer" or "article" or "aside" or "form"
            or "ul" or "ol" or "li" or "blockquote" or "figure" or "figurecaption" or "figcaption";

    public string DisplayName()
    {
        if (!string.IsNullOrWhiteSpace(Text) && Tag is "h1" or "h2" or "h3" or "h4" or "p" or "span" or "a" or "button")
        {
            var text = Text.Trim().Replace('\n', ' ');
            return text.Length > 34 ? text[..34] + "…" : text;
        }

        if (Attrs is not null)
        {
            foreach (var key in new[] { "aria-label", "alt", "name", "type" })
            {
                if (Attrs.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return Tag + " · " + value;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(ClassName))
        {
            var first = ClassName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (first is not null)
            {
                return Tag + "." + first;
            }
        }

        return Tag;
    }
}

/// <summary>A page document stored under <c>app/design/pages</c>.</summary>
public sealed class PageDoc
{
    public string Id { get; set; } = Ids.New("page");

    public string Title { get; set; } = "Untitled page";

    /// <summary>URL segment. Empty means the site root.</summary>
    public string Slug { get; set; } = "";

    public string Description { get; set; } = "";

    public string NavLabel { get; set; } = "";

    public bool ShowInNav { get; set; } = true;

    public List<PageNode> Nodes { get; set; } = new();

    public bool IsHome => string.IsNullOrEmpty(Slug);

    public string NavText => string.IsNullOrWhiteSpace(NavLabel) ? Title : NavLabel;

    public string OutputPath => string.IsNullOrEmpty(Slug)
        ? "index.html"
        : Slug.Trim('/') + "/index.html";

    public int SlugDepth => string.IsNullOrEmpty(Slug)
        ? 0
        : Slug.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

    public IEnumerable<PageNode> Walk()
    {
        foreach (var node in Nodes)
        {
            foreach (var found in WalkNode(node))
            {
                yield return found;
            }
        }
    }

    private static IEnumerable<PageNode> WalkNode(PageNode node)
    {
        yield return node;

        if (node.Children is null)
        {
            yield break;
        }

        foreach (var child in node.Children)
        {
            foreach (var found in WalkNode(child))
            {
                yield return found;
            }
        }
    }

    public PageNode? Find(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return Walk().FirstOrDefault(node => node.Id == id);
    }

    public PageNode? ParentOf(string id)
    {
        foreach (var node in Walk())
        {
            if (node.Children is not null && node.Children.Any(child => child.Id == id))
            {
                return node;
            }
        }

        return null;
    }
}

/// <summary>A reusable group of blocks stored under <c>app/design/blocks</c>.</summary>
public sealed class BlockDoc
{
    public string Id { get; set; } = Ids.New("block");

    public string Name { get; set; } = "New block";

    public string Description { get; set; } = "";

    /// <summary>"theme" for chrome and layout, "content" for page material.</summary>
    public string Kind { get; set; } = "content";

    /// <summary>A free-form grouping such as Hero, Section or Navigation.</summary>
    public string Category { get; set; } = "Section";

    public List<PageNode> Nodes { get; set; } = new();
}

/// <summary>Supported data field types.</summary>
public static class FieldTypes
{
    public const string Text = "text";
    public const string LongText = "textarea";
    public const string RichText = "richtext";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string Date = "date";
    public const string Select = "select";
    public const string Relation = "relation";
    public const string Image = "image";
    public const string Url = "url";

    public static readonly string[] All =
    [
        Text, LongText, RichText, Number, Boolean, Date, Select, Relation, Image, Url,
    ];

    public static string Label(string type) => type switch
    {
        LongText => "Long text",
        RichText => "Rich text",
        Relation => "Relation",
        Boolean => "Yes / no",
        _ => char.ToUpperInvariant(type[0]) + type[1..],
    };

    public static string Icon(string type) => type switch
    {
        LongText or RichText => "fa-align-left",
        Number => "fa-hashtag",
        Boolean => "fa-toggle-on",
        Date => "fa-calendar",
        Select => "fa-list",
        Relation => "fa-link",
        Image => "fa-image",
        Url => "fa-globe",
        _ => "fa-font",
    };

    public static bool IsTextual(string type) =>
        type is Text or LongText or RichText or Url or Image or Date or Select;
}

/// <summary>One field in a data schema.</summary>
public sealed class DataField
{
    public string Name { get; set; } = "field";

    public string Type { get; set; } = FieldTypes.Text;

    public string? Label { get; set; }

    public bool Required { get; set; }

    public string? Help { get; set; }

    /// <summary>Target schema for a relation field.</summary>
    public string? Target { get; set; }

    public List<string>? Options { get; set; }

    public string Display => string.IsNullOrWhiteSpace(Label) ? Ids.Humanize(Name) : Label!;
}

/// <summary>A data type stored under <c>app/data/schemas</c>.</summary>
public sealed class DataSchema
{
    public string Name { get; set; } = "Item";

    public string Plural { get; set; } = "items";

    public string Icon { get; set; } = "fa-cube";

    public string Description { get; set; } = "";

    /// <summary>
    /// Static sites have no server gate. Public types publish their records as
    /// JSON; private types stay in the project only.
    /// </summary>
    public bool Public { get; set; } = true;

    /// <summary>Page used as the template for individual entity routes.</summary>
    public string? DetailPageId { get; set; }

    public List<DataField> Fields { get; set; } = new();

    public bool HasSlugField => Field("slug") is not null;

    public DataField? Field(string name) =>
        Fields.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));

    public DataField? PrimaryField()
    {
        var textual = Fields.Where(field => field.Type is FieldTypes.Text or FieldTypes.LongText).ToList();

        return textual.FirstOrDefault(field =>
                   field.Name.Equals("title", StringComparison.OrdinalIgnoreCase)
                   || field.Name.Equals("name", StringComparison.OrdinalIgnoreCase)
                   || field.Name.Equals("headline", StringComparison.OrdinalIgnoreCase))
               ?? textual.FirstOrDefault()
               ?? Fields.FirstOrDefault();
    }

    public IEnumerable<DataField> Relations() =>
        Fields.Where(field => field.Type == FieldTypes.Relation && !string.IsNullOrWhiteSpace(field.Target));
}

/// <summary>One authoritative entity record.</summary>
public sealed class EntityDoc
{
    public string Id { get; set; } = Ids.New("ent");

    public string CreatedAt { get; set; } = Ids.Now();

    public string UpdatedAt { get; set; } = Ids.Now();

    /// <summary>Raw field values. Numbers and booleans are stored as text.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    public string Get(string field) => Values.TryGetValue(field, out var value) ? value : "";

    public void Set(string field, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Values.Remove(field);
        }
        else
        {
            Values[field] = value;
        }
    }

    public bool Flag(string field) =>
        Values.TryGetValue(field, out var value)
        && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
}

/// <summary>The project's visual theme.</summary>
public sealed class ThemeDoc
{
    public string Name { get; set; } = "Studio";

    public string Accent { get; set; } = "#2f6b4f";

    public string Background { get; set; } = "#f7f4ee";

    public string Surface { get; set; } = "#fffdf8";

    public string Text { get; set; } = "#1d2a22";

    public string Muted { get; set; } = "#5d6b61";

    public string Border { get; set; } = "#e2ded3";

    public string HeadingFont { get; set; } = "Georgia";

    public string BodyFont { get; set; } = "Segoe UI";

    public int Radius { get; set; } = 10;

    public bool Dark { get; set; }

    public ThemeDoc Clone() => new()
    {
        Name = Name,
        Accent = Accent,
        Background = Background,
        Surface = Surface,
        Text = Text,
        Muted = Muted,
        Border = Border,
        HeadingFont = HeadingFont,
        BodyFont = BodyFont,
        Radius = Radius,
        Dark = Dark,
    };

    public string CssVariables() =>
        "--site-bg:" + Background + ";"
        + "--site-surface:" + Surface + ";"
        + "--site-text:" + Text + ";"
        + "--site-muted:" + Muted + ";"
        + "--site-border:" + Border + ";"
        + "--site-accent:" + Accent + ";"
        + "--site-radius:" + Radius.ToString(CultureInfo.InvariantCulture) + "px;"
        + "--site-font-heading:" + QuoteFont(HeadingFont) + ";"
        + "--site-font-body:" + QuoteFont(BodyFont) + ";";

    public static string QuoteFont(string family)
    {
        var trimmed = family.Trim();

        if (trimmed.Contains(',') || trimmed.Contains('"'))
        {
            return trimmed;
        }

        return "\"" + trimmed + "\", serif";
    }
}

/// <summary>An automation condition.</summary>
public sealed class ConditionDoc
{
    public string Field { get; set; } = "";

    public string Operator { get; set; } = "equals";

    public string Value { get; set; } = "";

    public static readonly string[] Operators =
    [
        "equals", "is not", "contains", "is set", "is empty", "greater than", "less than",
    ];
}

/// <summary>One ordered action in an automation handler.</summary>
public sealed class ActionDoc
{
    public string Type { get; set; } = "log";

    public string Value { get; set; } = "";

    public string? Target { get; set; }

    public static readonly string[] Types =
    [
        "log", "notify", "email", "webhook", "set field", "delay",
    ];
}

/// <summary>An event handler stored in <c>app/automation/handlers.json</c>.</summary>
public sealed class HandlerDoc
{
    public string Id { get; set; } = Ids.New("handler");

    public string Name { get; set; } = "New handler";

    public string Event { get; set; } = "entity.created";

    public string? EntityType { get; set; }

    public bool Enabled { get; set; } = true;

    public List<ConditionDoc> Conditions { get; set; } = new();

    public List<ActionDoc> Actions { get; set; } = new();
}

public sealed class AutomationFile
{
    public List<HandlerDoc> Handlers { get; set; } = new();
}

/// <summary>A plugin discovered in <c>app/code</c>.</summary>
public sealed class PluginInfo
{
    public string Name { get; set; } = "";

    public string Vendor { get; set; } = "";

    public string Version { get; set; } = "1.0.0";

    public string Description { get; set; } = "";

    public string Path { get; set; } = "";

    public string Entry { get; set; } = "";

    public List<string> Capabilities { get; set; } = new();

    public bool HasComponentFolder { get; set; }

    public bool HasScriptFolder { get; set; }

    public bool HasStyleFolder { get; set; }
}

/// <summary>Options for the static site generator.</summary>
public sealed class BuildOptions
{
    public bool PrettyUrls { get; set; } = true;

    public bool SearchIndex { get; set; } = true;

    public bool CopyAssets { get; set; } = true;

    public bool Sitemap { get; set; } = true;

    public string OutputFolder { get; set; } = "build/site";
}

public sealed class BuildResult
{
    public bool Success { get; set; }

    public string OutputPath { get; set; } = "";

    public int Pages { get; set; }

    public int EntityPages { get; set; }

    public int Entities { get; set; }

    public int Assets { get; set; }

    public long Milliseconds { get; set; }

    public List<string> Log { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
}


