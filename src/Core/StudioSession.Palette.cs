namespace WebCanvasDotNet.Core;

public sealed class PaletteEntry
{
    public string Label { get; set; } = "";

    public string Hint { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Group { get; set; } = "";

    public Action Action { get; set; } = () => { };
}

public sealed partial class StudioSession
{
    public List<PaletteEntry> PaletteCommands()
    {
        var entries = new List<PaletteEntry>();

        void Add(string label, string hint, string icon, string group, Action action) =>
            entries.Add(new PaletteEntry
            {
                Label = label,
                Hint = hint,
                Icon = icon,
                Group = group,
                Action = () =>
                {
                    PaletteOpen = false;
                    PaletteQuery = "";
                    action();
                    MarkChanged();
                },
            });

        foreach (var mode in ModeInfo.All)
        {
            var shortcut = "Ctrl+" + ((int)mode + 1);
            Add("Go to " + ModeInfo.Label(mode), ModeInfo.Hint(mode) + " · " + shortcut, ModeInfo.Icon(mode), "Navigate", () => OpenMode(mode));
        }

        if (Project is not null)
        {
            foreach (var page in Project.Pages.Take(8))
            {
                Add("Open page: " + page.Title, page.IsHome ? "Home · /" : "/" + page.Slug, "fa-shapes", "Pages", () => SelectPage(page.Id));
            }

            foreach (var candidate in Project.Schemas)
            {
                var capturedSchema = candidate;
                Add("Open type: " + candidate.Name, Project.CountOf(candidate) + " records", candidate.Icon, "Data", () => SelectSchema(capturedSchema.Name));
            }

            Add("New page", "Create a page and start designing", "fa-plus", "Create", () => NewPage());
            Add("New data type", "Define fields and relationships", "fa-database", "Create", () => NewSchema());

            if (CurrentSchema is { } schema)
            {
                Add("New " + schema.Name + " record", "Add a record to " + schema.Plural, "fa-circle-plus", "Create", () => NewEntity(schema));
            }

            Add("New automation handler", "React to a project event", "fa-bolt", "Create", () => NewHandler(SelectedEvent));

            if (ActiveMode is StudioMode.Design or StudioMode.Blocks && CurrentNode is not null)
            {
                Add("Copy block", "Ctrl+C", "fa-copy", "Edit", CopyNode);
                Add("Cut block", "Ctrl+X", "fa-scissors", "Edit", CutNode);
                Add("Duplicate block", "Ctrl+D", "fa-copy", "Edit", DuplicateNode);
                Add("Delete block", "Delete", "fa-trash-can", "Edit", DeleteNode);
            }

            if (HasClipboard)
            {
                Add("Paste block", "Ctrl+V", "fa-paste", "Edit", PasteNode);
            }

            Add("Build site", "Generate the static site", "fa-cloud-arrow-up", "Project", () => RunBuild());

            if (CurrentPage is { } previewPage && !IsEditingBlock)
            {
                Add("Preview page", "Build and open in a new tab", "fa-eye", "Project", () => PreviewPage(previewPage));
            }
            Add("Open build settings", "Output, search and sitemap", "fa-sliders", "Project", () => SelectDevSection(DevSection.Build));
            Add("Regenerate APIs", "Rewrite app/api/index.json", "fa-globe", "Project", () => RegenerateApis());
            Add("Open project folder", "Show .wc, app and assets", "fa-folder-open", "Project", () => OpenFolder(Project.Root));
            Add("Save everything", "Write all project files", "fa-floppy-disk", "Project", () => SaveEverything());
        }

        Add("Create project", "Start a blank or starter site", "fa-folder-plus", "Project", OpenCreateDialog);
        Add("Open project", "Choose an existing .wc folder", "fa-folder-open", "Project", BrowseForProject);
        Add("Open the sample site", "A complete editorial starter", "fa-star", "Project", OpenSampleProject);

        foreach (var section in ModeInfo.DevSections)
        {
            Add("Development: " + ModeInfo.DevLabel(section), "Theme, plugins, APIs and builds", ModeInfo.DevIcon(section), "Develop", () => SelectDevSection(section));
        }

        Add(Dark ? "Switch to light theme" : "Switch to dark theme", "Ctrl+Shift+L", Dark ? "fa-sun" : "fa-moon", "View", ToggleTheme);
        Add(ShowSidebar ? "Hide sidebar" : "Show sidebar", "Ctrl+B", "fa-bars", "View", ToggleSidebar);
        Add(ShowInspector ? "Hide inspector" : "Show inspector", "Ctrl+Shift+I", "fa-sliders", "View", ToggleInspector);
        Add("Interface text: default", "100 percent", "fa-font", "View", () => SetUiScale(100));
        Add("Interface text: comfortable", "115 percent", "fa-font", "View", () => SetUiScale(115));
        Add("Interface text: large", "130 percent", "fa-font", "View", () => SetUiScale(130));

        if (ActiveTab is { } tab)
        {
            Add("Close tab: " + tab.Title, ModeInfo.Label(tab.Mode), "fa-xmark", "View", () => CloseTab(tab.Id));
        }

        var query = PaletteQuery.Trim();

        if (query.Length == 0)
        {
            return entries.Take(12).ToList();
        }

        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return entries
            .Where(entry => tokens.All(token =>
                entry.Label.Contains(token, StringComparison.OrdinalIgnoreCase)
                || entry.Hint.Contains(token, StringComparison.OrdinalIgnoreCase)
                || entry.Group.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.Label.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(entry => entry.Label.Length)
            .ToList();
    }
}
