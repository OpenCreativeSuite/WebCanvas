using OpenCreativeSuite.Components.Files;

namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public string? SelectedPageId { get; set; }

    public string? SelectedNodeId { get; set; }

    public string? SelectedSchemaName { get; set; }

    public string? SelectedEntityId { get; set; }

    public string? SelectedHandlerId { get; set; }

    public string SelectedEvent { get; set; } = "entity.created";

    public DevSection DevSection { get; private set; } = DevSection.Overview;

    public PageDoc? CurrentPage =>
        EditingBlockId is not null
            ? BlockDocument()
            : Project?.Page(SelectedPageId) ?? Project?.HomePage;

    public PageNode? CurrentNode => CurrentPage?.Find(SelectedNodeId);

    public DataSchema? CurrentSchema =>
        Project?.Schema(SelectedSchemaName) ?? Project?.Schemas.FirstOrDefault();

    public EntityDoc? CurrentEntity =>
        CurrentSchema is { } schema ? Project?.Entity(schema, SelectedEntityId) : null;

    public HandlerDoc? CurrentHandler =>
        Project?.Handlers.FirstOrDefault(handler => handler.Id == SelectedHandlerId);

    // ------------------------------------------------------------------ tabs

    public void OpenMode(StudioMode mode)
    {
        var (itemId, title, icon) = mode switch
        {
            StudioMode.Content => ContentMapOpen
                ? ("map", "Map", "fa-diagram-project")
                : (CurrentSchema?.Name ?? "", CurrentSchema?.Name ?? "Content", "fa-file"),
            StudioMode.Design => (CurrentPage?.Id ?? "", CurrentPage?.Title ?? "Design", "fa-shapes"),
            StudioMode.Blocks => IsEditingBlock && CurrentBlock is { } block
                ? (block.Id, block.Name, "fa-cube")
                : ("library", "Blocks", "fa-cube"),
            StudioMode.Data => (
                CurrentSchema?.Name ?? "",
                CurrentSchema?.Name ?? "Data",
                "fa-database"),
            StudioMode.Automation => (
                SelectedEvent,
                EventLabel(SelectedEvent),
                "fa-bolt"),
            _ => (
                DevSection.ToString(),
                ModeInfo.DevLabel(DevSection),
                ModeInfo.DevIcon(DevSection)),
        };

        OpenTab(mode, itemId, title, icon);
    }

    public void OpenTab(StudioMode mode, string itemId, string title, string icon)
    {
        ActiveMode = mode;

        var existing = Tabs.FirstOrDefault(tab =>
            tab.Mode == mode && string.Equals(tab.ItemId, itemId, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.Title = title;
            existing.Icon = icon;
            ActiveTabId = existing.Id;
            MarkChanged();
            return;
        }

        var tab = new WorkTab
        {
            Mode = mode,
            ItemId = itemId,
            Title = title,
            Icon = icon,
        };

        Tabs.Add(tab);
        ActiveTabId = tab.Id;

        while (Tabs.Count > 10)
        {
            var victim = Tabs.FirstOrDefault(candidate => candidate.Id != ActiveTabId);

            if (victim is null)
            {
                break;
            }

            Tabs.Remove(victim);
        }

        MarkChanged();
    }

    public void ActivateTab(string id)
    {
        var tab = Tabs.FirstOrDefault(candidate => candidate.Id == id);

        if (tab is null)
        {
            return;
        }

        ActiveTabId = tab.Id;
        ActiveMode = tab.Mode;

        switch (tab.Mode)
        {
            case StudioMode.Content:
                ContentMapOpen = tab.ItemId == "map";

                if (!ContentMapOpen && Project?.Schema(tab.ItemId) is { } schema)
                {
                    SelectedSchemaName = schema.Name;
                }

                break;
            case StudioMode.Design:
                if (Project?.Page(tab.ItemId) is { } page)
                {
                    EditingBlockId = null;
                    _blockDocument = null;
                    SelectedPageId = page.Id;
                }

                break;
            case StudioMode.Blocks:
                if (tab.ItemId == "library")
                {
                    EditingBlockId = null;
                    _blockDocument = null;
                    SelectedNodeId = null;
                }
                else if (Project?.Block(tab.ItemId) is { } block)
                {
                    EditingBlockId = block.Id;
                    SelectedPageId = null;
                    SelectedNodeId = null;
                    _blockDocument = null;
                }

                break;
            case StudioMode.Data:
                if (Project?.Schema(tab.ItemId) is { } type)
                {
                    SelectedSchemaName = type.Name;
                }

                break;
            case StudioMode.Automation:
                SelectedEvent = string.IsNullOrWhiteSpace(tab.ItemId) ? "entity.created" : tab.ItemId;
                break;
            case StudioMode.Development:
                if (Enum.TryParse<DevSection>(tab.ItemId, out var section))
                {
                    DevSection = section;
                }

                break;
            case StudioMode.Preview:
                break;
        }

        MarkChanged();
    }

    public void CloseTab(string id)
    {
        var index = Tabs.FindIndex(tab => tab.Id == id);

        if (index < 0)
        {
            return;
        }

        bool wasPreview = Tabs[index].Mode == StudioMode.Preview;
        Tabs.RemoveAt(index);

        if (wasPreview && Tabs.All(tab => tab.Mode != StudioMode.Preview))
        {
            StopPreviewServer();
        }

        if (ActiveTabId == id)
        {
            var next = Tabs.Count == 0
                ? null
                : Tabs[Math.Clamp(index - 1, 0, Tabs.Count - 1)];

            ActiveTabId = next?.Id;

            if (next is not null)
            {
                ActivateTab(next.Id);
                return;
            }
        }

        MarkChanged();
    }

    public void CloseActiveTab()
    {
        if (ActiveTab is { } tab)
        {
            CloseTab(tab.Id);
        }
    }

    private void SyncTabTitle(StudioMode mode, string itemId, string title, string icon)
    {
        var tab = Tabs.FirstOrDefault(candidate =>
            candidate.Mode == mode && string.Equals(candidate.ItemId, itemId, StringComparison.Ordinal));

        if (tab is null)
        {
            return;
        }

        tab.Title = title;
        tab.Icon = icon;
    }

    // ------------------------------------------------------------- selections

    public void SelectPage(string id)
    {
        if (Project?.Page(id) is not { } page)
        {
            return;
        }

        SelectedPageId = page.Id;
        SelectedNodeId = null;
        EditingBlockId = null;
        _blockDocument = null;
        EnsureTreeState(page);
        OpenTab(StudioMode.Design, page.Id, page.Title, "fa-shapes");
    }

    public void SelectNode(string? id)
    {
        SelectedNodeId = id;
        MarkChanged();
    }

    public void SelectSchema(string name, bool openContent = true)
    {
        if (Project?.Schema(name) is not { } schema)
        {
            return;
        }

        if (!string.Equals(SelectedSchemaName, schema.Name, StringComparison.Ordinal))
        {
            SelectedEntityId = null;
            EntityFilter = "all";
        }

        SelectedSchemaName = schema.Name;

        if (ActiveMode is StudioMode.Content or StudioMode.Data)
        {
            if (ActiveMode == StudioMode.Content && openContent)
            {
                ContentMapOpen = false;
            }

            OpenTab(ActiveMode, schema.Name, schema.Name, ActiveMode == StudioMode.Data ? "fa-database" : "fa-file");
        }
        else if (openContent)
        {
            ContentMapOpen = false;
            OpenTab(StudioMode.Content, schema.Name, schema.Name, "fa-file");
        }

        MarkChanged();
    }

    public void SelectEntity(string? id, bool openTab = true)
    {
        SelectedEntityId = id;

        if (openTab && CurrentSchema is { } schema)
        {
            ContentMapOpen = false;
            OpenTab(StudioMode.Content, schema.Name, schema.Name, "fa-file");
        }

        MarkChanged();
    }

    public void SelectHandler(string? id)
    {
        SelectedHandlerId = id;
        MarkChanged();
    }

    /// <summary>Switches from the data model to the records of a type.</summary>
    public void OpenRecords(DataSchema schema)
    {
        SelectedSchemaName = schema.Name;
        ActiveMode = StudioMode.Content;
        OpenTab(StudioMode.Content, schema.Name, schema.Name, "fa-file");
    }

    public void SelectDevSection(DevSection section)
    {
        DevSection = section;
        OpenTab(StudioMode.Development, section.ToString(), ModeInfo.DevLabel(section), ModeInfo.DevIcon(section));
    }

    public void SelectEvent(string eventId)
    {
        SelectedEvent = eventId;

        if (ActiveMode != StudioMode.Automation)
        {
            OpenTab(StudioMode.Automation, eventId, EventLabel(eventId), "fa-bolt");
        }
        else
        {
            var tab = ActiveTab;

            if (tab is { Mode: StudioMode.Automation })
            {
                tab.ItemId = eventId;
                tab.Title = EventLabel(eventId);
                tab.Icon = "fa-bolt";
            }
        }

        MarkChanged();
    }

    // ------------------------------------------------------------ event model

    public static readonly (string Id, string Label, string Icon, string Description, bool Scoped)[] Events =
    [
        ("entity.created", "Entity created", "fa-circle-plus", "A record is added to a data type.", true),
        ("entity.updated", "Entity updated", "fa-pen", "A record is saved with changes.", true),
        ("entity.deleted", "Entity deleted", "fa-trash-can", "A record is removed.", true),
        ("page.published", "Page published", "fa-cloud-arrow-up", "A page is published or re-published.", false),
        ("site.built", "Site built", "fa-gear", "A static build finishes.", false),
        ("manual.run", "Manual run", "fa-play", "A handler run from the Automation workspace.", false),
    ];

    public static string EventLabel(string id) =>
        Events.FirstOrDefault(item => item.Id == id).Label is { Length: > 0 } label and not null
            ? label
            : id;

    public static string EventIcon(string id) =>
        Events.FirstOrDefault(item => item.Id == id).Icon is { Length: > 0 } icon and not null
            ? icon
            : "fa-bolt";

    public List<HandlerDoc> HandlersFor(string eventId) =>
        Project?.Handlers.Where(handler => handler.Event == eventId).ToList() ?? new List<HandlerDoc>();

    // ---------------------------------------------------------- folder picker

    public void OpenDirectoryDialog(string title, string initialPath, Action<string> onChosen)
    {
        var theme = Preferences.Dark
            ? new FileBrowserTheme
            {
                Background = "#14171c",
                Surface = "#171a20",
                SurfaceHover = "#20252d",
                SurfaceSelected = "#243247",
                Border = "#2a303a",
                Text = "#e6e9ef",
                TextMuted = "#98a2b3",
                Accent = "#7aa2ff",
                Icon = "#98a2b3",
                SidebarBackground = "#101318",
                ToolbarBackground = "#171a20",
                ContextBackground = "#1c2027",
                DialogBackground = "#171a20",
                Focus = "#7aa2ff",
            }
            : new FileBrowserTheme
            {
                Background = "#ffffff",
                Surface = "#ffffff",
                SurfaceHover = "#f4f6f9",
                SurfaceSelected = "#e8efff",
                Border = "#e1e5eb",
                Text = "#171b22",
                TextMuted = "#5d6675",
                Accent = "#2456c7",
                Icon = "#697386",
                SidebarBackground = "#f7f8fa",
                ToolbarBackground = "#ffffff",
                ContextBackground = "#ffffff",
                DialogBackground = "#ffffff",
                Focus = "#2456c7",
            };

        FileBrowserDialogService.Open(new FileBrowserDialogRequest
        {
            Title = title,
            InitialPath = Directory.Exists(initialPath) ? initialPath : "",
            AllowFileSelection = false,
            AllowDirectorySelection = true,
            AllowMultiSelect = false,
            Theme = theme,
            DarkMode = Preferences.Dark,
            OnConfirm = items =>
            {
                var selected = items.FirstOrDefault();

                if (selected is not null && selected.IsDirectory)
                {
                    onChosen(selected.FullPath);
                }
            },
        });
    }
}
