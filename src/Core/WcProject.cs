using System.Text.Json;

namespace WebCanvasDotNet.Core;

/// <summary>A recoverable project problem with a message safe to show in the UI.</summary>
public sealed class WcException : Exception
{
    public WcException(string message) : base(message)
    {
    }

    public WcException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Owns the on-disk representation of a WebCanvas project: the manifest, page
/// trees, data schemas, entities, theme, automations and plugins.
/// </summary>
public sealed class WcProject
{
    private const string ManifestFile = ".wc";

    public string Root { get; private set; } = "";

    public ProjectManifest Manifest { get; set; } = new();

    public List<PageDoc> Pages { get; } = new();

    public List<BlockDoc> Blocks { get; } = new();

    public List<DataSchema> Schemas { get; } = new();

    public Dictionary<string, List<EntityDoc>> Entities { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ThemeDoc Theme { get; set; } = new();

    public List<HandlerDoc> Handlers { get; } = new();

    public List<PluginInfo> Plugins { get; } = new();

    public List<string> Warnings { get; } = new();

    public string Name => Manifest.Name;

    public string PagesFolder => System.IO.Path.Combine(Root, "app", "design", "pages");

    public string BlocksFolder => System.IO.Path.Combine(Root, "app", "design", "blocks");

    public string SchemasFolder => System.IO.Path.Combine(Root, "app", "data", "schemas");

    public string ThemeFolder => System.IO.Path.Combine(Root, "app", "design", "themes");

    public string ThemeFile => System.IO.Path.Combine(ThemeFolder, "theme.json");

    public string StyleFile => System.IO.Path.Combine(ThemeFolder, "style.css");

    public string AutomationFile => System.IO.Path.Combine(Root, "app", "automation", "handlers.json");

    public string CodeFolder => System.IO.Path.Combine(Root, "app", "code");

    public string ApiFolder => System.IO.Path.Combine(Root, "app", "api");

    public string AssetsFolder => System.IO.Path.Combine(Root, "assets");

    public string BuildFolder => System.IO.Path.Combine(Root, "build", "site");

    public PageDoc? Page(string? id) =>
        Pages.FirstOrDefault(page => string.Equals(page.Id, id, StringComparison.Ordinal));

    public BlockDoc? Block(string? id) =>
        Blocks.FirstOrDefault(block => string.Equals(block.Id, id, StringComparison.Ordinal));

    public PageDoc? HomePage =>
        Pages.FirstOrDefault(page => page.IsHome) ?? Pages.FirstOrDefault();

    public DataSchema? Schema(string? name) =>
        Schemas.FirstOrDefault(schema => string.Equals(schema.Name, name, StringComparison.OrdinalIgnoreCase));

    public List<EntityDoc> EntityList(DataSchema schema)
    {
        if (!Entities.TryGetValue(schema.Name, out var list))
        {
            list = new List<EntityDoc>();
            Entities[schema.Name] = list;
        }

        return list;
    }

    public List<EntityDoc> EntityList(string schemaName) =>
        Schema(schemaName) is { } schema ? EntityList(schema) : new List<EntityDoc>();

    public int CountOf(DataSchema schema) => EntityList(schema).Count;

    public int TotalEntities => Schemas.Sum(CountOf);

    public EntityDoc? Entity(DataSchema schema, string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return EntityList(schema).FirstOrDefault(entity => entity.Id == id);
    }

    public string TitleOf(DataSchema schema, EntityDoc? entity)
    {
        if (entity is null)
        {
            return "";
        }

        if (schema.PrimaryField() is { } primary && !string.IsNullOrWhiteSpace(entity.Get(primary.Name)))
        {
            return entity.Get(primary.Name);
        }

        return entity.Id;
    }

    public string RelationLabel(DataSchema source, DataField field, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "";
        }

        if (field.Target is null || Schema(field.Target) is not { } target)
        {
            return id;
        }

        var entity = Entity(target, id);
        return entity is null ? id : TitleOf(target, entity);
    }

