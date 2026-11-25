using System.Text.Json;

namespace WebCanvasDotNet.Core;

public sealed class BlockPreset
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Description { get; set; } = "";

    public string Group { get; set; } = "Blocks";
}

public sealed partial class StudioSession
{
    public bool CanvasEditing { get; private set; } = true;

    public HashSet<string> CollapsedNodes { get; } = new(StringComparer.Ordinal);

    public HashSet<string> CollapsedSections { get; } = new(StringComparer.Ordinal);

    public string PageSearch { get; set; } = "";

    public static readonly BlockPreset[] Presets =
    [
        // Layout
        new() { Id = "section", Label = "Section", Icon = "fa-layer-group", Description = "A titled content section", Group = "Layout" },
        new() { Id = "full-width", Label = "Full width", Icon = "fa-arrows-left-right", Description = "An edge-to-edge row you can nest", Group = "Layout" },
        new() { Id = "grid-2", Label = "2 column grid", Icon = "fa-table-columns", Description = "Two equal cards", Group = "Layout" },
        new() { Id = "grid-3", Label = "3 column grid", Icon = "fa-table-columns", Description = "Three equal cards", Group = "Layout" },
        new() { Id = "grid-4", Label = "4 column grid", Icon = "fa-table-columns", Description = "Four compact cards", Group = "Layout" },
        new() { Id = "columns", Label = "Feature row", Icon = "fa-layer-group", Description = "Three icon features", Group = "Layout" },
        new() { Id = "band", Label = "Callout band", Icon = "fa-square", Description = "A full-width prompt", Group = "Layout" },
        new() { Id = "spacer", Label = "Spacer", Icon = "fa-arrows-up-down", Description = "Vertical breathing room", Group = "Layout" },
        new() { Id = "divider", Label = "Divider", Icon = "fa-minus", Description = "A hairline rule", Group = "Layout" },

        // Text
        new() { Id = "heading", Label = "Heading", Icon = "fa-heading", Description = "A section title", Group = "Text" },
        new() { Id = "subtitle", Label = "Subtitle", Icon = "fa-heading", Description = "A smaller section heading", Group = "Text" },
        new() { Id = "text", Label = "Paragraph", Icon = "fa-align-left", Description = "A block of body copy", Group = "Text" },
        new() { Id = "eyebrow", Label = "Eyebrow", Icon = "fa-tag", Description = "A small uppercase label", Group = "Text" },
        new() { Id = "link", Label = "Text link", Icon = "fa-link", Description = "An inline link", Group = "Text" },
        new() { Id = "list", Label = "Bullet list", Icon = "fa-list", Description = "A short list of points", Group = "Text" },
        new() { Id = "quote", Label = "Quote", Icon = "fa-quote-left", Description = "A pull quote", Group = "Text" },
        new() { Id = "code", Label = "Code block", Icon = "fa-code", Description = "A preformatted code sample", Group = "Text" },

        // Content
        new() { Id = "hero", Label = "Hero with text", Icon = "fa-star", Description = "Headline, copy and actions", Group = "Content" },
        new() { Id = "media-left", Label = "Image with text", Icon = "fa-image", Description = "Image beside copy", Group = "Content" },
        new() { Id = "media-right", Label = "Text with image", Icon = "fa-image", Description = "Copy beside an image", Group = "Content" },
        new() { Id = "image-title", Label = "Image with title", Icon = "fa-image", Description = "A captioned image card", Group = "Content" },
        new() { Id = "buttons", Label = "Button group", Icon = "fa-arrow-right", Description = "Primary and secondary actions", Group = "Content" },
        new() { Id = "button", Label = "Button", Icon = "fa-arrow-right", Description = "A single primary action", Group = "Content" },
        new() { Id = "image", Label = "Image", Icon = "fa-image", Description = "A responsive image", Group = "Content" },
        new() { Id = "youtube", Label = "YouTube", Icon = "fa-play", Description = "A responsive video embed", Group = "Content" },
        new() { Id = "embed", Label = "Embed", Icon = "fa-globe", Description = "An embedded page or tool", Group = "Content" },
        new() { Id = "cards", Label = "Card row", Icon = "fa-copy", Description = "Two editable cards", Group = "Content" },
        new() { Id = "stats", Label = "Stat row", Icon = "fa-hashtag", Description = "Three numbers that matter", Group = "Content" },
        new() { Id = "logos", Label = "Logo row", Icon = "fa-star", Description = "A quiet trust strip", Group = "Content" },
        new() { Id = "faq", Label = "FAQ", Icon = "fa-circle-question", Description = "Three questions and answers", Group = "Content" },

        // Dynamic
        new() { Id = "collection", Label = "Collection", Icon = "fa-database", Description = "Records from a data type", Group = "Dynamic" },
    ];

