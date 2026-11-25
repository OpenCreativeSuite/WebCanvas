using Donkeh.UILab;
using Donkeh.Windowing;
using static Donkeh.UILab.HtmlHelper;
using static Donkeh.UILab._;

namespace WebCanvasDotNet.Core;

/// <summary>
/// Turns page JSON into live UILab nodes for the design canvas. The same rules
/// decide which entities a collection shows, so the canvas and the static build
/// agree on ordering.
/// </summary>
public static class SiteRenderer
{
    public static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "input", "hr", "br", "meta", "link", "source", "track", "area", "base", "col", "embed",
    };

    /// <summary>
    /// Interaction state for the design canvas. The same JSON tree feeds this
    /// renderer and the static publisher; only the chrome and event handlers
    /// change while editing.
    /// </summary>
    public sealed class CanvasEdit
    {
        public string? SelectedId { get; init; }

        public Action<string>? Select { get; init; }

        public bool Editing { get; init; } = true;

        public bool Dragging { get; init; }

        /// <summary>Canvas zoom. Layout boxes are unscaled, screen boxes are not.</summary>
        public float Scale { get; init; } = 1f;

        /// <summary>Minimum page height in unscaled CSS pixels.</summary>
        public int CanvasMinHeight { get; init; } = 640;

        /// <summary>
        /// Live check used by drag handlers. Handlers must exist before a drag
        /// starts, so they ask the session whether a widget is actually in flight
        /// rather than relying on the render snapshot.
        /// </summary>
        public Func<bool>? IsDragging { get; init; }

        public DropHint? Hint { get; init; }

        public Action<string, DropPosition, DropHint>? Hover { get; init; }

        public Action<string, DropPosition, int>? Drop { get; init; }
    }

    public static Node RenderPage(PageDoc page, WcProject project, CanvasEdit edit)
    {
        var children = new List<Node?>();

        if (page.Nodes.Count == 0 && edit.Editing)
        {
            children.Add(RenderEmptyCanvas(edit));
        }

        foreach (var node in page.Nodes)
        {
            AppendNodeWithHint(children, node, project, page, edit, null);
        }

        var classes = new List<string> { "wc-site", "wc-canvas" };

        if (edit.Editing)
        {
            classes.Add("is-editing");
        }

        if (edit.Dragging)
        {
            classes.Add("is-dragging");
        }

        var rootProps = new Props
        {
            ClassName = string.Join(' ', classes),
            Key = "wc-canvas-root",
            Attributes = new Dictionary<string, string>
            {
                ["style"] = project.Theme.CssVariables()
                            + "min-height:" + edit.CanvasMinHeight + "px;",
            },
        };

        if (edit.Editing)
        {
            rootProps = new Props
            {
                ClassName = rootProps.ClassName,
                Key = rootProps.Key,
                Attributes = rootProps.Attributes,
                OnDragOver = drag =>
                {
                    if (edit.IsDragging?.Invoke() != true)
                    {
                        return;
                    }

                    drag.PreventDefault();
                    drag.Data.DropEffect = "copy";
                    edit.Hover?.Invoke("", DropPosition.After, BoxFor(drag, "", edit.Scale));
                },
                OnDrop = drop =>
                {
                    if (edit.IsDragging?.Invoke() != true)
                    {
                        return;
                    }

                    drop.PreventDefault();
                    edit.Drop?.Invoke("", DropPosition.After, -1);
                },
            };
        }

        return Div(rootProps, children.ToArray()!);
    }

    private static Node RenderEmptyCanvas(CanvasEdit edit)
    {
        DropPosition position = DropPosition.Inside;

        return Div(new Props
        {
            ClassName = "wc-empty-canvas",
            Key = "wc-empty-canvas",
            OnDragOver = drag =>
            {
                if (edit.IsDragging?.Invoke() != true)
                {
                    return;
                }

                drag.PreventDefault();
                drag.Data.DropEffect = "copy";
                edit.Hover?.Invoke("", position, BoxFor(drag, "", edit.Scale));
            },
            OnDrop = drop =>
            {
                if (edit.IsDragging?.Invoke() != true)
                {
                    return;
                }

                drop.PreventDefault();
                edit.Drop?.Invoke("", position, -1);
            },
        },
            Span(new Props { ClassName = "wc-empty-icon" }, Text("+")),
            Span(new Props { ClassName = "wc-empty-title" }, Text("Drop your first widget here")),
            Span(new Props { ClassName = "wc-empty-copy" }, Text("Drag from the Widgets palette, or click a widget to add it.")));
    }

    public static Node RenderNode(
        PageNode node,
        WcProject project,
        PageDoc page,
        CanvasEdit edit,
        HashSet<string>? blockStack) =>
        RenderNodeCore(node, project, page, edit, blockStack);

    /// <summary>
    /// Appends a node and, while dragging, a full-height drop slot immediately
    /// before or after it. The slot is a sibling, not a wrapper, so the target
    /// element keeps its identity and a slot can never be left behind.
    /// </summary>
    private static void AppendNodeWithHint(
        List<Node?> children,
        PageNode node,
        WcProject project,
        PageDoc page,
        CanvasEdit edit,
        HashSet<string>? blockStack)
    {
        children.Add(RenderNode(node, project, page, edit, blockStack));
    }

    private static Node RenderNodeCore(
        PageNode node,
        WcProject project,
        PageDoc page,
        CanvasEdit edit,
        HashSet<string>? blockStack)
    {
        if (node.Tag.Equals("block", StringComparison.OrdinalIgnoreCase))
        {
            return RenderBlock(node, project, page, edit, blockStack);
        }

        bool selected = edit.Editing && string.Equals(node.Id, edit.SelectedId, StringComparison.Ordinal);
        var classes = new List<string> { "wc-node" };

        if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            classes.Add(node.ClassName!);
        }

        if (selected)
        {
            classes.Add("is-selected");
        }

        bool dropInside = edit.Dragging
                          && edit.Hint is { Position: DropPosition.Inside } hint
                          && hint.TargetId == node.Id;

        if (dropInside)
        {
            classes.Add("is-drop-inside");
        }

        var attrs = new Dictionary<string, string>();

        if (node.Style is not null)
        {
            attrs["style"] = string.Join(";", node.Style.Select(pair => pair.Key + ":" + pair.Value));
        }

        if (node.Attrs is not null)
        {
            foreach (var (key, value) in node.Attrs)
            {
                if (key is "nav" or "active" or "limit" or "type")
                {
                    continue;
                }

                attrs[key] = value;
            }
        }

        if (edit.Editing)
        {
            attrs["data-node-id"] = node.Id;
        }

        var props = new Props
        {
            ClassName = string.Join(' ', classes),
            Key = node.Id,
            Attributes = attrs,
            OnClickEvent = edit.Editing ? click =>
            {
                click.StopPropagation();
                edit.Select?.Invoke(node.Id);
            } : null,
        };

        if (edit.Editing)
        {
            props = WithDropHandlers(props, node, edit);
        }

        // Custom HTML is opaque to the native canvas. Reserve its measured box
        // and show a selectable placeholder; the publisher emits the real HTML.
        if (node.Tag.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            classes.Add("wc-html-block");
            int width = StudioSession.HtmlBlockWidth(node);
            int height = StudioSession.HtmlBlockHeight(node);
            var style = new List<string>();

            if (width > 0)
            {
                style.Add("width:" + width + "px");
                style.Add("max-width:100%");
            }

            style.Add("min-height:" + (height > 0 ? height : 160) + "px");

            if (attrs.TryGetValue("style", out string? existingStyle) && existingStyle.Length > 0)
            {
                style.Insert(0, existingStyle);
            }

            attrs.Remove("css");
            attrs.Remove("width");
            attrs.Remove("height");
            attrs["style"] = string.Join(";", style);

            var htmlProps = new Props
            {
                ClassName = string.Join(' ', classes),
                Key = node.Id,
                Attributes = attrs,
                OnClickEvent = edit.Editing ? click =>
                {
                    click.StopPropagation();
                    edit.Select?.Invoke(node.Id);
                } : null,
            };

            if (edit.Editing)
            {
                htmlProps = WithDropHandlers(htmlProps, node, edit);
            }

            return Div(htmlProps,
                Span(new Props { ClassName = "wc-html-label" }, Text("HTML block")),
                Span(new Props { ClassName = "wc-html-snippet" }, Text("Custom markup and CSS")));
        }

        // Embeds are opaque in a browser; the design canvas shows a selectable
        // preview card while the publisher emits the real iframe.
        if (node.Tag.Equals("iframe", StringComparison.OrdinalIgnoreCase))
        {
            classes.Add("wc-embed");
            string embedSource = node.Attrs is not null && node.Attrs.TryGetValue("src", out var src) ? src : "";
            string title = node.Attrs is not null && node.Attrs.TryGetValue("title", out var heading) ? heading : "Embedded content";
            var embedProps = new Props
            {
                ClassName = string.Join(' ', classes),
                Key = node.Id,
                Attributes = attrs,
                OnClickEvent = edit.Editing ? click =>
                {
                    click.StopPropagation();
                    edit.Select?.Invoke(node.Id);
                } : null,
            };

            if (edit.Editing)
            {
                embedProps = WithDropHandlers(embedProps, node, edit);
            }

            return Div(embedProps,
                Span(new Props { ClassName = "wc-embed-icon" }, Text("▶")),
                Span(new Props { ClassName = "wc-embed-title" }, Text(title)),
                Span(new Props { ClassName = "wc-embed-source" }, Text(embedSource)));
        }

        // Collections and navigation are generated from project data.
        if (node.Tag.Equals("collection", StringComparison.OrdinalIgnoreCase))
        {
            var cards = CollectionCards(node, project, page);
            return Div(props, cards.ToArray()!);
        }

        if (node.Tag.Equals("nav", StringComparison.OrdinalIgnoreCase)
            || (node.Attrs is not null && node.Attrs.ContainsKey("nav")))
        {
            return RenderNav(node, project, page, classes, attrs, edit);
        }

        var children = new List<Node?>();

        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            children.Add(Text(node.Text));
        }

        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                AppendNodeWithHint(children, child, project, page, edit, blockStack);
            }
        }

        if (node.Tag.Equals("img", StringComparison.OrdinalIgnoreCase)
            && (!node.Attrs?.TryGetValue("src", out var source) ?? true || string.IsNullOrWhiteSpace(source)))
        {
            classes.Add("wc-media");
            var mediaProps = new Props
            {
                ClassName = string.Join(' ', classes),
                Key = node.Id,
                Attributes = attrs,
                OnClickEvent = edit.Editing ? click =>
                {
                    click.StopPropagation();
                    edit.Select?.Invoke(node.Id);
                } : null,
            };

            if (edit.Editing)
            {
                mediaProps = WithDropHandlers(mediaProps, node, edit);
            }

            return Div(mediaProps, Text("Image"));
        }

        if (VoidTags.Contains(node.Tag))
        {
            return H(node.Tag, props);
        }

        return H(node.Tag, props, children.ToArray()!);
    }

    private static Props WithDropHandlers(Props props, PageNode node, CanvasEdit edit) =>
        new()
        {
            ClassName = props.ClassName,
            Key = props.Key,
            Attributes = props.Attributes,
            OnClickEvent = props.OnClickEvent,
            OnDragOver = drag =>
            {
                if (edit.IsDragging?.Invoke() != true)
                {
                    return;
                }

                drag.PreventDefault();
                drag.Data.DropEffect = "copy";
                DropPosition position = DropPositionFor(drag, node, edit.Scale);
                DropHint hint = BoxFor(drag, node.Id, edit.Scale);
                hint.Position = position;
                hint.Index = position == DropPosition.Inside
                    ? DropIndexFor(drag, node, edit.Scale)
                    : -1;
                edit.Hover?.Invoke(node.Id, position, hint);
            },
            OnDrop = drop =>
            {
                if (edit.IsDragging?.Invoke() != true)
                {
                    return;
                }

                drop.PreventDefault();
                DropPosition position = DropPositionFor(drop, node, edit.Scale);
                int index = position == DropPosition.Inside
                    ? DropIndexFor(drop, node, edit.Scale)
                    : -1;
                edit.Drop?.Invoke(node.Id, position, index);
            },
        };

    private static DropPosition DropPositionFor(DragEvent drag, PageNode node, float scale)
    {
        DropHint hint = BoxFor(drag, node.Id, scale);
        float height = hint.Height;

        if (height <= 0)
        {
            return node.IsContainer ? DropPosition.Inside : DropPosition.After;
        }

        // A block reference is a single unit in the page: drop around it, not
        // into it. Containers get a middle band that appends a child.
        if (!node.IsContainer || node.Tag.Equals("block", StringComparison.OrdinalIgnoreCase))
        {
            return drag.LocalY < height / 2f ? DropPosition.Before : DropPosition.After;
        }

        if (drag.LocalY < height * 0.22f)
        {
            return DropPosition.Before;
        }

        if (drag.LocalY > height * 0.78f)
        {
            return DropPosition.After;
        }

        return DropPosition.Inside;
    }

    /// <summary>
    /// Maps a pointer inside a container to the nearest child boundary. This is
    /// what makes the drop slot follow the pointer instead of snapping to the
    /// bottom of a tall card.
    /// </summary>
    private static int DropIndexFor(DragEvent drag, PageNode node, float scale)
    {
        if (!node.IsContainer
            || node.Children is not { Count: > 0 } children
            || drag.CurrentTarget is not { } container
            || AppRootHost.Current?.FrameResult is not { } layout
            || !layout.TryGetBox(container, out var containerBox))
        {
            return -1;
        }

        for (int index = 0; index < children.Count; index++)
        {
            if (FindDescendant(container, children[index].Id) is not { } element
                || !layout.TryGetBox(element, out var box))
            {
                continue;
            }

            float top = (box.BorderBox.Y - containerBox.BorderBox.Y) * scale;
            float middle = top + (box.BorderBox.Height * scale * 0.5f);

            if (drag.LocalY < middle)
            {
                return index;
            }
        }

        return children.Count;
    }

    private static ElementNode? FindDescendant(ElementNode parent, string nodeId)
    {
        foreach (var child in parent.Children)
        {
            if (child is not ElementNode element)
            {
                continue;
            }

            if (element.TryGetAttribute("data-node-id", out var value) && value == nodeId)
            {
                return element;
            }

            if (FindDescendant(element, nodeId) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static DropHint BoxFor(DragEvent drag, string targetId, float scale)
    {
        var hint = new DropHint { TargetId = targetId };

        hint.PointerX = drag.ClientX;
        hint.PointerY = drag.ClientY;

        if (drag.CurrentTarget is { } element
            && AppRootHost.Current?.FrameResult is { } layout
            && layout.TryGetBox(element, out var box))
        {
            // The canvas paper is scaled with a CSS transform. Layout boxes are
            // unscaled, while LocalX/LocalY and ClientX/ClientY are painted
            // window pixels. Using the pointer and its local offset gives the
            // real on-screen top-left without needing the paper origin.
            hint.X = drag.ClientX - drag.LocalX;
            hint.Y = drag.ClientY - drag.LocalY;
            hint.Width = box.BorderBox.Width * scale;
            hint.Height = box.BorderBox.Height * scale;
        }

        return hint;
    }

    private static Node RenderBlock(
        PageNode node,
        WcProject project,
        PageDoc page,
        CanvasEdit edit,
        HashSet<string>? blockStack)
    {
        string? blockId = node.Attrs is not null && node.Attrs.TryGetValue("block", out string? id) ? id : null;
        BlockDoc? block = project.Block(blockId);

        if (block is null)
        {
            var missing = new Props
            {
                ClassName = "wc-node wc-block-missing",
                Key = node.Id,
                OnClickEvent = edit.Editing ? click =>
                {
                    click.StopPropagation();
                    edit.Select?.Invoke(node.Id);
                } : null,
            };

            if (edit.Editing)
            {
                missing = WithDropHandlers(missing, node, edit);
            }

            return Div(missing, Text("Missing block"));
        }

        blockStack ??= new HashSet<string>(StringComparer.Ordinal);

        if (!blockStack.Add(block.Id))
        {
            return Div(new Props { ClassName = "wc-node wc-block-missing", Key = node.Id }, Text("Recursive block"));
        }

        List<Node> children = [];

        foreach (PageNode child in block.Nodes)
        {
            children.Add(RenderNode(child, project, page, new CanvasEdit
            {
                Editing = false,
                SelectedId = null,
            }, blockStack));
        }

        List<string> classes = ["wc-block-instance"];

        if (edit.Editing)
        {
            classes.Add("wc-node");

            if (string.Equals(node.Id, edit.SelectedId, StringComparison.Ordinal))
            {
                classes.Add("is-selected");
            }

            if (edit.Dragging
                && edit.Hint is { Position: DropPosition.Inside } hint
                && hint.TargetId == node.Id)
            {
                classes.Add("is-drop-inside");
            }
        }

        var props = new Props
        {
            ClassName = string.Join(' ', classes),
            Key = node.Id,
            Attributes = edit.Editing ? new Dictionary<string, string> { ["data-node-id"] = node.Id } : null,
            OnClickEvent = edit.Editing ? click =>
            {
                click.StopPropagation();
                edit.Select?.Invoke(node.Id);
            } : null,
        };

        if (edit.Editing)
        {
            props = WithDropHandlers(props, node, edit);
        }

        Node content = Div(props, children.ToArray()!);

        if (edit.Editing)
        {
            content = Div(new Props { ClassName = "wc-block-wrap", Key = node.Id + "-wrap" },
                Span(new Props { ClassName = "wc-block-label" }, Text("Block · " + block.Name)),
                content);
        }

        blockStack.Remove(block.Id);
        return content;
    }
    private static Node RenderNav(
        PageNode node,
        WcProject project,
        PageDoc page,
        List<string> classes,
        Dictionary<string, string> attrs,
        CanvasEdit edit)
    {
        var active = node.Attrs is not null && node.Attrs.TryGetValue("active", out var slug) ? slug : page.Slug;
        var links = new List<Node?>();

        foreach (var target in NavPages(project))
        {
            var isActive = string.Equals(target.Slug, active, StringComparison.OrdinalIgnoreCase);
            var linkClasses = isActive ? "is-active" : null;

            links.Add(A(new Props
            {
                ClassName = linkClasses,
                Href = HrefFor(target, page),
                OnClickEvent = edit.Editing ? click =>
                {
                    click.StopPropagation();
                    edit.Select?.Invoke(node.Id);
                } : null,
            }, Text(target.NavText)));
        }

        var props = new Props
        {
            ClassName = string.Join(' ', classes),
            Key = node.Id,
            Attributes = attrs,
            OnClickEvent = edit.Editing ? click =>
            {
                click.StopPropagation();
                edit.Select?.Invoke(node.Id);
            } : null,
        };

        if (edit.Editing)
        {
            props = WithDropHandlers(props, node, edit);
        }

        return H(node.Tag == "nav" ? "nav" : "div", props, links.ToArray()!);
    }
    public static List<PageDoc> NavPages(WcProject project) =>
        project.Pages
            .Where(page => page.ShowInNav)
            .Where(page => !project.Schemas.Any(schema => schema.DetailPageId == page.Id))
            .OrderByDescending(page => page.IsHome)
            .ThenBy(page => page.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string HrefFor(PageDoc target, PageDoc from) =>
        "/" + target.OutputPath.Replace("index.html", "", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The last path segment for a record route. Records with a <c>slug</c>
    /// field use their slug; everything else falls back to the record id.
    /// </summary>
    public static string EntitySegment(DataSchema schema, EntityDoc entity)
    {
        if (schema.HasSlugField)
        {
            var slug = Ids.Slug(entity.Get("slug"));

            if (slug.Length > 0)
            {
                return slug;
            }
        }

        return entity.Id;
    }

    public static string EntityPath(DataSchema schema, EntityDoc entity) =>
        "/" + Ids.Slug(schema.Plural) + "/" + EntitySegment(schema, entity);

    public static List<EntityDoc> CollectionEntities(WcProject project, PageNode node)
    {
        var schemaName = node.Attrs is not null && node.Attrs.TryGetValue("type", out var type)
            ? type
            : "Post";

        var schema = project.Schema(schemaName)
                     ?? project.Schemas.FirstOrDefault(candidate =>
                         candidate.Plural.Equals(schemaName, StringComparison.OrdinalIgnoreCase));

        if (schema is null)
        {
            return new List<EntityDoc>();
        }

        var limit = 6;

        if (node.Attrs is not null
            && node.Attrs.TryGetValue("limit", out var raw)
            && int.TryParse(raw, out var parsed))
        {
            limit = Math.Clamp(parsed, 1, 60);
        }

        var entities = project.EntityList(schema);

        return schema.Name switch
        {
            "Post" => entities
                .OrderByDescending(entity => entity.Get("publishedAt"), StringComparer.Ordinal)
                .ThenBy(entity => entity.Get("title"), StringComparer.Ordinal)
                .Take(limit)
                .ToList(),
            "Project" => entities
                .OrderByDescending(entity => entity.Get("year"), StringComparer.Ordinal)
                .ThenBy(entity => entity.Get("title"), StringComparer.Ordinal)
                .Take(limit)
                .ToList(),
            _ => entities
                .OrderBy(entity => entity.Get("name"), StringComparer.Ordinal)
                .ThenBy(entity => entity.Get("title"), StringComparer.Ordinal)
                .Take(limit)
                .ToList(),
        };
    }

    private static List<Node> CollectionCards(PageNode node, WcProject project, PageDoc page)
    {
        var schemaName = node.Attrs is not null && node.Attrs.TryGetValue("type", out var type) ? type : "Post";
        var schema = project.Schema(schemaName)
                     ?? project.Schemas.FirstOrDefault(candidate =>
                         candidate.Plural.Equals(schemaName, StringComparison.OrdinalIgnoreCase));

        var cards = new List<Node>();

        if (schema is null)
        {
            cards.Add(Div(new() { ClassName = "post-card" },
                Div(new() { ClassName = "post-body" },
                    Div(new() { ClassName = "post-meta" }, Text("Collection")),
                    Div(new() { ClassName = "post-title" }, Text("Add a “" + schemaName + "” data type to show this list.")))));
            return cards;
        }

        foreach (var entity in CollectionEntities(project, node))
        {
            cards.Add(schema.Name == "Author"
                ? AuthorCard(schema, entity, project)
                : EntityCard(schema, entity, project));
        }

        if (cards.Count == 0)
        {
            cards.Add(Div(new() { ClassName = "post-card" },
                Div(new() { ClassName = "post-body" },
                    Div(new() { ClassName = "post-meta" }, Text(schema.Name)),
                    Div(new() { ClassName = "post-title" }, Text("Nothing here yet.")),
                    Div(new() { ClassName = "post-excerpt" }, Text("Add a record in Content and it will appear in this collection.")))));
        }

        return cards;
    }

    private static Node EntityCard(DataSchema schema, EntityDoc entity, WcProject project)
    {
        var title = project.TitleOf(schema, entity);
        var summary = entity.Get("excerpt");

        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = entity.Get("summary");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = entity.Get("description");
        }

        var metaParts = new List<string>();

        if (schema.Name == "Post")
        {
            var date = entity.Get("publishedAt");

            if (DateTimeOffset.TryParse(date, out var published))
            {
                metaParts.Add(Ids.Clock(published));
            }

            if (schema.Field("author") is { } author)
            {
                var label = project.RelationLabel(schema, author, entity.Get("author"));

                if (!string.IsNullOrWhiteSpace(label))
                {
                    metaParts.Add(label);
                }
            }
        }
        else if (schema.Name == "Project")
        {
            var client = entity.Get("client");
            var year = entity.Get("year");

            if (!string.IsNullOrWhiteSpace(client))
            {
                metaParts.Add(client);
            }

            if (!string.IsNullOrWhiteSpace(year))
            {
                metaParts.Add(year);
            }
        }

        var thumbClass = (Math.Abs(entity.Id.GetHashCode()) % 3) switch
        {
            1 => "post-thumb is-warm",
            2 => "post-thumb is-slate",
            _ => "post-thumb",
        };

        var body = new List<Node?>
        {
            Div(new() { ClassName = "post-meta" }, Text(string.Join(" · ", metaParts))),
            Div(new() { ClassName = "post-title" }, Text(title)),
        };

        if (!string.IsNullOrWhiteSpace(summary))
        {
            body.Add(Div(new() { ClassName = "post-excerpt" }, Text(summary)));
        }

        return Div(new() { ClassName = "post-card" },
            Div(new() { ClassName = thumbClass }),
            Div(new() { ClassName = "post-body" }, body.ToArray()!));
    }

    private static Node AuthorCard(DataSchema schema, EntityDoc entity, WcProject project)
    {
        var name = project.TitleOf(schema, entity);
        var initial = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[0].ToString().ToUpperInvariant();

        return Div(new() { ClassName = "team-card" },
            Div(new() { ClassName = "avatar" }, Text(initial)),
            Div(new() { ClassName = "team-copy" },
                Div(new() { ClassName = "team-name" }, Text(name)),
                Div(new() { ClassName = "team-role" }, Text(entity.Get("role")))));
    }
}
