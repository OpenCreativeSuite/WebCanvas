using System.Collections.Generic;
using Donkeh.LayoutLab;
using Donkeh.UILab;
using Donkeh.Windowing;

namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    /// <summary>The widget payload currently being dragged from a palette.</summary>
    public string? DraggedWidget { get; private set; }

    /// <summary>A human label for the widget being dragged, shown on the canvas.</summary>
    public string WidgetDragLabel { get; private set; } = "";

    /// <summary>The insertion preview shown under the pointer.</summary>
    public DropHint? DropHint { get; private set; }

    /// <summary>Page boxes frozen at drag start for the wireframe drop map.</summary>
    public List<DropBound> DropBounds { get; private set; } = [];

    public bool IsWidgetDragging => DraggedWidget is not null;

    public bool IsDropWireframe => IsWidgetDragging && DropBounds.Count > 0;

    public string WidgetSearch { get; private set; } = "";

    private long _widgetClickSuppressedUntil;

    /// <summary>
    /// True just after a drag finishes. Palette buttons use it so a cancelled
    /// drag can never fall through as a click and insert a second widget.
    /// </summary>
    public bool ShouldIgnoreWidgetClick() =>
        Environment.TickCount64 < _widgetClickSuppressedUntil;

    public void SetWidgetSearch(string value)
    {
        if (WidgetSearch == value)
        {
            return;
        }

        WidgetSearch = value;
        Refresh?.Invoke();
    }

    public void BeginWidgetDrag(string payload)
    {
        DraggedWidget = payload;
        WidgetDragLabel = PayloadLabel(payload);
        DropHint = null;
        DropBounds = CaptureDropBounds();
        Refresh?.Invoke();
    }

    public void EndWidgetDrag()
    {
        _widgetClickSuppressedUntil = Environment.TickCount64 + 350;

        if (DraggedWidget is null && DropHint is null && DropBounds.Count == 0)
        {
            return;
        }

        DraggedWidget = null;
        WidgetDragLabel = "";
        DropHint = null;
        DropBounds = [];
        Refresh?.Invoke();
    }

    /// <summary>
    /// Adds a widget inside the selected container, after the selected node, or
    /// at the end of the page when nothing is selected. This is the click path;
    /// dragging uses <see cref="DropWidgetAt"/> instead.
    /// </summary>
    public void AddWidget(string payload, string? targetId = null)
    {
        PageDoc? page = CurrentPage;

        if (page is null || Project is null)
        {
            return;
        }

        string? target = targetId ?? SelectedNodeId;

        if (target is null)
        {
            InsertWidget(payload, null, DropPosition.After, -1);
            return;
        }

        PageNode? node = page.Find(target);

        if (node is null)
        {
            InsertWidget(payload, null, DropPosition.After, -1);
            return;
        }

        InsertWidget(payload, node.Id,
            node.IsContainer ? DropPosition.Inside : DropPosition.After, -1);
    }

    /// <summary>
    /// Updates the canvas drop preview. Called repeatedly by node level
    /// <c>dragover</c> handlers, so it only re-renders when the target or
    /// position actually changes.
    /// </summary>
    public void HoverDrop(
        string targetId,
        DropPosition position,
        int index,
        float x,
        float y,
        float width,
        float height,
        float pointerX,
        float pointerY)
    {
        if (DraggedWidget is null)
        {
            return;
        }

        if (DropHint is { } current
            && current.TargetId == targetId
            && current.Position == position
            && current.Index == index
            && Math.Abs(current.PointerY - pointerY) < 0.5f)
        {
            return;
        }

        DropHint = new DropHint
        {
            TargetId = targetId,
            Position = position,
            Index = index,
            Label = WidgetDragLabel,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            PointerX = pointerX,
            PointerY = pointerY,
        };

        Refresh?.Invoke();
    }

    public void ClearDropHint()
    {
        if (DropHint is null)
        {
            return;
        }

        DropHint = null;
        Refresh?.Invoke();
    }

    /// <summary>Completes a drag onto a specific node and insertion position.</summary>
    public void DropWidgetAt(string? targetId, DropPosition position, int index)
    {
        string? payload = DraggedWidget;

        if (payload is null)
        {
            return;
        }

        _widgetClickSuppressedUntil = Environment.TickCount64 + 350;
        DraggedWidget = null;
        WidgetDragLabel = "";
        DropHint = null;
        DropBounds = [];
        InsertWidget(payload, targetId, position, index);
    }

    /// <summary>
    /// Used by the canvas background when a drag is released outside a node.
    /// </summary>
    public void DropWidgetAtEnd() => DropWidgetAt(null, DropPosition.After, -1);

    private void InsertWidget(string payload, string? targetId, DropPosition position, int index)
    {
        PageDoc? page = CurrentPage;

        if (page is null || Project is null)
        {
            return;
        }

        if (CreateWidgetNode(payload) is not { } widget)
        {
            return;
        }

        PageNode node = widget.Node;
        string label = widget.Label;
        PageNode? target = page.Find(targetId);
        Capture(page, "Add " + label);

        List<PageNode> list;
        int insertAt;
        PageNode? expand = null;

        if (target is not null && position == DropPosition.Inside && target.IsContainer)
        {
            target.Children ??= new List<PageNode>();
            list = target.Children;
            insertAt = index >= 0 ? Math.Clamp(index, 0, list.Count) : list.Count;
            expand = target;
        }
        else if (target is not null)
        {
            PageNode? parent = page.ParentOf(target.Id);
            list = parent?.Children ?? page.Nodes;
            int foundIndex = list.FindIndex(child => child.Id == target.Id);

            if (foundIndex < 0)
            {
                list = page.Nodes;
                insertAt = EndOfPageIndex(page);
            }
            else
            {
                insertAt = foundIndex + (position == DropPosition.After ? 1 : 0);
            }

            expand = parent;
        }
        else
        {
            list = page.Nodes;
            insertAt = EndOfPageIndex(page);
        }

        // Footers stay last. Never insert a new block beneath the page footer.
        if (ReferenceEquals(list, page.Nodes))
        {
            int footer = list.FindIndex(child => IsFooterNode(child, Project));

            if (footer >= 0 && insertAt > footer)
            {
                insertAt = footer;
            }
        }

        list.Insert(Math.Clamp(insertAt, 0, list.Count), node);

        if (expand is not null)
        {
            CollapsedNodes.Remove(expand.Id);
        }

        SelectedNodeId = node.Id;
        DropHint = null;
        SaveCurrentPage(page, "Added " + label);
    }

    private int EndOfPageIndex(PageDoc page)
    {
        int footer = Project is null
            ? -1
            : page.Nodes.FindIndex(child => IsFooterNode(child, Project));

        return footer >= 0 ? footer : page.Nodes.Count;
    }

    private (PageNode Node, string Label)? CreateWidgetNode(string payload)
    {
        if (payload.StartsWith("preset:", StringComparison.Ordinal))
        {
            string presetId = payload["preset:".Length..];
            BlockPreset? preset = Presets.FirstOrDefault(item => item.Id == presetId);

            if (preset is null)
            {
                return null;
            }

            return (CreatePreset(presetId), preset.Label);
        }

        if (payload.StartsWith("block:", StringComparison.Ordinal))
        {
            string blockId = payload["block:".Length..];

            if (Project?.Block(blockId) is not { } block)
            {
                return null;
            }

            if (EditingBlockId == block.Id)
            {
                Notify("A block can't contain itself.", "warning");
                return null;
            }

            if (IsEditingBlock && BlockContains(block, EditingBlockId!))
            {
                Notify("That block already uses this one, so it would create a loop.", "warning");
                return null;
            }

            return (BlockRefNode(block), block.Name);
        }

        return null;
    }

    public bool CanInsertBlock(BlockDoc block)
    {
        if (EditingBlockId == block.Id)
        {
            return false;
        }

        return !IsEditingBlock || !BlockContains(block, EditingBlockId!);
    }

    private static bool BlockContains(BlockDoc block, string blockId)
    {
        foreach (PageNode node in block.Nodes)
        {
            if (NodeContains(node, blockId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NodeContains(PageNode node, string blockId)
    {
        if (node.Tag == "block"
            && node.Attrs is not null
            && node.Attrs.TryGetValue("block", out string? id)
            && id == blockId)
        {
            return true;
        }

        if (node.Children is null)
        {
            return false;
        }

        return node.Children.Any(child => NodeContains(child, blockId));
    }

    private static PageNode BlockRefNode(BlockDoc block)
    {
        var node = PageNode.Create("block");
        node.Attrs = new Dictionary<string, string> { ["block"] = block.Id };
        return node;
    }

    private string PayloadLabel(string payload)
    {
        if (payload.StartsWith("preset:", StringComparison.Ordinal))
        {
            string id = payload["preset:".Length..];
            return Presets.FirstOrDefault(item => item.Id == id)?.Label ?? "Widget";
        }

        if (payload.StartsWith("block:", StringComparison.Ordinal))
        {
            string id = payload["block:".Length..];
            return Project?.Block(id)?.Name ?? "Block";
        }

        return "Widget";
    }

    // ------------------------------------------------------------ drop wireframe

    private List<DropBound> CaptureDropBounds()
    {
        PageDoc? page = CurrentPage;

        if (page is null || AppRootHost.Current?.FrameResult is not { Root: { } root })
        {
            return [];
        }

        LayoutBox? canvas = null;
        var boxes = new Dictionary<string, LayoutBox>(StringComparer.Ordinal);

        WalkBoxes(root);

        if (canvas is null)
        {
            return [];
        }

        var origin = canvas.BorderBox;
        var bounds = new List<DropBound>
        {
            new()
            {
                NodeId = "",
                Label = "Page",
                Tag = "page",
                X = 0,
                Y = 0,
                Width = canvas.BorderBox.Width,
                Height = canvas.BorderBox.Height,
                Depth = -1,
                IsContainer = true,
                IsPage = true,
            },
        };

        WalkPage(page.Nodes, 0);
        return bounds;

        void WalkBoxes(LayoutBox box)
        {
            if (box.Node is ElementNode element)
            {
                string classes = ClassOf(element);

                if (canvas is null
                    && classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("wc-canvas"))
                {
                    canvas = box;
                }

                if (element.TryGetAttribute("data-node-id", out var id) && !string.IsNullOrEmpty(id))
                {
                    boxes[id] = box;
                }
            }

            foreach (LayoutBox child in box.Children)
            {
                WalkBoxes(child);
            }
        }

        void WalkPage(List<PageNode> nodes, int depth)
        {
            foreach (PageNode node in nodes)
            {
                if (boxes.TryGetValue(node.Id, out var box))
                {
                    var rect = box.BorderBox;

                    if (rect.Width > 1 && rect.Height > 1)
                    {
                        bounds.Add(new DropBound
                        {
                            NodeId = node.Id,
                            Label = BoundLabel(node),
                            Tag = node.Tag,
                            X = rect.X - origin.X,
                            Y = rect.Y - origin.Y,
                            Width = rect.Width,
                            Height = rect.Height,
                            Depth = depth,
                            IsContainer = node.IsContainer,
                            IsBlock = node.Tag == "block",
                        });
                    }
                }

                if (node.Children is not null)
                {
                    WalkPage(node.Children, depth + 1);
                }
            }
        }
    }

    private static string ClassOf(ElementNode element)
    {
        foreach (var pair in element.EnumerateAttributes())
        {
            if (pair.Key == "class")
            {
                return pair.Value;
            }
        }

        return "";
    }

    private string BoundLabel(PageNode node)
    {
        if (node.Tag == "block"
            && node.Attrs is not null
            && node.Attrs.TryGetValue("block", out var id)
            && Project?.Block(id) is { } block)
        {
            return block.Name;
        }

        if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            foreach (string token in node.ClassName!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (token == "wc-node" || token.StartsWith("is-", StringComparison.Ordinal))
                {
                    continue;
                }

                return HumanizeToken(token);
            }
        }

        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            return node.DisplayName();
        }

        return node.Tag;
    }

    private static string HumanizeToken(string token)
    {
        string[] parts = token.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();

        foreach (string part in parts.Take(3))
        {
            words.Add(char.ToUpperInvariant(part[0]) + part[1..]);
        }

        return words.Count == 0 ? token : string.Join(' ', words);
    }
}
