using System.Diagnostics;
using System.Text.Json;
using Donkeh.Hosting;

namespace WebCanvasDotNet.Core;

public enum StudioMode
{
    Content,
    Design,
    Blocks,
    Data,
    Automation,
    Development,
    Preview,
}

public enum DevSection
{
    Overview,
    Theme,
    Plugins,
    Api,
    Build,
}

public static class ModeInfo
{
    public static readonly StudioMode[] All =
    [
        StudioMode.Content, StudioMode.Design, StudioMode.Blocks, StudioMode.Data, StudioMode.Automation,
        StudioMode.Development,
    ];

    public static string Label(StudioMode mode) => mode switch
    {
        StudioMode.Content => "Content",
        StudioMode.Design => "Design",
        StudioMode.Blocks => "Blocks",
        StudioMode.Data => "Data",
        StudioMode.Automation => "Automation",
        StudioMode.Preview => "Preview",
        _ => "Development",
    };

    public static string Icon(StudioMode mode) => mode switch
    {
        StudioMode.Content => "fa-file",
        StudioMode.Design => "fa-shapes",
        StudioMode.Blocks => "fa-cube",
        StudioMode.Data => "fa-database",
        StudioMode.Automation => "fa-bolt",
        StudioMode.Preview => "fa-eye",
        _ => "fa-code",
    };

    public static string RailLabel(StudioMode mode) => mode == StudioMode.Development ? "Dev" : Label(mode);

    public static string Hint(StudioMode mode) => mode switch
    {
        StudioMode.Content => "Write and organise records",
        StudioMode.Design => "Compose pages by dragging blocks",
        StudioMode.Blocks => "Build reusable blocks",
        StudioMode.Data => "Model types and relationships",
        StudioMode.Automation => "React to events",
        StudioMode.Preview => "See the compiled page in a browser view",
        _ => "Theme, plugins, APIs and builds",
    };

    public static string DevLabel(DevSection section) => section switch
    {
        DevSection.Overview => "Overview",
        DevSection.Theme => "Theme",
        DevSection.Plugins => "Plugins",
        DevSection.Api => "APIs",
        _ => "Build",
    };

    public static string DevIcon(DevSection section) => section switch
    {
        DevSection.Overview => "fa-circle-info",
        DevSection.Theme => "fa-palette",
        DevSection.Plugins => "fa-puzzle-piece",
        DevSection.Api => "fa-globe",
        _ => "fa-cloud-arrow-up",
    };

    public static readonly DevSection[] DevSections =
    [
        DevSection.Overview, DevSection.Theme, DevSection.Plugins, DevSection.Api, DevSection.Build,
    ];
}

public sealed class WorkTab
{
    public string Id { get; set; } = Ids.New("tab");

    public StudioMode Mode { get; set; }

    public string ItemId { get; set; } = "";

    public string Title { get; set; } = "";

    public string Icon { get; set; } = "";
}

public sealed class Toast
{
    public string Id { get; set; } = Ids.New("toast");

    public string Message { get; set; } = "";

    public string Kind { get; set; } = "info";
}

public sealed class PromptRequest
{
    public string Title { get; set; } = "";

    public string Label { get; set; } = "";

    public string Value { get; set; } = "";

    public string Placeholder { get; set; } = "";

    public string Confirm { get; set; } = "Continue";

    public bool Multiline { get; set; }

    public Action<string>? OnConfirm { get; set; }
}

public sealed class ConfirmRequest
{
    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public string Confirm { get; set; } = "Delete";

    public bool Danger { get; set; } = true;

    public bool ShowCancel { get; set; } = true;

    public Action? OnConfirm { get; set; }
}

public sealed class PageSnapshot
{
    public string PageId { get; set; } = "";

    public string Json { get; set; } = "";

    public string Label { get; set; } = "";

    public string? SelectedNodeId { get; set; }
}

public sealed class AutomationLogEntry
{
    public DateTimeOffset At { get; set; } = DateTimeOffset.Now;

    public string Handler { get; set; } = "";

    public string Message { get; set; } = "";

    public string Kind { get; set; } = "info";
}

public sealed class Preferences
{
    public bool Dark { get; set; }

    public bool ShowSidebar { get; set; } = true;

    public bool ShowInspector { get; set; } = true;

    public string? LastProject { get; set; }

    public List<string> RecentProjects { get; set; } = new();

    public int DesignZoom { get; set; } = 60;

    public string Viewport { get; set; } = "desktop";