    public List<EntityDoc> SortedEntities(DataSchema schema)
    {
        return EntityList(schema)
            .OrderByDescending(entity => entity.UpdatedAt, StringComparer.Ordinal)
            .ToList();
    }

    public IEnumerable<DataSchema> Referencing(DataSchema target) =>
        Schemas.Where(schema =>
            schema.Relations().Any(field => string.Equals(field.Target, target.Name, StringComparison.OrdinalIgnoreCase)));

    // ---------------------------------------------------------------- loading

    private static readonly HashSet<string> HtmlAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "src", "href", "alt", "title", "loading", "allowfullscreen", "allow", "target", "rel",
        "name", "placeholder", "controls", "poster", "referrerpolicy",
    };

    /// <summary>
    /// Older widget presets stored link and embed attributes in the style map.
    /// Move them back on load so existing projects publish correctly.
    /// </summary>
    private static void MigrateNode(PageNode node)
    {
        if (node.Style is { Count: > 0 })
        {
            var moved = node.Style.Keys.Where(HtmlAttributes.Contains).ToList();

            foreach (var key in moved)
            {
                node.Attrs ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                node.Attrs[key] = node.Style[key];
                node.Style.Remove(key);
            }

            if (node.Style.Count == 0)
            {
                node.Style = null;
            }
        }

        if (node.Children is null)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            MigrateNode(child);
        }
    }

    public static WcProject Open(string folder)
    {
        var root = System.IO.Path.GetFullPath(folder);

        if (!File.Exists(System.IO.Path.Combine(root, ManifestFile)))
        {
            throw new WcException("That folder isn't a WebCanvas project — no .wc file was found.");
        }

        var project = new WcProject { Root = root };
        project.Load();
        return project;
    }

    private void Load()
    {
        Manifest = ReadJson<ProjectManifest>(System.IO.Path.Combine(Root, ManifestFile), new ProjectManifest())
                   ?? new ProjectManifest();
        Manifest.Name = string.IsNullOrWhiteSpace(Manifest.Name) ? new DirectoryInfo(Root).Name : Manifest.Name;

        Pages.Clear();

        if (Directory.Exists(PagesFolder))
        {
            foreach (var file in Directory.GetFiles(PagesFolder, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var page = ReadJson<PageDoc>(file, null);

                if (page is null)
                {
                    continue;
                }

                page.Nodes ??= new List<PageNode>();

                foreach (var node in page.Nodes)
                {
                    MigrateNode(node);
                }

                Pages.Add(page);
            }
        }

        if (Pages.Count == 0)
        {
            var page = new PageDoc { Title = "Home", Slug = "" };
            page.Nodes.Add(PageNode.Create("section", "site-section", null, null, PageNode.Create("h1", null, "Your new site")));
            Pages.Add(page);
        }

        Blocks.Clear();

        if (Directory.Exists(BlocksFolder))
        {
            foreach (var file in Directory.GetFiles(BlocksFolder, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var block = ReadJson<BlockDoc>(file, null);

                if (block is null || string.IsNullOrWhiteSpace(block.Id))
                {
                    continue;
                }

                block.Nodes ??= new List<PageNode>();

                foreach (var node in block.Nodes)
                {
                    MigrateNode(node);
                }

                Blocks.Add(block);
            }
        }

        // A home page is expected by navigation and the builder, so make sure one exists.
        if (Pages.All(page => !page.IsHome))
        {
            Pages[0].Slug = "";
            Pages[0].Title = string.IsNullOrWhiteSpace(Pages[0].Title) ? "Home" : Pages[0].Title;
        }

        Schemas.Clear();

        if (Directory.Exists(SchemasFolder))
        {
            foreach (var file in Directory.GetFiles(SchemasFolder, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var schema = ReadJson<DataSchema>(file, null);

                if (schema is null || string.IsNullOrWhiteSpace(schema.Name))
                {
                    continue;
                }

                schema.Plural = string.IsNullOrWhiteSpace(schema.Plural) ? Ids.Plural(schema.Name) : schema.Plural;
                schema.Fields ??= new List<DataField>();
                Schemas.Add(schema);
            }
        }

        Schemas.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        Entities.Clear();

        foreach (var schema in Schemas)
        {
            var folder = EntityFolder(schema);
            var list = new List<EntityDoc>();

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    var entity = ReadJson<EntityDoc>(file, null);

                    if (entity is null)
                    {
                        continue;
                    }

                    entity.Values ??= new Dictionary<string, string>();
                    list.Add(entity);
                }
            }

            Entities[schema.Name] = list;
        }

        Theme = ReadJson<ThemeDoc>(ThemeFile, new ThemeDoc()) ?? new ThemeDoc();

        var automation = ReadJson<AutomationFile>(AutomationFile, new AutomationFile());
        Handlers.Clear();

        if (automation?.Handlers is not null)
        {
            foreach (var handler in automation.Handlers)
            {
                handler.Conditions ??= new List<ConditionDoc>();
                handler.Actions ??= new List<ActionDoc>();
                Handlers.Add(handler);
            }
        }

        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();

        if (!Directory.Exists(CodeFolder))
        {
            return;
        }

        // app/code/<vendor>/<plugin>/plugin.json, and the flatter app/code/<plugin>/plugin.json.
        var files = Directory.GetFiles(CodeFolder, "plugin.json", SearchOption.AllDirectories);

        foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                var directory = System.IO.Path.GetDirectoryName(file)!;
                var relative = System.IO.Path.GetRelativePath(CodeFolder, directory);
                var parts = relative.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                var plugin = new PluginInfo
                {
                    Name = Text(root, "name") ?? parts.LastOrDefault() ?? "Plugin",
                    Vendor = Text(root, "vendor") ?? (parts.Length > 1 ? parts[0] : "Local"),
                    Version = Text(root, "version") ?? "1.0.0",
                    Description = Text(root, "description") ?? "",
                    Path = directory,
                    Entry = Text(root, "entry") ?? "",
                    HasComponentFolder = Directory.Exists(System.IO.Path.Combine(directory, "Components")),
                    HasScriptFolder = Directory.Exists(System.IO.Path.Combine(directory, "Scripts")),
                    HasStyleFolder = Directory.Exists(System.IO.Path.Combine(directory, "Styles")),
                };

                if (root.TryGetProperty("capabilities", out var capabilities) && capabilities.ValueKind == JsonValueKind.Array)
                {
                    foreach (var capability in capabilities.EnumerateArray())
                    {
                        var label = capability.ValueKind == JsonValueKind.String
                            ? capability.GetString()
                            : Text(capability, "name");

                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            plugin.Capabilities.Add(label);
                        }
                    }
                }

                Plugins.Add(plugin);
            }
            catch (Exception exception)
            {
                Warnings.Add("Couldn't read plugin manifest " + System.IO.Path.GetFileName(file) + ": " + exception.Message);
            }
        }
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private T? ReadJson<T>(string path, T? fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), WcJson.Options) ?? fallback;
        }
        catch (Exception exception)
        {
            Warnings.Add("Couldn't read " + System.IO.Path.GetRelativePath(Root, path) + ": " + exception.Message);
            return fallback;
        }
    }

    // ---------------------------------------------------------------- creation

    public static WcProject Create(string parentFolder, string name, bool starter)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new WcException("Give the project a name first.");
        }

        if (string.IsNullOrWhiteSpace(parentFolder))
        {
            throw new WcException("Choose a folder to create the project in.");
        }

        var folderName = SanitizeFolder(name);
        var root = System.IO.Path.Combine(System.IO.Path.GetFullPath(parentFolder), folderName);

        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            throw new WcException("That folder already exists and isn't empty. Choose another name or location.");
        }

        Directory.CreateDirectory(root);

        var project = new WcProject
        {
            Root = root,
            Manifest = new ProjectManifest
            {
                Name = name.Trim(),
                Created = Ids.Now(),
                Template = starter ? "starter" : "blank",
                Description = starter
                    ? "An editorial studio site with journal posts, authors and case studies."
                    : "A new WebCanvas site.",
            },
        };

        foreach (var folder in project.RequiredFolders())
        {
            Directory.CreateDirectory(folder);
        }

        if (starter)
        {
            StarterContent.Populate(project);
        }
        else
        {
            StarterContent.PopulateBlank(project);
        }

        project.SaveEverything();
        project.GenerateApiDefinitions();
        return project;
    }

    public static string SanitizeFolder(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Trim().Where(ch => !invalid.Contains(ch)).ToArray()).Trim(' ', '.');
        return string.IsNullOrWhiteSpace(cleaned) ? "WebCanvas site" : cleaned;
    }

    private IEnumerable<string> RequiredFolders()
    {
        yield return PagesFolder;
        yield return BlocksFolder;
        yield return SchemasFolder;
        yield return ThemeFolder;
        yield return System.IO.Path.Combine(Root, "app", "automation");
        yield return System.IO.Path.Combine(Root, "app", "api");
        yield return CodeFolder;
        yield return AssetsFolder;
        yield return BuildFolder;
    }

    // ---------------------------------------------------------------- saving

    public void SaveEverything()
    {
        try
        {
            Directory.CreateDirectory(Root);
            WriteJson(System.IO.Path.Combine(Root, ManifestFile), Manifest);
            SaveTheme();
            SaveHandlers();

            foreach (var page in Pages)
            {
                SavePage(page);
            }

            foreach (var block in Blocks)
            {
                SaveBlock(block);
            }

            foreach (var schema in Schemas)
            {
                SaveSchema(schema);

                foreach (var entity in EntityList(schema))
                {
                    SaveEntity(schema, entity);
                }
            }
        }
        catch (Exception exception) when (exception is not WcException)
        {
            throw new WcException("Couldn't save the project: " + exception.Message, exception);
        }
    }

    public void SaveManifest()
    {
        try
        {
            WriteJson(System.IO.Path.Combine(Root, ManifestFile), Manifest);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the project: " + exception.Message, exception);
        }
    }

    public void SavePage(PageDoc page)
    {
        try
        {
            Directory.CreateDirectory(PagesFolder);
            WriteJson(System.IO.Path.Combine(PagesFolder, Ids.Slug(page.Id) + ".json"), page);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the page: " + exception.Message, exception);
        }
    }

    public void DeletePage(PageDoc page)
    {
        var path = System.IO.Path.Combine(PagesFolder, Ids.Slug(page.Id) + ".json");

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Pages.Remove(page);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't delete the page: " + exception.Message, exception);
        }
    }

    public void SaveBlock(BlockDoc block)
    {
        try
        {
            Directory.CreateDirectory(BlocksFolder);
            WriteJson(System.IO.Path.Combine(BlocksFolder, Ids.Slug(block.Id) + ".json"), block);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the block: " + exception.Message, exception);
        }
    }

    public void DeleteBlock(BlockDoc block)
    {
        try
        {
            var path = System.IO.Path.Combine(BlocksFolder, Ids.Slug(block.Id) + ".json");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Blocks.Remove(block);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't delete the block: " + exception.Message, exception);
        }
    }

    public void SaveSchema(DataSchema schema)
    {
        try
        {
            Directory.CreateDirectory(SchemasFolder);
            WriteJson(System.IO.Path.Combine(SchemasFolder, Ids.Slug(schema.Name) + ".json"), schema);
            Directory.CreateDirectory(EntityFolder(schema));
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the data type: " + exception.Message, exception);
        }
    }

    public void SaveEntity(DataSchema schema, EntityDoc entity)
    {
        try
        {
            entity.UpdatedAt = Ids.Now();
            var folder = EntityFolder(schema);
            Directory.CreateDirectory(folder);
            WriteJson(System.IO.Path.Combine(folder, Ids.Slug(entity.Id) + ".json"), entity);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the record: " + exception.Message, exception);
        }
    }

    public void DeleteEntity(DataSchema schema, EntityDoc entity)
    {
        try
        {
            var path = System.IO.Path.Combine(EntityFolder(schema), Ids.Slug(entity.Id) + ".json");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            EntityList(schema).Remove(entity);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't delete the record: " + exception.Message, exception);
        }
    }

    public string EntityFolder(DataSchema schema) =>
        System.IO.Path.Combine(Root, "app", "data", Ids.Slug(schema.Plural), "entities");

    public void SaveTheme()
    {
        try
        {
            Directory.CreateDirectory(ThemeFolder);
            WriteJson(ThemeFile, Theme);
            var css = ":root{" + Theme.CssVariables() + "}\n";
            File.WriteAllText(StyleFile, css);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save the theme: " + exception.Message, exception);
        }
    }

    public void SaveHandlers()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AutomationFile)!);
            WriteJson(AutomationFile, new AutomationFile { Handlers = Handlers });
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't save automations: " + exception.Message, exception);
        }
    }

    public void GenerateApiDefinitions()
    {
        try
        {
            Directory.CreateDirectory(ApiFolder);

            var endpoints = new List<object>();

            foreach (var schema in Schemas.Where(schema => schema.Public))
            {
                endpoints.Add(new
                {
                    method = "GET",
                    path = "/data/" + Ids.Slug(schema.Plural) + "/index.json",
                    type = schema.Name,
                    description = "All published " + schema.Plural + ".",
                });

                endpoints.Add(new
                {
                    method = "GET",
                    path = "/data/" + Ids.Slug(schema.Plural) + "/{id}.json",
                    type = schema.Name,
                    description = "A single " + schema.Name + " record.",
                });
            }

            foreach (var page in Pages)
            {
                endpoints.Add(new
                {
                    method = "GET",
                    path = "/" + page.OutputPath.Replace("index.html", ""),
                    type = "Page",
                    description = page.Title,
                });
            }

            var definition = new
            {
                version = 1,
                generatedAt = Ids.Now(),
                site = new
                {
                    name = Manifest.Name,
                    baseUrl = Manifest.BaseUrl,
                    theme = Theme.Name,
                },
                endpoints,
            };

            WriteJson(System.IO.Path.Combine(ApiFolder, "index.json"), definition);
        }
        catch (Exception exception)
        {
            throw new WcException("Couldn't write the API definitions: " + exception.Message, exception);
        }
    }

    public void AddExamplePlugin()
    {
        var directory = System.IO.Path.Combine(CodeFolder, "studio", "seo-toolkit");
        Directory.CreateDirectory(System.IO.Path.Combine(directory, "Scripts"));
        Directory.CreateDirectory(System.IO.Path.Combine(directory, "Styles"));
        Directory.CreateDirectory(System.IO.Path.Combine(directory, "Components"));

        var manifest = new
        {
            name = "SEO Toolkit",
            vendor = "Studio",
            version = "1.0.0",
            description = "Meta tags, sitemap and social cards for every published page.",
            entry = "Scripts/seo.js",
            capabilities = new[] { "meta-tags", "sitemap", "social-cards" },
        };

        WriteJson(System.IO.Path.Combine(directory, "plugin.json"), manifest);
        File.WriteAllText(
            System.IO.Path.Combine(directory, "Scripts", "seo.js"),
            "// Runs after each build. Reads page metadata and writes social cards.\nexport function afterBuild(site) {\n    return site.pages.map(page => ({ url: page.url, title: page.title }));\n}\n");
        File.WriteAllText(
            System.IO.Path.Combine(directory, "Styles", "seo.css"),
            "/* Plugin styles are appended after the site theme. */\n");
        LoadPlugins();
    }

    // ---------------------------------------------------------------- helpers

    private static void WriteJson<T>(string path, T value)
    {
        var directory = System.IO.Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(value, WcJson.Options));
    }
}
