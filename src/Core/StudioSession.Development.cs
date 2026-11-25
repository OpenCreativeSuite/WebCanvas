using Donkeh.Hosting;

namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public BuildOptions BuildOptions { get; } = new();

    public BuildResult? BuildResult { get; private set; }

    private PreviewServer? _previewServer;
    private string _previewRoot = "";

    public List<string> BuildLog { get; } = new();

    public string ThemeError { get; set; } = "";

    // ------------------------------------------------------------------ theme

    public void EditTheme(Action<ThemeDoc> edit, string label = "theme")
    {
        if (Project is null)
        {
            return;
        }

        try
        {
            edit(Project.Theme);
            Project.SaveTheme();
            ThemeError = "";
            SaveState = "Theme updated";
            MarkChanged();
        }
        catch (Exception exception)
        {
            ThemeError = exception.Message;
            Notify(exception.Message, "error");
        }
    }

    public void ApplyAccent(string accent)
    {
        if (!accent.StartsWith('#') || (accent.Length != 7 && accent.Length != 4))
        {
            ThemeError = "Use a hex colour such as #2f6b4f.";
            MarkChanged();
            return;
        }

        EditTheme(theme => theme.Accent = accent, "accent");
    }

    public void ResetTheme()
    {
        AskConfirm(new ConfirmRequest
        {
            Title = "Reset the theme?",
            Message = "Colour, type and radius return to the default studio values.",
            Confirm = "Reset theme",
            Danger = false,
            OnConfirm = () =>
            {
                if (Project is null)
                {
                    return;
                }

                Project.Theme = new ThemeDoc();
                Project.SaveTheme();
                Notify("Theme reset.", "success");
            },
        });
    }

    // ----------------------------------------------------------------- build

    public void SetBuildOption(Action<BuildOptions> edit)
    {
        edit(BuildOptions);
        MarkChanged();
    }

    public void RunBuild()
    {
        if (Project is null)
        {
            Notify("Open a project before building.", "warning");
            return;
        }

        Notify("Building the site…", "info");
        var result = StaticSiteBuilder.Build(Project, BuildOptions);
        BuildResult = result;
        BuildLog.Clear();
        BuildLog.AddRange(result.Log);

        try
        {
            Project.GenerateApiDefinitions();
        }
        catch (Exception exception)
        {
            BuildLog.Add("Couldn't write API definitions: " + exception.Message);
        }

        AutomationLog.Insert(0, new AutomationLogEntry
        {
            Handler = "Build",
            Message = result.Success
                ? "Built " + result.Pages + " pages in " + result.Milliseconds + " ms."
                : "Build failed.",
            Kind = result.Success ? "success" : "error",
        });

        if (result.Success)
        {
            Notify("Built " + result.Pages + " pages in " + result.Milliseconds + " ms.", "success");
        }
        else
        {
            Notify("Build failed. See the build log for details.", "error");
        }

        MarkChanged();
    }

    /// <summary>
    /// Resolves the compiled file and visible route for a page preview. Detail
    /// templates preview their first record because the template itself is not
    /// written to the build.
    /// </summary>
    public (string File, string Url, string Address)? PreviewTarget(PageDoc page)
    {
        if (Project is null || BuildResult is not { Success: true } result)
        {
            return null;
        }

        string relative;
        string urlRelative;
        string address;
        var schema = Project.Schemas.FirstOrDefault(candidate => candidate.DetailPageId == page.Id);

        if (schema is not null)
        {
            var entity = Project.EntityList(schema).FirstOrDefault();

            if (entity is null)
            {
                return null;
            }

            var route = Ids.Slug(schema.Plural) + "/" + SiteRenderer.EntitySegment(schema, entity);
            relative = route + (BuildOptions.PrettyUrls ? "/index.html" : ".html");
            urlRelative = route + (BuildOptions.PrettyUrls ? "/" : ".html");
            address = "/" + route + "/";
        }
        else
        {
            relative = BuildOptions.PrettyUrls
                ? page.OutputPath
                : (page.IsHome ? "index.html" : page.Slug.Trim('/') + ".html");
            urlRelative = BuildOptions.PrettyUrls
                ? (page.IsHome ? "" : page.Slug.Trim('/') + "/")
                : (page.IsHome ? "index.html" : page.Slug.Trim('/') + ".html");
            address = page.IsHome ? "/" : "/" + page.Slug.Trim('/') + "/";
        }

        var file = System.IO.Path.Combine(
            result.OutputPath,
            relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(file))
        {
            return null;
        }

        var server = EnsurePreviewServer(result.OutputPath);

        if (server is null)
        {
            return null;
        }

        return (file, server.BaseUrl + urlRelative, address);
    }

    private PreviewServer? EnsurePreviewServer(string root)
    {
        if (_previewServer is not null
            && string.Equals(_previewRoot, root, StringComparison.OrdinalIgnoreCase))
        {
            return _previewServer;
        }

        StopPreviewServer();

        try
        {
            _previewServer = PreviewServer.Start(root);
            _previewRoot = root;
            return _previewServer;
        }
        catch (Exception exception)
        {
            Notify("Couldn't start the preview server: " + exception.Message, "error");
            return null;
        }
    }

    public void StopPreviewServer()
    {
        _previewServer?.Dispose();
        _previewServer = null;
        _previewRoot = "";
    }

    /// <summary>Builds the site and opens the current page in a preview tab.</summary>
    public void PreviewPage(PageDoc page)
    {
        if (Project is null)
        {
            return;
        }

        RunBuild();

        if (PreviewTarget(page) is null)
        {
            Notify(Project.Schemas.Any(schema => schema.DetailPageId == page.Id)
                    ? "Add a record first to preview this entity page."
                    : "That page isn't in the build output yet.", "warning");
            return;
        }

        // Recreate the preview tab so the webview loads the new build.
        Tabs.RemoveAll(tab => tab.Mode == StudioMode.Preview);
        OpenTab(StudioMode.Preview, page.Id, "Preview · " + page.Title, ModeInfo.Icon(StudioMode.Preview));
    }

    public void RegenerateApis()
    {        if (Project is null)
        {
            return;
        }

        try
        {
            Project.GenerateApiDefinitions();
            Notify("API definitions regenerated.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void AddExamplePlugin()
    {
        if (Project is null)
        {
            return;
        }

        try
        {
            Project.AddExamplePlugin();
            Notify("Added the SEO Toolkit plugin.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    // -------------------------------------------------------------- overview

    public List<(string Label, string Detail, string State)> HealthChecks()
    {
        var checks = new List<(string, string, string)>();

        if (Project is null)
        {
            return checks;
        }

        checks.Add(("Pages", Project.Pages.Count + " pages · " + Project.Pages.Count(page => page.ShowInNav) + " in navigation", "good"));
        checks.Add(("Data model", Project.Schemas.Count + " types · " + Project.TotalEntities + " records", Project.Schemas.Count > 0 ? "good" : "hint"));

        if (Project.Handlers.Count == 0)
        {
            checks.Add(("Automation", "No handlers yet", "hint"));
        }
        else
        {
            checks.Add(("Automation", Project.Handlers.Count(handler => handler.Enabled) + " active handlers", "good"));
        }

        if (Project.Plugins.Count == 0)
        {
            checks.Add(("Plugins", "None installed", "hint"));
        }
        else
        {
            checks.Add(("Plugins", Project.Plugins.Count + " plugin" + (Project.Plugins.Count == 1 ? "" : "s"), "good"));
        }

        checks.Add(string.IsNullOrWhiteSpace(Project.Manifest.BaseUrl)
            ? ("Site URL", "Set it before publishing a sitemap", "warning")
            : ("Site URL", Project.Manifest.BaseUrl, "good"));

        checks.Add(("Output", BuildResult?.Success == true
            ? "Last build " + BuildResult.Milliseconds + " ms"
            : "Build/site is ready when you are", "info"));

        return checks;
    }
}