    /// <summary>One-time migration marker for defaults that change over releases.</summary>
    public int ZoomVersion { get; set; }

    /// <summary>Interface text scale: 100, 115 or 130 percent.</summary>
    public int UiScale { get; set; } = 115;
}

/// <summary>
/// The application's single mutable session. Views read it during render and
/// call its methods from event handlers; <see cref="Refresh"/> asks the root
/// component to rebuild the tree.
/// </summary>
public sealed partial class StudioSession
{
    public static readonly (string Id, string Label, int Width, string Icon)[] Viewports =
    [
        ("desktop", "Desktop", 1200, "fa-desktop"),
        ("tablet", "Tablet", 834, "fa-tablet"),
        ("mobile", "Mobile", 390, "fa-mobile"),
    ];

    private readonly List<PageSnapshot> _undo = new();
    private readonly List<PageSnapshot> _redo = new();
    private PageNode? _clipboard;
    private string _lastCaptureKey = "";
    private long _lastCaptureTick;
    private readonly string _preferencesPath;
    private bool _initialised;

    public StudioSession()
    {
        var folder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebCanvasDotNet");
        Directory.CreateDirectory(folder);
        _preferencesPath = System.IO.Path.Combine(folder, "preferences.json");
        Preferences = LoadPreferences();
    }

    public Action? Refresh { get; set; }

    public Preferences Preferences { get; private set; }

    public WcProject? Project { get; private set; }

    public bool HasProject => Project is not null;

    public string? ProjectError { get; private set; }

    // ------------------------------------------------------------ shell state

    public List<WorkTab> Tabs { get; } = new();

    public string? ActiveTabId { get; set; }

    public StudioMode ActiveMode { get; private set; } = StudioMode.Design;

    public bool ShowSidebar { get; private set; } = true;

    public bool ShowInspector { get; private set; } = true;

    public bool IsTyping { get; set; }

    public string SaveState { get; private set; } = "All changes saved";

    public List<Toast> Toasts { get; } = new();

    public bool HasClipboard => _clipboard is not null;

    public bool PaletteOpen { get; private set; }

    public string PaletteQuery { get; private set; } = "";

    public int PaletteIndex { get; set; }

    public PromptRequest? Prompt { get; private set; }

    public ConfirmRequest? Confirm { get; private set; }

    public bool CreateOpen { get; private set; }

    public string CreateName { get; set; } = "";

    public string CreateParent { get; set; } = "";

    public bool CreateStarter { get; set; } = true;

    public string? CreateError { get; set; }

    public bool ShowModeMenu { get; private set; }

    public string StatusHint { get; set; } = "";

    // -------------------------------------------------------- content can leak

    public WorkTab? ActiveTab =>
        Tabs.FirstOrDefault(tab => tab.Id == ActiveTabId) ?? Tabs.FirstOrDefault();

    public bool Dark => Preferences.Dark;

    public void Initialize()
    {
        if (_initialised)
        {
            return;
        }

        _initialised = true;

        if (Preferences.ZoomVersion < 1)
        {
            Preferences.DesignZoom = 60;
            Preferences.ZoomVersion = 1;
            SavePreferences();
        }

        var startup = Environment.GetCommandLineArgs();

        if (startup.Contains("--dark"))
        {
            Preferences.Dark = true;
            SavePreferences();
        }
        else if (startup.Contains("--light"))
        {
            Preferences.Dark = false;
            SavePreferences();
        }

        if (Argument(startup, "--scale") is { } scaleText
            && int.TryParse(scaleText, out int scaleValue))
        {
            Preferences.UiScale = Math.Clamp(scaleValue, 100, 130);
            SavePreferences();
        }

        // --resume restores the last project before the remaining startup flags
        // are applied, so --mode and --section act on the restored project.
        if (Project is null
            && startup.Contains("--resume")
            && !string.IsNullOrWhiteSpace(Preferences.LastProject)
            && Directory.Exists(Preferences.LastProject))
        {
            try
            {
                SetProject(WcProject.Open(Preferences.LastProject!));
            }
            catch
            {
                Preferences.LastProject = null;
                SavePreferences();
            }
        }

        ApplyStartupArguments();

        var uiArguments = Environment.GetCommandLineArgs();

        if (uiArguments.Contains("--new-page"))
        {
            NewPage();
        }

        if (uiArguments.Contains("--create-dialog"))
        {
            OpenCreateDialog();
        }
        else if (uiArguments.Contains("--palette"))
        {
            OpenPalette();

            if (Argument(uiArguments, "--query") is { } paletteQuery)
            {
                SetPaletteQuery(paletteQuery);
            }
        }
    }

