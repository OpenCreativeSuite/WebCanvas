using System.Collections.Generic;

namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public string? EditingBlockId { get; private set; }

    private PageDoc? _blockDocument;

    public BlockDoc? CurrentBlock => Project?.Block(EditingBlockId);

    public bool IsEditingBlock => EditingBlockId is not null && CurrentBlock is not null;

    public PageDoc? BlockDocument()
    {
        if (Project?.Block(EditingBlockId) is not { } block)
        {
            return null;
        }

        if (_blockDocument is null || _blockDocument.Id != "block:" + block.Id)
        {
            _blockDocument = new PageDoc
            {
                Id = "block:" + block.Id,
                Title = block.Name,
                Slug = "",
                Nodes = block.Nodes,
            };
        }

        _blockDocument.Title = block.Name;
        _blockDocument.Nodes = block.Nodes;
        return _blockDocument;
    }

    public void OpenBlock(BlockDoc block)
    {
        EditingBlockId = block.Id;
        SelectedPageId = null;
        SelectedNodeId = null;
        _blockDocument = null;
        CollapsedNodes.Clear();

        if (BlockDocument() is { } document)
        {
            EnsureTreeState(document);
        }

        ActiveMode = StudioMode.Blocks;
        OpenTab(StudioMode.Blocks, block.Id, block.Name, "fa-cube");
    }

    public void OpenBlocksLibrary()
    {
        EditingBlockId = null;
        _blockDocument = null;
        SelectedNodeId = null;
        ActiveMode = StudioMode.Blocks;
        OpenTab(StudioMode.Blocks, "library", "Blocks", "fa-cube");
    }

    public void CloseBlock()
    {
        OpenBlocksLibrary();
    }

    public void NewBlock() => CreateBlock("content", "Section");

    public void NewThemeBlock() => CreateBlock("theme", "Navigation");

    private void CreateBlock(string kind, string category)
    {
        OpenPrompt(new PromptRequest
        {
            Title = kind == "theme" ? "New theme block" : "New block",
            Label = "Block name",
            Value = kind == "theme" ? "New theme block" : "New block",
            Confirm = "Create block",
            OnConfirm = value =>
            {
                if (Project is null)
                {
                    return;
                }

                var name = string.IsNullOrWhiteSpace(value) ? "New block" : value.Trim();
                var block = new BlockDoc
                {
                    Name = name,
                    Kind = kind,
                    Category = category,
                    Nodes =
                    {
                        PageNode.Create("section", "site-section site-shell", null, null,
                            PageNode.Create("div", "section-head", null, null,
                                PageNode.Create("div", null, null, null,
                                    PageNode.Create("span", "section-kicker", "Block"),
                                    PageNode.Create("h2", "section-title", name))),
                            PageNode.Create("p", "prose", "Edit this block, then reuse it on any page.")),
                    },
                };

                try
                {
                    Project.Blocks.Add(block);
                    Project.SaveBlock(block);
                    OpenBlock(block);
                    Notify("Created the “" + name + "” block.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void CreateBlockFromSelection()
    {
        PageDoc? page = CurrentPage;
        PageNode? node = CurrentNode;

        if (page is null || node is null || Project is null)
        {
            return;
        }

        OpenPrompt(new PromptRequest
        {
            Title = "Save as block",
            Label = "Block name",
            Value = node.DisplayName(),
            Confirm = "Create block",
            OnConfirm = value =>
            {
                if (Project is null)
                {
                    return;
                }

                var name = string.IsNullOrWhiteSpace(value) ? node.DisplayName() : value.Trim();
                var block = new BlockDoc
                {
                    Name = name,
                    Kind = "content",
                    Category = "Section",
                    Nodes = { node.Clone() },
                };

                try
                {
                    Project.Blocks.Add(block);
                    Project.SaveBlock(block);
                    OpenBlock(block);
                    Notify("Saved “" + name + "” as a reusable block.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void SetBlockMeta(BlockDoc block, Action<BlockDoc> edit)
    {
        try
        {
            edit(block);
            Project!.SaveBlock(block);

            if (EditingBlockId == block.Id)
            {
                SyncTabTitle(StudioMode.Blocks, block.Id, block.Name, "fa-cube");
            }

            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public int BlockUsageCount(BlockDoc block)
    {
        if (Project is null)
        {
            return 0;
        }

        int pages = Project.Pages.Sum(page => CountReferences(page.Nodes, block.Id));
        int blocks = Project.Blocks
            .Where(candidate => candidate.Id != block.Id)
            .Sum(candidate => CountReferences(candidate.Nodes, block.Id));

        return pages + blocks;
    }

    public List<PageDoc> PagesUsing(BlockDoc block) =>
        Project?.Pages.Where(page => CountReferences(page.Nodes, block.Id) > 0).ToList() ?? new List<PageDoc>();

    private static int CountReferences(List<PageNode> nodes, string blockId)
    {
        int count = 0;

        foreach (PageNode node in nodes)
        {
            if (node.Tag == "block"
                && node.Attrs is not null
                && node.Attrs.TryGetValue("block", out string? id)
                && id == blockId)
            {
                count++;
            }

            if (node.Children is not null)
            {
                count += CountReferences(node.Children, blockId);
            }
        }

        return count;
    }

    public void DeleteBlock(BlockDoc block)
    {
        int usage = BlockUsageCount(block);

        AskConfirm(new ConfirmRequest
        {
            Title = "Delete the “" + block.Name + "” block?",
            Message = usage == 0
                ? "The block is removed from the project."
                : "It is used " + usage + " time" + (usage == 1 ? "" : "s") + ". Those pages will show a missing block until it is replaced.",
            Confirm = "Delete block",
            OnConfirm = () =>
            {
                try
                {
                    Project!.DeleteBlock(block);

                    if (EditingBlockId == block.Id)
                    {
                        EditingBlockId = null;
                        _blockDocument = null;
                    }

                    if (Tabs.FirstOrDefault(tab => tab.Mode == StudioMode.Blocks && tab.ItemId == block.Id) is { } tab)
                    {
                        CloseTab(tab.Id);
                    }

                    Notify("Block deleted.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    /// <summary>
    /// Applies a core prefab to the block currently being edited, or creates a
    /// new theme block when the library is open. Existing content asks first.
    /// </summary>
    public void UseCorePrefab(string prefabId)
    {
        CoreBlockPrefab? prefab = CorePrefabs.Find(prefabId);

        if (prefab is null || Project is null)
        {
            return;
        }

        BlockDoc? block = CurrentBlock;

        if (block is null)
        {
            AddCorePrefab(prefab);
            return;
        }

        ApplyCorePrefab(block, prefab);
    }

    private void AddCorePrefab(CoreBlockPrefab prefab)
    {
        if (Project is null)
        {
            return;
        }

        string brand = Project.Manifest.Name;
        var block = new BlockDoc
        {
            Name = UniqueBlockName(prefab.Name),
            Description = prefab.Description,
            Kind = "theme",
            Category = prefab.Category,
            Nodes = { prefab.Build(string.IsNullOrWhiteSpace(brand) ? "Your site" : brand) },
        };

        try
        {
            Project.Blocks.Add(block);
            Project.SaveBlock(block);
            OpenBlock(block);
            Notify("Created “" + block.Name + "” from the prefab kit.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    private void ApplyCorePrefab(BlockDoc block, CoreBlockPrefab prefab)
    {
        if (Project is null)
        {
            return;
        }

        try
        {
            string brand = Project.Manifest.Name;
            block.Nodes = [prefab.Build(string.IsNullOrWhiteSpace(brand) ? "Your site" : brand)];
            block.Kind = "theme";
            block.Category = prefab.Category;

            if (string.IsNullOrWhiteSpace(block.Description))
            {
                block.Description = prefab.Description;
            }

            Project.SaveBlock(block);
            _blockDocument = null;
            SelectedNodeId = null;
            CollapsedNodes.Clear();
            SyncTabTitle(StudioMode.Blocks, block.Id, block.Name, "fa-cube");
            MarkChanged();
            Notify("Applied the “" + prefab.Name + "” layout.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    private static bool IsPlaceholderBlock(BlockDoc block) =>
        block.Nodes.Count == 1
        && !block.Nodes[0].HasChildren
        && string.IsNullOrWhiteSpace(block.Nodes[0].Text);

    private string UniqueBlockName(string seed)
    {
        if (Project is null)
        {
            return seed;
        }

        string name = seed;
        int index = 2;

        while (Project.Blocks.Any(block => block.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            name = seed + " " + index++;
        }

        return name;
    }

    public void AddSavedBlock(BlockDoc block) =>
        AddWidget("block:" + block.Id, SelectedNodeId);

    public List<BlockDoc> ThemeBlocks =>
        Project?.Blocks.Where(block => block.Kind == "theme").ToList() ?? new List<BlockDoc>();
}
