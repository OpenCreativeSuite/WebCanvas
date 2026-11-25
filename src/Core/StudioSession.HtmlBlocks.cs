using System.Globalization;

namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public int HtmlPreviewRevision { get; private set; }

    public PageDoc? HtmlEditorPage
    {
        get
        {
            var (page, _) = HtmlTarget();
            return page;
        }
    }

    public PageNode? CurrentHtmlNode
    {
        get
        {
            var (_, node) = HtmlTarget();
            return node;
        }
    }

    public static string HtmlBlockCss(PageNode node) =>
        node.Attrs is not null && node.Attrs.TryGetValue("css", out string? css) ? css : "";

    public static int HtmlBlockWidth(PageNode node) => AttributeNumber(node, "width");

    public static int HtmlBlockHeight(PageNode node) => AttributeNumber(node, "height");

    public void OpenHtmlEditor(PageNode node)
    {
        PageDoc? page = CurrentPage;

        if (page is null || page.Find(node.Id) is null)
        {
            return;
        }

        if (page.Id.StartsWith("block:", StringComparison.Ordinal))
        {
            EditingBlockId = page.Id["block:".Length..];
            SelectedPageId = null;
            _blockDocument = page;
        }
        else
        {
            EditingBlockId = null;
            _blockDocument = null;
            SelectedPageId = page.Id;
        }

        SelectedNodeId = node.Id;
        OpenTab(StudioMode.Html, page.Id + "|" + node.Id, "HTML · " + page.Title, ModeInfo.Icon(StudioMode.Html));
    }

    /// <summary>Returns from the HTML editor to the page or block being edited.</summary>
    public void CloseHtmlEditor()
    {
        var (page, _) = HtmlTarget();

        if (page is null)
        {
            return;
        }

        if (page.Id.StartsWith("block:", StringComparison.Ordinal)
            && Project?.Block(page.Id["block:".Length..]) is { } block)
        {
            EditingBlockId = block.Id;
            SelectedPageId = null;
            _blockDocument = null;
            OpenTab(StudioMode.Blocks, block.Id, block.Name, "fa-cube");
            return;
        }

        SelectPage(page.Id);
    }

    /// <summary>Writes the HTML block document to its page or reusable block.</summary>
    public void SaveHtmlBlock()
    {
        var (page, _) = HtmlTarget();

        if (page is null)
        {
            return;
        }

        SaveDocument(page);
        Notify("HTML block saved.", "success");
    }

    public void SetHtmlBlockHtml(string html) =>
        EditNode(node => node.Text = html, "Edit HTML");

    public void SetHtmlBlockCss(string css)
    {
        EditNode(
            node =>
            {
                node.Attrs ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(css))
                {
                    node.Attrs.Remove("css");
                }
                else
                {
                    node.Attrs["css"] = css;
                }
            },
            "Edit HTML styles");
    }

    public void SetHtmlBlockSize(string nodeId, int width, int height)
    {
        var (page, _) = HtmlTarget();
        PageNode? node = page?.Find(nodeId);

        if (page is null || node is null)
        {
            return;
        }

        node.Attrs ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        node.Attrs["width"] = Math.Max(1, width).ToString(CultureInfo.InvariantCulture);
        node.Attrs["height"] = Math.Max(1, height).ToString(CultureInfo.InvariantCulture);

        try
        {
            SaveDocument(page);
        }
        catch
        {
            return;
        }

        MarkChanged();
    }

    public void RefreshHtmlPreview()
    {
        HtmlPreviewRevision++;
        MarkChanged();
    }

    private (PageDoc? Page, PageNode? Node) HtmlTarget()
    {
        string itemId = ActiveTab?.Mode == StudioMode.Html ? ActiveTab.ItemId : "";

        if (itemId.Length == 0)
        {
            return (null, null);
        }

        int separator = itemId.IndexOf('|');

        if (separator <= 0)
        {
            return (null, null);
        }

        string documentId = itemId[..separator];
        string nodeId = itemId[(separator + 1)..];

        if (documentId.StartsWith("block:", StringComparison.Ordinal))
        {
            string blockId = documentId["block:".Length..];

            if (Project?.Block(blockId) is null)
            {
                return (null, null);
            }

            if (!string.Equals(EditingBlockId, blockId, StringComparison.Ordinal))
            {
                EditingBlockId = blockId;
                SelectedPageId = null;
                _blockDocument = null;
            }

            PageDoc? blockPage = BlockDocument();
            return (blockPage, blockPage?.Find(nodeId));
        }

        PageDoc? page = Project?.Page(documentId);
        return (page, page?.Find(nodeId));
    }

    private void SaveDocument(PageDoc page)
    {
        if (page.Id.StartsWith("block:", StringComparison.Ordinal)
            && Project is not null
            && Project.Block(page.Id["block:".Length..]) is { } block)
        {
            block.Nodes = page.Nodes;
            Project.SaveBlock(block);
            return;
        }

        Project?.SavePage(page);
    }

    private static int AttributeNumber(PageNode node, string key) =>
        node.Attrs is not null
        && node.Attrs.TryGetValue(key, out string? value)
        && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 0;
}