    // ------------------------------------------------------------------ pages

    public void NewPage()
    {
        if (Project is null)
        {
            return;
        }

        var name = UniquePageTitle("Untitled page");

        OpenPrompt(new PromptRequest
        {
            Title = "New page",
            Label = "Page title",
            Value = name,
            Placeholder = "About",
            Confirm = "Create page",
            OnConfirm = value =>
            {
                if (Project is null)
                {
                    return;
                }

                var title = string.IsNullOrWhiteSpace(value) ? "Untitled page" : value.Trim();
                var page = new PageDoc
                {
                    Title = title,
                    Slug = UniqueSlug(Ids.Slug(title)),
                    NavLabel = "",
                    ShowInNav = true,
                    Nodes =
                    {
                        PageNode.Create("section", "page-head site-shell", null, null,
                            PageNode.Create("span", "eyebrow", "Page"),
                            PageNode.Create("h1", "page-title", title),
                            PageNode.Create("p", "page-lead", "Add a sentence or two about this page.")),
                        PageNode.Create("section", "site-section site-shell", null, null,
                            PageNode.Create("div", "section-head", null, null,
                                PageNode.Create("div", null, null, null,
                                    PageNode.Create("span", "section-kicker", "Section"),
                                    PageNode.Create("h2", "section-title", "Start writing here."))),
                            PageNode.Create("div", "feature-grid", null, null,
                                PageNode.Create("div", "feature", null, null,
                                    PageNode.Create("h3", "feature-title", "A first block"),
                                    PageNode.Create("p", "feature-copy", "Select any block on the canvas to edit it in the inspector.")))),
                    },
                };

                try
                {
                    Project.Pages.Add(page);
                    Project.SavePage(page);
                    SelectedPageId = page.Id;
                    SelectedNodeId = null;
                    OpenTab(StudioMode.Design, page.Id, page.Title, "fa-shapes");
                    Notify("Created “" + page.Title + "”.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void RenamePage(PageDoc page)
    {
        OpenPrompt(new PromptRequest
        {
            Title = "Rename page",
            Label = "Page title",
            Value = page.Title,
            Confirm = "Rename",
            OnConfirm = value =>
            {
                var title = string.IsNullOrWhiteSpace(value) ? page.Title : value.Trim();

                try
                {
                    page.Title = title;
                    Project!.SavePage(page);
                    SyncTabTitle(StudioMode.Design, page.Id, title, "fa-shapes");
                    Notify("Page renamed.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void SetPageSlug(PageDoc page, string slug)
    {
        var cleaned = Ids.SlugPath(slug);

        if (cleaned == page.Slug)
        {
            return;
        }

        if (Project is null)
        {
            return;
        }

        if (cleaned.Length == 0 && Project.Pages.Any(candidate => candidate.Id != page.Id && candidate.IsHome))
        {
            Notify("Another page is already the site home.", "warning");
            return;
        }

        if (Project.Pages.Any(candidate =>
                candidate.Id != page.Id && string.Equals(candidate.Slug, cleaned, StringComparison.OrdinalIgnoreCase)))
        {
            Notify("Another page already uses that address.", "warning");
            return;
        }

        try
        {
            page.Slug = cleaned;
            Project.SavePage(page);
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void SetPageMeta(PageDoc page, Action<PageDoc> edit)
    {
        try
        {
            edit(page);
            Project!.SavePage(page);
            SyncTabTitle(StudioMode.Design, page.Id, page.Title, "fa-shapes");
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void DeletePage(PageDoc page)
    {
        if (Project is null)
        {
            return;
        }

        if (Project.Pages.Count <= 1)
        {
            Notify("A project needs at least one page.", "warning");
            return;
        }

        if (Project.Schemas.FirstOrDefault(schema => schema.DetailPageId == page.Id) is { } template)
        {
            AskConfirm(new ConfirmRequest
            {
                Title = "“" + template.Name + "” uses this page",
                Message = "This page renders individual " + template.Name + " records, so it can't be deleted while the type points at it. "
                          + "Delete the entities first, or choose a different detail page in Data.",
                Confirm = "Got it",
                Danger = false,
                ShowCancel = false,
            });
            return;
        }

        AskConfirm(new ConfirmRequest
        {
            Title = "Delete “" + page.Title + "”?",
            Message = "The page file is removed from the project. This cannot be undone.",
            Confirm = "Delete page",
            OnConfirm = () =>
            {
                try
                {
                    Project.DeletePage(page);
                    var tab = Tabs.FirstOrDefault(candidate => candidate.Mode == StudioMode.Design && candidate.ItemId == page.Id);

                    if (tab is not null)
                    {
                        Tabs.Remove(tab);
                    }

                    if (SelectedPageId == page.Id)
                    {
                        SelectedPageId = Project.HomePage?.Id;
                        SelectedNodeId = null;
                    }

                    if (ActiveTabId is null && Project.HomePage is { } home)
                    {
                        OpenTab(StudioMode.Design, home.Id, home.Title, "fa-shapes");
                    }

                    Notify("Page deleted.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    private string UniquePageTitle(string seed)
    {
        var title = seed;
        var index = 2;

        while (Project!.Pages.Any(page => page.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
        {
            title = seed + " " + index++;
        }

        return title;
    }

    private string UniqueSlug(string seed)
    {
        if (Project is null)
        {
            return seed;
        }

        var slug = seed;
        var index = 2;

        while (Project.Pages.Any(page => page.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
        {
            slug = seed + "-" + index++;
        }

        return slug;
    }

    // ------------------------------------------------------------- node edits

    public void ToggleCanvasEditing()
    {
        CanvasEditing = !CanvasEditing;
        MarkChanged();
    }

    public void SetPreview(bool preview)
    {
        CanvasEditing = !preview;
        MarkChanged();
    }

    public void MoveNode(int direction)
    {
        var page = CurrentPage;
        var node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        var parent = page.ParentOf(node.Id);
        var siblings = parent?.Children ?? page.Nodes;
        var index = siblings.FindIndex(candidate => candidate.Id == node.Id);
        var target = index + direction;

        if (index < 0 || target < 0 || target >= siblings.Count)
        {
            return;
        }

        Capture(page, direction < 0 ? "Move node up" : "Move node down");
        (siblings[index], siblings[target]) = (siblings[target], siblings[index]);
        SaveCurrentPage(page, direction < 0 ? "Moved node up" : "Moved node down");
    }

    public void IndentNode()
    {
        PageDoc? page = CurrentPage;
        PageNode? node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        PageNode? parent = page.ParentOf(node.Id);
        List<PageNode> siblings = parent?.Children ?? page.Nodes;
        int index = siblings.FindIndex(candidate => candidate.Id == node.Id);

        if (index <= 0 || !IsContainer(siblings[index - 1]))
        {
            Notify("The block above isn't a container, so this can't be nested there.", "info");
            return;
        }

        Capture(page, "Indent block");
        siblings.RemoveAt(index);
        PageNode target = siblings[index - 1];
        target.Children ??= new List<PageNode>();
        target.Children.Add(node);
        CollapsedNodes.Remove(target.Id);
        SelectedNodeId = node.Id;
        SaveCurrentPage(page, "Nested block");
    }

    public void OutdentNode()
    {
        PageDoc? page = CurrentPage;
        PageNode? node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        PageNode? parent = page.ParentOf(node.Id);

        if (parent is null)
        {
            return;
        }

        PageNode? grandparent = page.ParentOf(parent.Id);
        List<PageNode> outside = grandparent?.Children ?? page.Nodes;
        List<PageNode> inside = parent.Children ?? new List<PageNode>();
        int insideIndex = inside.FindIndex(candidate => candidate.Id == node.Id);
        int parentIndex = outside.FindIndex(candidate => candidate.Id == parent.Id);

        if (insideIndex < 0 || parentIndex < 0)
        {
            return;
        }

        Capture(page, "Outdent block");
        inside.RemoveAt(insideIndex);
        outside.Insert(parentIndex + 1, node);
        SelectedNodeId = node.Id;
        SaveCurrentPage(page, "Moved block out");
    }

    public void DuplicateNode()
    {
        var page = CurrentPage;
        var node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        var parent = page.ParentOf(node.Id);
        var siblings = parent?.Children ?? page.Nodes;
        var index = siblings.FindIndex(candidate => candidate.Id == node.Id);

        if (index < 0)
        {
            return;
        }

        Capture(page, "Duplicate node");
        var copy = node.Clone();
        siblings.Insert(index + 1, copy);
        SelectedNodeId = copy.Id;
        SaveCurrentPage(page, "Duplicated node");
    }

    public void CopyNode()
    {
        if (CurrentNode is not { } node)
        {
            return;
        }

        _clipboard = node.Clone();
        Notify("Copied the selected block.", "info");
    }

    public void CutNode()
    {
        var page = CurrentPage;
        var node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        _clipboard = node.Clone();
        var parent = page.ParentOf(node.Id);
        var siblings = parent?.Children ?? page.Nodes;

        Capture(page, "Cut node");
        siblings.RemoveAll(candidate => candidate.Id == node.Id);
        SelectedNodeId = parent?.Id;
        SaveCurrentPage(page, "Cut node");
        Notify("Cut the selected block.", "info");
    }

    public void PasteNode()
    {
        var page = CurrentPage;

        if (page is null || _clipboard is null)
        {
            Notify("Copy a block first, then paste it here.", "info");
            return;
        }

        var copy = _clipboard.Clone();

        if (IsEditingBlock && NodeContains(copy, EditingBlockId!))
        {
            Notify("That would make this block contain itself.", "warning");
            return;
        }

        var node = CurrentNode;
        Capture(page, "Paste node");

        if (node is not null)
        {
            var parent = page.ParentOf(node.Id);
            var siblings = parent?.Children ?? page.Nodes;

            if (node.IsContainer)
            {
                node.Children ??= new List<PageNode>();
                node.Children.Add(copy);
                CollapsedNodes.Remove(node.Id);
            }
            else
            {
                var index = siblings.FindIndex(candidate => candidate.Id == node.Id);

                if (index < 0)
                {
                    siblings.Add(copy);
                }
                else
                {
                    siblings.Insert(index + 1, copy);
                }
            }
        }
        else
        {
            page.Nodes.Insert(Math.Clamp(EndOfPageIndex(page), 0, page.Nodes.Count), copy);
        }

        SelectedNodeId = copy.Id;
        SaveCurrentPage(page, "Pasted node");
        Notify("Pasted the block.", "success");
    }

    public void DeleteNode()
    {
        var page = CurrentPage;
        var node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        var parent = page.ParentOf(node.Id);
        var siblings = parent?.Children ?? page.Nodes;

        Capture(page, "Delete node");
        siblings.RemoveAll(candidate => candidate.Id == node.Id);
        SelectedNodeId = parent?.Id;
        SaveCurrentPage(page, "Deleted node");
    }

    public void EditNode(Action<PageNode> edit, string label)
    {
        var page = CurrentPage;
        var node = CurrentNode;

        if (page is null || node is null)
        {
            return;
        }

        Capture(page, label);
        edit(node);
        SaveCurrentPage(page, label);
    }

    public void SetNodeText(string text) =>
        EditNode(node => node.Text = text, "Edit text");

    public void SetNodeClassName(string className) =>
        EditNode(node => node.ClassName = string.IsNullOrWhiteSpace(className) ? null : className.Trim(), "Edit class");

    public void SetNodeTag(string tag)
    {
        var cleaned = tag.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return;
        }

        EditNode(node => node.Tag = cleaned, "Change element");
    }

    /// <summary>
    /// Replaces a node's inline style from a small CSS declaration block. This
    /// backs the inspector's manual stylesheet editor; malformed declarations
    /// are skipped rather than clearing the whole style.
    /// </summary>
    public void SetNodeStyleSheet(string text)
    {
        Dictionary<string, string> parsed = [];

        foreach (string part in (text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = part.IndexOf(':');

            if (colon <= 0)
            {
                continue;
            }

            string property = part[..colon].Trim();
            string value = part[(colon + 1)..].Trim();

            if (property.Length > 0 && value.Length > 0)
            {
                parsed[property] = value;
            }
        }

        EditNode(
            node => node.Style = parsed.Count == 0 ? null : parsed,
            "Edit stylesheet");
    }

    public void SetNodeStyle(string property, string? value)
    {
        EditNode(
            node =>
            {
                node.Style ??= new Dictionary<string, string>();

                if (string.IsNullOrWhiteSpace(value))
                {
                    node.Style.Remove(property);
                }
                else
                {
                    node.Style[property] = value.Trim();
                }

                if (node.Style.Count == 0)
                {
                    node.Style = null;
                }
            },
            "Edit " + property);
    }

    public void SetNodeAttr(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        EditNode(
            node =>
            {
                node.Attrs ??= new Dictionary<string, string>();

                if (string.IsNullOrWhiteSpace(value))
                {
                    node.Attrs.Remove(key);
                }
                else
                {
                    node.Attrs[key] = value.Trim();
                }

                if (node.Attrs.Count == 0)
                {
                    node.Attrs = null;
                }
            },
            "Edit attribute");
    }

    public void SetCollectionTarget(string type)
    {
        EditNode(
            node =>
            {
                node.Attrs ??= new Dictionary<string, string>();
                node.Attrs["type"] = type;
            },
            "Collection data type");
    }

    /// <summary>
    /// Expands the top level of a page and collapses deeper containers so the
    /// tree opens at a readable size.
    /// </summary>
    public void EnsureTreeState(PageDoc page)
    {
        if (page.Nodes.Any(node => CollapsedNodes.Contains(node.Id)))
        {
            return;
        }

        foreach (PageNode top in page.Nodes)
        {
            if (top.Children is not { Count: > 0 })
            {
                continue;
            }

            foreach (PageNode child in top.Children)
            {
                CollapseSubtree(child);
            }
        }
    }

    private void CollapseSubtree(PageNode node)
    {
        if (node.Children is not { Count: > 0 })
        {
            return;
        }

        CollapsedNodes.Add(node.Id);

        foreach (PageNode child in node.Children)
        {
            CollapseSubtree(child);
        }
    }

    public void ToggleNode(string id)
    {
        if (!CollapsedNodes.Add(id))
        {
            CollapsedNodes.Remove(id);
        }

        MarkChanged();
    }

    public void ToggleSection(string key)
    {
        if (!CollapsedSections.Add(key))
        {
            CollapsedSections.Remove(key);
        }

        MarkChanged();
    }

    public bool IsCollapsed(string key) => CollapsedSections.Contains(key);

    public void SelectParentNode()
    {
        var page = CurrentPage;

        if (page is null || SelectedNodeId is null)
        {
            return;
        }

        SelectedNodeId = page.ParentOf(SelectedNodeId)?.Id;
        MarkChanged();
    }

    private static bool IsContainer(PageNode node) =>
        node.Tag.ToLowerInvariant() is
            "section" or "div" or "main" or "header" or "footer" or "article" or "aside" or "form"
            or "ul" or "ol" or "li" or "blockquote" or "figure" or "figurecaption" or "figcaption";

    private void SaveCurrentPage(PageDoc page, string label)
    {
        try
        {
            if (page.Id.StartsWith("block:", StringComparison.Ordinal))
            {
                if (Project?.Block(page.Id["block:".Length..]) is not { } block)
                {
                    return;
                }

                block.Nodes = page.Nodes;
                Project.SaveBlock(block);
            }
            else
            {
                Project!.SavePage(page);
            }

            SaveState = label;
            Refresh?.Invoke();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    private PageDoc? DocumentForSnapshot(string id)
    {
        PageDoc? page = Project?.Page(id);

        if (page is not null)
        {
            return page;
        }

        if (id.StartsWith("block:", StringComparison.Ordinal)
            && Project?.Block(id["block:".Length..]) is { } block)
        {
            EditingBlockId = block.Id;
            SelectedPageId = null;
            _blockDocument = null;
            return BlockDocument();
        }

        return null;
    }

    // ------------------------------------------------------------ undo/redo

    private void Capture(PageDoc page, string label)
    {
        var json = JsonSerializer.Serialize(page, WcJson.Options);

        if (_undo.Count > 0 && _undo[^1].PageId == page.Id && _undo[^1].Json == json)
        {
            return;
        }

        // Live property editing produces many changes per second. Coalesce them
        // into one undo step while the same field on the same node is active.
        string key = page.Id + "|" + label + "|" + (SelectedNodeId ?? "");
        long now = Environment.TickCount64;

        if (_undo.Count > 0
            && _lastCaptureKey == key
            && now - _lastCaptureTick < 900
            && _undo[^1].PageId == page.Id)
        {
            _lastCaptureTick = now;
            return;
        }

        _lastCaptureKey = key;
        _lastCaptureTick = now;

        _undo.Add(new PageSnapshot
        {
            PageId = page.Id,
            Json = json,
            Label = label,
            SelectedNodeId = SelectedNodeId,
        });

        while (_undo.Count > 60)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Undo()
    {
        if (_undo.Count == 0 || Project is null)
        {
            return;
        }

        var snapshot = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _lastCaptureKey = "";
        var page = DocumentForSnapshot(snapshot.PageId);

        if (page is null)
        {
            return;
        }

        _redo.Add(new PageSnapshot
        {
            PageId = page.Id,
            Json = JsonSerializer.Serialize(page, WcJson.Options),
            Label = snapshot.Label,
            SelectedNodeId = SelectedNodeId,
        });

        ApplySnapshot(snapshot);
        Notify("Undid " + snapshot.Label.ToLowerInvariant() + ".", "info");
    }

    public void Redo()
    {
        if (_redo.Count == 0 || Project is null)
        {
            return;
        }

        var snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _lastCaptureKey = "";
        var page = DocumentForSnapshot(snapshot.PageId);

        if (page is null)
        {
            return;
        }

        _undo.Add(new PageSnapshot
        {
            PageId = page.Id,
            Json = JsonSerializer.Serialize(page, WcJson.Options),
            Label = snapshot.Label,
            SelectedNodeId = SelectedNodeId,
        });

        ApplySnapshot(snapshot);
        Notify("Redid " + snapshot.Label.ToLowerInvariant() + ".", "info");
    }

    private void ApplySnapshot(PageSnapshot snapshot)
    {
        if (Project is null)
        {
            return;
        }

        var page = JsonSerializer.Deserialize<PageDoc>(snapshot.Json, WcJson.Options);

        if (page is null)
        {
            return;
        }

        try
        {
            if (page.Id.StartsWith("block:", StringComparison.Ordinal)
                && Project.Block(page.Id["block:".Length..]) is { } block)
            {
                block.Nodes = page.Nodes;
                Project.SaveBlock(block);
                EditingBlockId = block.Id;
                SelectedPageId = null;
                _blockDocument = page;
                OpenTab(StudioMode.Blocks, block.Id, block.Name, "fa-cube");
            }
            else
            {
                int index = Project.Pages.FindIndex(candidate => candidate.Id == page.Id);

                if (index >= 0)
                {
                    Project.Pages[index] = page;
                }

                Project.SavePage(page);
                SelectedPageId = page.Id;
                OpenTab(StudioMode.Design, page.Id, page.Title, "fa-shapes");
            }
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }

        SelectedNodeId = snapshot.SelectedNodeId;
        MarkChanged();
    }

    // ----------------------------------------------------------- block presets

    private static PageNode CreatePreset(string id) => id switch
    {
        "section" => PageNode.Create("section", "site-section site-shell", null, null,
            PageNode.Create("div", "section-head", null, null,
                PageNode.Create("div", null, null, null,
                    PageNode.Create("span", "section-kicker", "Section"),
                    PageNode.Create("h2", "section-title", "A new section.")),
                PageNode.Create("p", "section-lead", "Explain what this part of the page is for.")),
            PageNode.Create("div", "feature-grid", null, null,
                FeatureBlock("01", "First point", "A sentence or two of supporting copy."))),

        "full-width" => PageNode.Create("div", "full-width full-width-row", null, null,
            PageNode.Create("div", "full-width-copy", null, null,
                PageNode.Create("span", "eyebrow", "Full width"),
                PageNode.Create("h2", "section-title", "A full-width row."),
                PageNode.Create("p", "section-lead", "Drop this into a grid or flex row to span every column, then edit its inner layout.")),
            PageNode.CreateWithAttrs("a", "site-btn", "Action", new Dictionary<string, string> { ["href"] = "#" })),

        "grid-2" => GridPreset(2, "Two clear paths.", "Give each card a short title and one useful line of copy."),
        "grid-3" => GridPreset(3, "Three reasons to choose you.", "Short, specific cards work better than long paragraphs."),
        "grid-4" => GridPreset(4, "Four small proofs.", "Use compact cards for services, features or steps."),

        "columns" => PageNode.Create("div", "feature-grid", null, null,
            FeatureBlock("01", "One", "A clear, specific point about the work."),
            FeatureBlock("02", "Two", "Another point, with room to breathe."),
            FeatureBlock("03", "Three", "The last of three short ideas.")),

        "band" => PageNode.Create("section", "site-band", null, null,
            PageNode.Create("div", "site-shell site-band-inner", null, null,
                PageNode.Create("div", "site-band-copy", null, null,
                    PageNode.Create("h2", "section-title", "A clear next step."),
                    PageNode.Create("p", "section-lead", "Give the reader one reason to keep going.")),
                PageNode.CreateWithAttrs("a", "site-btn", "Continue", new Dictionary<string, string> { ["href"] = "#" }))),

        "spacer" => PageNode.Create("div", "spacer"),

        "divider" => PageNode.Create("div", "divider"),

        "heading" => PageNode.Create("h2", "section-title", "A new heading."),

        "subtitle" => PageNode.Create("h3", "subtitle", "A smaller heading."),

        "text" => PageNode.Create("p", "prose", "Write the first sentence here. Keep it specific, and let the next sentence do some work."),

        "eyebrow" => PageNode.Create("span", "eyebrow", "Eyebrow"),

        "link" => PageNode.CreateWithAttrs("a", "text-link", "Read more", new Dictionary<string, string> { ["href"] = "#" }),

        "list" => PageNode.Create("ul", "bullet-list", null, null,
            PageNode.Create("li", null, "First point worth making."),
            PageNode.Create("li", null, "Second point, short and concrete."),
            PageNode.Create("li", null, "Third point to close the list.")),

        "quote" => PageNode.Create("blockquote", "quote", null, null,
            PageNode.Create("span", null, "“A sentence worth setting apart from everything around it.”"),
            PageNode.Create("cite", null, "Name, role")),

        "code" => PageNode.Create("pre", "code-block",
            "// A short code sample\nconst site = build({\n  pages: \"json\",\n  output: \"html\"\n});"),

        "youtube" => PageNode.CreateWithAttrs("iframe", "embed-video", null, new Dictionary<string, string>
        {
            ["src"] = "https://www.youtube.com/embed/ysz5S6PUM-U",
            ["title"] = "YouTube video",
            ["loading"] = "lazy",
            ["allow"] = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share",
            ["referrerpolicy"] = "strict-origin-when-cross-origin",
            ["allowfullscreen"] = "true",
        }),

        "embed" => PageNode.CreateWithAttrs("iframe", "embed-frame", null, new Dictionary<string, string>
        {
            ["src"] = "https://example.com",
            ["title"] = "Embedded page",
            ["loading"] = "lazy",
            ["allow"] = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share",
            ["referrerpolicy"] = "strict-origin-when-cross-origin",
        }),

        "hero" => PageNode.Create("section", "site-hero", null, null,
            PageNode.Create("div", "site-shell", null, null,
                PageNode.Create("div", "hero-grid", null, null,
                    PageNode.Create("div", "hero-main", null, null,
                        PageNode.Create("span", "eyebrow", "New"),
                        PageNode.Create("h1", "hero-title", "A clear, confident headline."),
                        PageNode.Create("p", "hero-copy", "Say what you do, who it is for, and why it matters. Keep the first sentence direct."),
                        PageNode.Create("div", "hero-actions", null, null,
                            PageNode.CreateWithAttrs("a", "site-btn", "Primary action", new Dictionary<string, string> { ["href"] = "#" }),
                            PageNode.CreateWithAttrs("a", "site-btn ghost", "Secondary action", new Dictionary<string, string> { ["href"] = "#" })))))),

        "media-left" => MediaPreset(false),
        "media-right" => MediaPreset(true),

        "image-title" => PageNode.Create("div", "media-card", null, null,
            PageNode.Create("img", "wc-image", null, null),
            PageNode.Create("div", "media-card-body", null, null,
                PageNode.Create("h3", "feature-title", "Image with title"),
                PageNode.Create("p", "feature-copy", "A short caption or supporting line beneath the image."))),

        "buttons" => PageNode.Create("div", "button-row", null, null,
            PageNode.CreateWithAttrs("a", "site-btn", "Primary action", new Dictionary<string, string> { ["href"] = "#" }),
            PageNode.CreateWithAttrs("a", "site-btn ghost", "Secondary action", new Dictionary<string, string> { ["href"] = "#" })),

        "button" => PageNode.CreateWithAttrs("a", "site-btn", "Button label", new Dictionary<string, string> { ["href"] = "#" }),

        "image" => PageNode.Create("img", "wc-image", null, null),

        "cards" => PageNode.Create("div", "collection-grid", null, null,
            CardPlaceholder("Card one", "A short description of this item."),
            CardPlaceholder("Card two", "Another short description, similar in length.")),

        "stats" => PageNode.Create("div", "stat-row", null, null,
            StatBlock("12", "Projects shipped"),
            StatBlock("98%", "Client satisfaction"),
            StatBlock("9 yrs", "Making the work")),

        "logos" => PageNode.Create("div", "logo-row", null, null,
            PageNode.Create("span", "logo-word", "Northwind"),
            PageNode.Create("span", "logo-word", "Aster & Oak"),
            PageNode.Create("span", "logo-word", "Fieldwork"),
            PageNode.Create("span", "logo-word", "Meridian")),

        "faq" => PageNode.Create("div", "faq-list", null, null,
            FaqItem("How long does a project take?", "Most projects run in focused phases, with a clear review at the end of each one."),
            FaqItem("What do you need from me?", "A short brief, a sense of the audience, and quick decisions when they matter."),
            FaqItem("Can we work together remotely?", "Yes. Everything runs through clear documents, calls and shared previews.")),

        "collection" => PageNode.CreateWithAttrs("collection", "collection-grid", null, new Dictionary<string, string>
        {
            ["type"] = "Post",
            ["limit"] = "3",
        }),

        _ => PageNode.Create("p", "prose", "New block."),
    };

    private static PageNode GridPreset(int columns, string title, string copy)
    {
        List<PageNode> cards = [];

        for (int index = 1; index <= columns; index++)
        {
            cards.Add(PageNode.Create("div", "grid-card", null, null,
                PageNode.Create("h3", "feature-title", "Card " + index),
                PageNode.Create("p", "feature-copy", copy)));
        }

        return PageNode.Create("div", "grid grid-" + columns, null, null,
            cards.ToArray());
    }

    private static PageNode MediaPreset(bool reverse)
    {
        PageNode frame = PageNode.Create("figure", "media-frame", null, null,
            PageNode.Create("img", "wc-image", null, null));

        PageNode copy = PageNode.Create("div", "media-copy", null, null,
            PageNode.Create("span", "eyebrow", "About"),
            PageNode.Create("h2", "section-title", "Image with text."),
            PageNode.Create("p", "section-lead", "Pair a strong image with a short explanation. Keep the copy focused on one idea."),
            PageNode.CreateWithAttrs("a", "site-btn ghost", "Read more", new Dictionary<string, string> { ["href"] = "#" }));

        return PageNode.Create("div", reverse ? "media-split is-reverse" : "media-split", null, null,
            reverse ? copy : frame,
            reverse ? frame : copy);
    }

    private static PageNode StatBlock(string value, string label) =>
        PageNode.Create("div", "stat", null, null,
            PageNode.Create("strong", null, value),
            PageNode.Create("span", null, label));

    private static PageNode FaqItem(string question, string answer) =>
        PageNode.Create("div", "faq-item", null, null,
            PageNode.Create("h3", "feature-title", question),
            PageNode.Create("p", "feature-copy", answer));

    private static PageNode FeatureBlock(string icon, string title, string copy) =>
        PageNode.Create("div", "feature", null, null,
            PageNode.Create("div", "feature-icon", icon),
            PageNode.Create("h3", "feature-title", title),
            PageNode.Create("p", "feature-copy", copy));

    private static PageNode CardPlaceholder(string title, string copy) =>
        PageNode.Create("div", "post-card", null, null,
            PageNode.Create("div", "post-thumb"),
            PageNode.Create("div", "post-body", null, null,
                PageNode.Create("div", "post-meta", "Category"),
                PageNode.Create("h3", "post-title", title),
                PageNode.Create("p", "post-excerpt", copy)));
}