    public void Notify(string message, string kind = "info")
    {
        Toasts.Insert(0, new Toast { Message = message, Kind = kind });

        while (Toasts.Count > 4)
        {
            Toasts.RemoveAt(Toasts.Count - 1);
        }

        MarkChanged();
    }

    public void Dismiss(Toast toast)
    {
        Toasts.Remove(toast);
        MarkChanged();
    }

    public void MarkChanged()
    {
        SaveState = "All changes saved";
        Refresh?.Invoke();
    }

    public void SetStatus(string hint)
    {
        StatusHint = hint;
        MarkChanged();
    }

    // ------------------------------------------------------------ preferences

    private Preferences LoadPreferences()
    {
        try
        {
            if (File.Exists(_preferencesPath))
            {
                return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(_preferencesPath), WcJson.Options)
                       ?? new Preferences();
            }
        }
        catch
        {
            // A corrupt preferences file should reset quietly.
        }

        return new Preferences();
    }

    public void SavePreferences()
    {
        try
        {
            File.WriteAllText(_preferencesPath, JsonSerializer.Serialize(Preferences, WcJson.Options));
        }
        catch
        {
            // Preferences are a convenience, never a reason to interrupt work.
        }
    }

    public void ToggleTheme()
    {
        Preferences.Dark = !Preferences.Dark;
        SavePreferences();
        MarkChanged();
    }

    public void SetUiScale(int scale)
    {
        Preferences.UiScale = Math.Clamp(scale, 100, 130);
        SavePreferences();
        MarkChanged();
    }

    public void ToggleSidebar()
    {
        ShowSidebar = !ShowSidebar;
        Preferences.ShowSidebar = ShowSidebar;
        SavePreferences();
        MarkChanged();
    }

    public void ToggleInspector()
    {
        ShowInspector = !ShowInspector;
        Preferences.ShowInspector = ShowInspector;
        SavePreferences();
        MarkChanged();
    }

    // ------------------------------------------------------------ project I/O

    public void SetProject(WcProject project)
    {
        StopPreviewServer();
        Project = project;
        ProjectError = null;
        Tabs.Clear();
        _undo.Clear();
        _redo.Clear();
        _lastCaptureKey = "";
        Toasts.Clear();
        SelectedPageId = project.HomePage?.Id;
        SelectedNodeId = null;
        EditingBlockId = null;
        _blockDocument = null;
        ContentMapOpen = true;
        CollapsedNodes.Clear();

        if (project.HomePage is { } home)
        {
            EnsureTreeState(home);
        }
        SelectedSchemaName = project.Schemas.FirstOrDefault()?.Name;
        SelectedEntityId = null;
        SelectedHandlerId = project.Handlers.FirstOrDefault()?.Id;
        SelectedEvent = "entity.created";
        BuildResult = null;
        BuildLog.Clear();
        AutomationLog.Clear();
        PaletteOpen = false;
        Prompt = null;
        Confirm = null;
        CreateOpen = false;

        Preferences.LastProject = project.Root;
        Preferences.RecentProjects.RemoveAll(path => string.Equals(path, project.Root, StringComparison.OrdinalIgnoreCase));
        Preferences.RecentProjects.Insert(0, project.Root);
        Preferences.RecentProjects = Preferences.RecentProjects.Take(10).ToList();
        SavePreferences();

        OpenTab(StudioMode.Design, project.HomePage?.Id ?? "", project.HomePage?.Title ?? "Design", "fa-shapes");

        foreach (var warning in project.Warnings.Take(3))
        {
            Notify(warning, "warning");
        }

        if (project.Warnings.Count > 3)
        {
            Notify(project.Warnings.Count + " project warnings were recorded.", "warning");
        }

        ApplyStartupSelection();
        MarkChanged();
    }

    public void OpenProject(string folder)
    {
        try
        {
            SetProject(WcProject.Open(folder));
        }
        catch (Exception exception)
        {
            ProjectError = exception.Message;
            Notify(exception.Message, "error");
        }
    }

    public void CloseProject()
    {
        StopPreviewServer();
        Project = null;
        Tabs.Clear();
        ActiveTabId = null;
        SelectedPageId = null;
        SelectedNodeId = null;
        SelectedSchemaName = null;
        SelectedEntityId = null;
        SelectedHandlerId = null;
        Preferences.LastProject = null;
        SavePreferences();
        MarkChanged();
    }

    public void OpenSampleProject()
    {
        var parent = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebCanvasDotNet");
        Directory.CreateDirectory(parent);

        var root = System.IO.Path.Combine(parent, "Aster & Oak sample");

        if (File.Exists(System.IO.Path.Combine(root, ".wc")))
        {
            OpenProject(root);
            return;
        }

        try
        {
            var project = WcProject.Create(parent, "Aster & Oak sample", starter: true);
            SetProject(project);
            Notify("The Aster & Oak sample is ready.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public List<string> RecentProjects =>
        Preferences.RecentProjects.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    // --------------------------------------------------------------- create UI

    public void OpenCreateDialog()
    {
        CreateOpen = true;
        CreateError = null;
        CreateName = "";
        CreateStarter = true;
        CreateParent = Preferences.LastProject is not null
            ? System.IO.Path.GetDirectoryName(Preferences.LastProject) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        MarkChanged();
    }

    public void CloseCreateDialog()
    {
        CreateOpen = false;
        CreateError = null;
        MarkChanged();
    }

    public void ConfirmCreate()
    {
        if (string.IsNullOrWhiteSpace(CreateName))
        {
            CreateError = "Give the project a name first.";
            MarkChanged();
            return;
        }

        if (string.IsNullOrWhiteSpace(CreateParent) || !Directory.Exists(CreateParent))
        {
            CreateError = "Choose an existing parent folder.";
            MarkChanged();
            return;
        }

        try
        {
            var project = WcProject.Create(CreateParent, CreateName, CreateStarter);
            CreateOpen = false;
            SetProject(project);
            Notify("Created " + project.Name + ".", "success");
        }
        catch (Exception exception)
        {
            CreateError = exception.Message;
            MarkChanged();
        }
    }

    public void BrowseForParent()
    {
        OpenDirectoryDialog(
            "Choose where to create the project",
            CreateParent,
            path =>
            {
                CreateParent = path;
                MarkChanged();
            });
    }

    public void BrowseForProject()
    {
        OpenDirectoryDialog(
            "Open a WebCanvas project",
            Preferences.LastProject is not null
                ? System.IO.Path.GetDirectoryName(Preferences.LastProject) ?? ""
                : "",
            path =>
            {
                OpenProject(path);
            });
    }

    // ---------------------------------------------------------------- overlays

    public void OpenPalette()
    {
        PaletteOpen = true;
        PaletteQuery = "";
        PaletteIndex = 0;
        MarkChanged();
    }

    public void ClosePalette()
    {
        PaletteOpen = false;
        PaletteQuery = "";
        PaletteIndex = 0;
        MarkChanged();
    }

    public void SetPaletteQuery(string query)
    {
        PaletteQuery = query;
        PaletteIndex = 0;
        MarkChanged();
    }

    public void SetPaletteIndex(int index)
    {
        PaletteIndex = Math.Max(0, index);
        MarkChanged();
    }

    public void OpenPrompt(PromptRequest request)
    {
        Prompt = request;
        MarkChanged();
    }

    public void ClosePrompt()
    {
        Prompt = null;
        MarkChanged();
    }

    public void SubmitPrompt(string value)
    {
        var request = Prompt;

        if (request is null)
        {
            return;
        }

        Prompt = null;
        request.OnConfirm?.Invoke(value);
        MarkChanged();
    }

    public void AskConfirm(ConfirmRequest request)
    {
        Confirm = request;
        MarkChanged();
    }

    public void CloseConfirm()
    {
        Confirm = null;
        MarkChanged();
    }

    public void AcceptConfirm()
    {
        var request = Confirm;

        if (request is null)
        {
            return;
        }

        Confirm = null;
        request.OnConfirm?.Invoke();
        MarkChanged();
    }

    public void ToggleModeMenu()
    {
        ShowModeMenu = !ShowModeMenu;
        MarkChanged();
    }

    public void CloseModeMenu()
    {
        ShowModeMenu = false;
        MarkChanged();
    }

    // ------------------------------------------------------------ keyboard/UI

    public void HandleKey(string key)
    {
        if (PaletteOpen)
        {
            HandlePaletteKey(key);
            return;
        }

        if (Prompt is not null)
        {
            if (key == "Escape")
            {
                ClosePrompt();
            }

            return;
        }

        if (Confirm is not null)
        {
            if (key == "Escape")
            {
                CloseConfirm();
            }
            else if (key is "Enter" or "Return")
            {
                AcceptConfirm();
            }

            return;
        }

        if (CreateOpen)
        {
            if (key == "Escape")
            {
                CloseCreateDialog();
            }

            return;
        }

        if (ShowModeMenu && key == "Escape")
        {
            CloseModeMenu();
            return;
        }

        if (IsTyping)
        {
            return;
        }

        if (key is "Ctrl+K" or "Ctrl+P" or "F1")
        {
            OpenPalette();
            return;
        }

        if (key == "Ctrl+O")
        {
            BrowseForProject();
            return;
        }

        if (key == "Ctrl+B")
        {
            ToggleSidebar();
            return;
        }

        if (key == "Ctrl+Shift+L")
        {
            ToggleTheme();
            return;
        }

        if (key is "Ctrl+Shift+I" or "Ctrl+Alt+I")
        {
            ToggleInspector();
            return;
        }

        if (key == "Ctrl+Shift+B")
        {
            RunBuild();
            return;
        }

        if (key == "Ctrl+S")
        {
            SaveEverything("Everything is already saved.");
            return;
        }

        if (key == "Ctrl+W")
        {
            if (ActiveTab is { } tab)
            {
                CloseTab(tab.Id);
            }

            return;
        }

        if (key is "Ctrl+Z" or "Ctrl+Shift+Z" or "Ctrl+Y")
        {
            if (key == "Ctrl+Z")
            {
                Undo();
            }
            else
            {
                Redo();
            }

            return;
        }

        if (key == "Ctrl+C")
        {
            CopyNode();
            return;
        }

        if (key == "Ctrl+X")
        {
            CutNode();
            return;
        }

        if (key == "Ctrl+V")
        {
            PasteNode();
            return;
        }

        if (key == "Ctrl+D")
        {
            DuplicateNode();
            return;
        }

        if (key == "Backspace"
            || key.Equals("Delete", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Del", StringComparison.OrdinalIgnoreCase))
        {
            if (SelectedNodeId is not null)
            {
                DeleteNode();
            }

            return;
        }

        if (key == "Escape")
        {
            if (ShowModeMenu)
            {
                CloseModeMenu();
            }
            else if (SelectedNodeId is not null)
            {
                SelectedNodeId = null;
                MarkChanged();
            }

            return;
        }

        if (key is "Ctrl+1" or "Ctrl+2" or "Ctrl+3" or "Ctrl+4" or "Ctrl+5" or "Ctrl+6")
        {
            var index = key[^1] - '1';

            if (index >= 0 && index < ModeInfo.All.Length)
            {
                OpenMode(ModeInfo.All[index]);
            }
        }
    }

    private void HandlePaletteKey(string key)
    {
        if (key == "Escape")
        {
            ClosePalette();
            return;
        }

        if (key == "Backspace")
        {
            if (PaletteQuery.Length > 0)
            {
                SetPaletteQuery(PaletteQuery[..^1]);
            }

            return;
        }

        if (key == "Down")
        {
            var commands = PaletteCommands();
            PaletteIndex = commands.Count == 0 ? 0 : Math.Min(PaletteIndex + 1, commands.Count - 1);
            MarkChanged();
            return;
        }

        if (key == "Up")
        {
            PaletteIndex = Math.Max(PaletteIndex - 1, 0);
            MarkChanged();
            return;
        }

        if (key is "Enter" or "Return")
        {
            var commands = PaletteCommands();

            if (commands.Count > 0)
            {
                commands[Math.Clamp(PaletteIndex, 0, commands.Count - 1)].Action();
            }

            return;
        }

        if (key.Length == 1 && !char.IsControl(key[0]))
        {
            SetPaletteQuery(PaletteQuery + key);
            return;
        }

        if (key == "Space")
        {
            SetPaletteQuery(PaletteQuery + " ");
        }
    }

    public void SaveEverything(string? message = null)
    {
        if (Project is null)
        {
            return;
        }

        try
        {
            Project.SaveEverything();
            Project.GenerateApiDefinitions();
            SaveState = "All changes saved";
            Notify(message ?? "Project saved.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    // ---------------------------------------------------------------- startup

    private void ApplyStartupArguments()
    {
        var args = Environment.GetCommandLineArgs();
        var projectPath = Argument(args, "--project");
        var createParent = Argument(args, "--create");
        var createName = Argument(args, "--name");

        try
        {
            if (!string.IsNullOrWhiteSpace(createParent))
            {
                var name = string.IsNullOrWhiteSpace(createName)
                    ? "WebCanvas site"
                    : createName;
                var project = WcProject.Create(createParent!, name, starter: !args.Contains("--blank"));
                SetProject(project);
                Notify("Created " + project.Name + ".", "success");
            }
            else if (!string.IsNullOrWhiteSpace(projectPath))
            {
                SetProject(WcProject.Open(projectPath!));
            }
        }
        catch (Exception exception)
        {
            ProjectError = exception.Message;

            try
            {
                var logFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WebCanvasDotNet");
                Directory.CreateDirectory(logFolder);
                File.WriteAllText(System.IO.Path.Combine(logFolder, "last-error.log"), exception.ToString());
            }
            catch
            {
                // Diagnostics must never replace the original failure.
            }
        }

        if (args.Contains("--build") && Project is not null)
        {
            RunBuild();
        }

        var mode = Argument(args, "--mode");

        if (!string.IsNullOrWhiteSpace(mode) && Enum.TryParse<StudioMode>(mode, ignoreCase: true, out var requested))
        {
            OpenMode(requested);
        }

        if (args.Contains("--preview"))
        {
            CanvasEditing = false;
        }



        var section = Argument(args, "--section");

        if (!string.IsNullOrWhiteSpace(section) && Enum.TryParse<DevSection>(section, ignoreCase: true, out var dev))
        {
            SelectDevSection(dev);
        }

    }

    private void ApplyStartupSelection()
    {
        var arguments = Environment.GetCommandLineArgs();
        var page = Argument(arguments, "--page");
        var type = Argument(arguments, "--type");
        var record = Argument(arguments, "--record");
        var node = Argument(arguments, "--node");
        var block = Argument(arguments, "--block");
        var showType = Argument(arguments, "--show-on-site");

        if (!string.IsNullOrWhiteSpace(type) && Project?.Schema(type) is { } selectedSchema)
        {
            SelectedSchemaName = selectedSchema.Name;
            ContentMapOpen = false;
        }

        if (!string.IsNullOrWhiteSpace(record))
        {
            SelectedEntityId = record;
            ContentMapOpen = false;
        }

        if (!string.IsNullOrWhiteSpace(node))
        {
            SelectedNodeId = node;
        }

        if (!string.IsNullOrWhiteSpace(block) && Project?.Block(block) is { } blockDoc)
        {
            OpenBlock(blockDoc);
        }

        if (!string.IsNullOrWhiteSpace(showType) && Project?.Schema(showType) is { } schemaToShow)
        {
            ShowSchemaOnSite(schemaToShow);
        }

        if (!string.IsNullOrWhiteSpace(page) && Project is not null)
        {
            var target = Project.Pages.FirstOrDefault(candidate =>
                candidate.Slug.Equals(page, StringComparison.OrdinalIgnoreCase)
                || candidate.Title.Equals(page, StringComparison.OrdinalIgnoreCase));

            if (target is not null)
            {
                SelectedPageId = target.Id;
                OpenTab(StudioMode.Design, target.Id, target.Title, "fa-shapes");
            }
        }
    }

    private static string? Argument(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);

        if (index >= 0 && index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return args[index + 1];
        }

        return null;
    }

    // ----------------------------------------------------------------- helpers

    public void OpenFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Notify("That folder doesn't exist yet.", "warning");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            Notify("Couldn't open the folder: " + exception.Message, "error");
        }
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            Notify("Couldn't open the link: " + exception.Message, "error");
        }
    }

    public string RelativePath(string path)
    {
        if (Project is null)
        {
            return path;
        }

        try
        {
            return System.IO.Path.GetRelativePath(Project.Root, path).Replace('\\', '/');
        }
        catch
        {
            return path;
        }
    }

    public int SelectedViewportWidth =>
        Viewports.FirstOrDefault(viewport => viewport.Id == Preferences.Viewport).Width is var width and > 0
            ? width
            : 1280;

    public string SelectedViewportLabel =>
        Viewports.FirstOrDefault(viewport => viewport.Id == Preferences.Viewport).Label ?? "Desktop";

    public double CanvasScale => Math.Clamp(Preferences.DesignZoom, 25, 200) / 100.0;

    public void SetViewport(string id)
    {
        Preferences.Viewport = id;
        Preferences.DesignZoom = id switch
        {
            "tablet" => 80,
            "mobile" => 100,
            _ => 60,
        };

        SavePreferences();
        MarkChanged();
    }

    public void SetZoom(int zoom)
    {
        Preferences.DesignZoom = Math.Clamp(zoom, 25, 200);
        SavePreferences();
        MarkChanged();
    }

    public void ZoomBy(int delta) => SetZoom(Preferences.DesignZoom + delta);
}
