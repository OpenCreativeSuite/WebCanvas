namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public string EntitySearch { get; set; } = "";

    public string EntityFilter { get; set; } = "all";

    public string SchemaSearch { get; set; } = "";

    public void SetEntityFilter(string filter)
    {
        EntityFilter = filter;
        MarkChanged();
    }

    public List<(string Value, string Label)> EntityFilters(DataSchema schema)
    {
        var filters = new List<(string, string)> { ("all", "All") };

        if (schema.Field("featured") is not null)
        {
            filters.Add(("featured", "Featured"));
            filters.Add(("unfeatured", "Not featured"));
        }

        if (schema.Field("publishedAt") is not null)
        {
            filters.Add(("published", "Published"));
            filters.Add(("draft", "Drafts"));
        }

        return filters;
    }

    /// <summary>True while Content is showing the relationship map.</summary>
    public bool ContentMapOpen { get; private set; } = true;

    public void OpenContentMap()
    {
        ContentMapOpen = true;
        ActiveMode = StudioMode.Content;
        OpenTab(StudioMode.Content, "map", "Map", "fa-diagram-project");
    }

    public void OpenContentRecords()
    {
        ContentMapOpen = false;
        ActiveMode = StudioMode.Content;

        if (CurrentSchema is { } schema)
        {
            OpenTab(StudioMode.Content, schema.Name, schema.Name, "fa-file");
        }
        else
        {
            OpenTab(StudioMode.Content, "records", "Content", "fa-file");
        }
    }

    public string? SelectedFieldName { get; set; }

    public bool AddFieldMenuOpen { get; set; }

    public DataField? CurrentField =>
        CurrentSchema?.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, SelectedFieldName, StringComparison.OrdinalIgnoreCase));

    public void SelectField(string? name)
    {
        SelectedFieldName = name;
        MarkChanged();
    }

    public void ToggleAddFieldMenu()
    {
        AddFieldMenuOpen = !AddFieldMenuOpen;
        MarkChanged();
    }

    public List<EntityDoc> FilteredEntities(DataSchema schema)
    {
        var all = Project?.SortedEntities(schema) ?? new List<EntityDoc>();
        var query = EntitySearch.Trim();

        if (EntityFilter == "featured")
        {
            all = all.Where(entity => entity.Flag("featured")).ToList();
        }
        else if (EntityFilter == "unfeatured")
        {
            all = all.Where(entity => !entity.Flag("featured")).ToList();
        }
        else if (EntityFilter == "published")
        {
            all = all.Where(entity => !string.IsNullOrWhiteSpace(entity.Get("publishedAt"))).ToList();
        }
        else if (EntityFilter == "draft")
        {
            all = all.Where(entity => string.IsNullOrWhiteSpace(entity.Get("publishedAt"))).ToList();
        }

        if (query.Length == 0)
        {
            return all;
        }

        return all.Where(entity =>
        {
            if (entity.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Project is not null && Project.TitleOf(schema, entity).Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return entity.Values.Values.Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }

    public string EntityBadge(DataSchema schema, EntityDoc entity)
    {
        if (schema.Field("featured") is not null && entity.Flag("featured"))
        {
            return "Featured";
        }

        if (schema.Field("publishedAt") is not null && !string.IsNullOrWhiteSpace(entity.Get("publishedAt")))
        {
            return "Published";
        }

        if (schema.Field("status") is { } status && !string.IsNullOrWhiteSpace(entity.Get(status.Name)))
        {
            return entity.Get(status.Name);
        }

        return "Draft";
    }

    public string EntitySubtitle(DataSchema schema, EntityDoc entity)
    {
        foreach (var field in schema.Fields)
        {
            if (field.Name is "title" or "name")
            {
                continue;
            }

            var value = entity.Get(field.Name);

            if (!string.IsNullOrWhiteSpace(value) && field.Type is FieldTypes.Text or FieldTypes.LongText)
            {
                return value.Length > 90 ? value[..90] + "…" : value;
            }
        }

        return "Updated " + Ids.Relative(entity.UpdatedAt);
    }

    public void NewEntity(DataSchema? schema = null)
    {
        schema ??= CurrentSchema;

        if (schema is null || Project is null)
        {
            Notify("Create a data type before adding records.", "warning");
            return;
        }

        var entity = new EntityDoc();
        var primary = schema.PrimaryField();

        if (primary is not null)
        {
            entity.Values[primary.Name] = "Untitled " + schema.Name;
        }

        try
        {
            Project.EntityList(schema).Add(entity);
            Project.SaveEntity(schema, entity);
            SelectedSchemaName = schema.Name;
            SelectedEntityId = entity.Id;
            EntitySearch = "";
            ContentMapOpen = false;
            OpenTab(StudioMode.Content, schema.Name, schema.Name, "fa-file");
            Notify("Added a " + schema.Name + ".", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void SetEntityValue(DataSchema schema, EntityDoc entity, string field, string? value)
    {
        try
        {
            entity.Set(field, value);
            Project!.SaveEntity(schema, entity);
            SaveState = "Saved just now";
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void DuplicateEntity(DataSchema schema, EntityDoc entity)
    {
        if (Project is null)
        {
            return;
        }

        var copy = new EntityDoc
        {
            Values = new Dictionary<string, string>(entity.Values),
        };

        var primary = schema.PrimaryField();

        if (primary is not null)
        {
            copy.Values[primary.Name] = entity.Get(primary.Name) + " copy";
        }

        try
        {
            Project.EntityList(schema).Add(copy);
            Project.SaveEntity(schema, copy);
            SelectedEntityId = copy.Id;
            Notify("Record duplicated.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void DeleteEntity(DataSchema schema, EntityDoc entity)
    {
        var title = Project?.TitleOf(schema, entity) ?? entity.Id;

        AskConfirm(new ConfirmRequest
        {
            Title = "Delete “" + title + "”?",
            Message = "The record is removed from the project. This cannot be undone.",
            Confirm = "Delete record",
            OnConfirm = () =>
            {
                try
                {
                    Project!.DeleteEntity(schema, entity);

                    if (SelectedEntityId == entity.Id)
                    {
                        SelectedEntityId = null;
                    }

                    Notify("Record deleted.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    // ---------------------------------------------------------------- schemas

    public void NewSchema()
    {
        OpenPrompt(new PromptRequest
        {
            Title = "New data type",
            Label = "Type name",
            Value = "Article",
            Placeholder = "Article",
            Confirm = "Create type",
            OnConfirm = value =>
            {
                if (Project is null)
                {
                    return;
                }

                var name = Identifier(value, "Article");

                if (Project.Schema(name) is not null)
                {
                    Notify("A data type called “" + name + "” already exists.", "warning");
                    return;
                }

                var schema = new DataSchema
                {
                    Name = name,
                    Plural = Ids.Plural(name.ToLowerInvariant()),
                    Icon = "fa-cube",
                    Description = "",
                    Fields =
                    {
                        new DataField { Name = "title", Type = FieldTypes.Text, Required = true },
                        new DataField { Name = "summary", Type = FieldTypes.LongText },
                        new DataField { Name = "body", Type = FieldTypes.RichText },
                        new DataField { Name = "publishedAt", Type = FieldTypes.Date },
                    },
                };

                try
                {
                    Project.Schemas.Add(schema);
                    Project.Schemas.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
                    Project.SaveSchema(schema);
                    Project.Entities[schema.Name] = new List<EntityDoc>();
                    SelectedSchemaName = schema.Name;
                    SelectedEntityId = null;
                    OpenTab(StudioMode.Data, schema.Name, schema.Name, "fa-database");
                    Notify("Created the " + schema.Name + " type.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void RenameSchema(DataSchema schema)
    {
        OpenPrompt(new PromptRequest
        {
            Title = "Rename data type",
            Label = "Type name",
            Value = schema.Name,
            Confirm = "Rename",
            OnConfirm = value =>
            {
                if (Project is null)
                {
                    return;
                }

                var name = Identifier(value, schema.Name);

                if (name == schema.Name)
                {
                    return;
                }

                if (Project.Schema(name) is not null)
                {
                    Notify("Another type already uses that name.", "warning");
                    return;
                }

                var oldName = schema.Name;
                var oldPath = System.IO.Path.Combine(Project.SchemasFolder, Ids.Slug(oldName) + ".json");

                try
                {
                    schema.Name = name;

                    if (Project.Entities.Remove(oldName, out var entities))
                    {
                        Project.Entities[name] = entities;
                    }

                    foreach (var other in Project.Schemas)
                    {
                        foreach (var field in other.Relations())
                        {
                            if (string.Equals(field.Target, oldName, StringComparison.OrdinalIgnoreCase))
                            {
                                field.Target = name;
                            }
                        }

                        Project.SaveSchema(other);
                    }

                    Project.SaveSchema(schema);

                    if (File.Exists(oldPath) && !oldPath.Equals(
                            System.IO.Path.Combine(Project.SchemasFolder, Ids.Slug(name) + ".json"),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(oldPath);
                    }

                    SelectedSchemaName = name;

                    foreach (var tab in Tabs.Where(tab =>
                                 (tab.Mode == StudioMode.Content || tab.Mode == StudioMode.Data)
                                 && tab.ItemId == oldName))
                    {
                        tab.ItemId = name;
                        tab.Title = name;
                    }

                    Notify("Renamed the type to " + name + ".", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void DeleteSchema(DataSchema schema)
    {
        if (Project is null)
        {
            return;
        }

        var count = Project.CountOf(schema);

        AskConfirm(new ConfirmRequest
        {
            Title = "Delete the " + schema.Name + " type?",
            Message = count == 0
                ? "The schema file is removed. No records will be deleted."
                : count + " record" + (count == 1 ? "" : "s") + " will become hidden. The entity folder is kept on disk so nothing is lost.",
            Confirm = "Delete type",
            OnConfirm = () =>
            {
                try
                {
                    var path = System.IO.Path.Combine(Project.SchemasFolder, Ids.Slug(schema.Name) + ".json");

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    Project.Schemas.Remove(schema);
                    Project.Entities.Remove(schema.Name);

                    foreach (var other in Project.Schemas)
                    {
                        foreach (var field in other.Relations())
                        {
                            if (string.Equals(field.Target, schema.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                field.Target = null;
                            }
                        }

                        Project.SaveSchema(other);
                    }

                    Tabs.RemoveAll(tab =>
                        (tab.Mode == StudioMode.Content || tab.Mode == StudioMode.Data)
                        && tab.ItemId == schema.Name);

                    if (SelectedSchemaName == schema.Name)
                    {
                        SelectedSchemaName = Project.Schemas.FirstOrDefault()?.Name;
                        SelectedEntityId = null;
                    }

                    if (ActiveTabId is null)
                    {
                        OpenMode(StudioMode.Data);
                    }

                    Notify("Type deleted. Its records remain in app/data.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void SetSchemaMeta(DataSchema schema, Action<DataSchema> edit)
    {
        try
        {
            edit(schema);
            Project!.SaveSchema(schema);
            SyncTabTitle(StudioMode.Data, schema.Name, schema.Name, "fa-database");
            SyncTabTitle(StudioMode.Content, schema.Name, schema.Name, "fa-file");
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void SetSchemaPublic(DataSchema schema, bool isPublic)
    {
        try
        {
            schema.Public = isPublic;
            Project!.SaveSchema(schema);
            Project.GenerateApiDefinitions();
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    /// <summary>
    /// Creates a page that uses <c>{{field}}</c> tokens, assigns it as the
    /// record template for the type and opens it for editing.
    /// </summary>
    public void CreateDetailPage(DataSchema schema)
    {
        if (Project is null)
        {
            return;
        }

        var title = UniquePageTitle(schema.Name + " detail");
        var lead = DetailFieldToken(schema, "summary", "excerpt", "description", "intro", "lead")
                   ?? DetailFieldTokenOfType(schema, FieldTypes.RichText, FieldTypes.LongText)
                   ?? "{{title}}";
        var body = DetailFieldToken(schema, "body", "content", "details", "story")
                   ?? DetailFieldTokenOfType(schema, FieldTypes.RichText)
                   ?? DetailFieldTokenOfType(schema, FieldTypes.LongText)
                   ?? "";

        var page = new PageDoc
        {
            Title = title,
            Slug = UniqueSlug("templates/" + Ids.Slug(schema.Plural)),
            Description = "Template for " + schema.Name + " records.",
            ShowInNav = false,
        };

        var header = Project.Blocks.FirstOrDefault(block =>
            block.Kind.Equals("theme", StringComparison.OrdinalIgnoreCase)
            && (block.Category.Equals("Navigation", StringComparison.OrdinalIgnoreCase)
                || block.Name.Contains("header", StringComparison.OrdinalIgnoreCase)));

        var footer = Project.Blocks.FirstOrDefault(block =>
            block.Kind.Equals("theme", StringComparison.OrdinalIgnoreCase)
            && (block.Name.Contains("footer", StringComparison.OrdinalIgnoreCase)
                || block.Category.Equals("Footer", StringComparison.OrdinalIgnoreCase)));

        if (header is not null)
        {
            page.Nodes.Add(BlockReference(header));
        }

        page.Nodes.Add(PageNode.Create("section", "page-head site-shell", null, null,
            PageNode.Create("span", "eyebrow", schema.Name),
            PageNode.Create("h1", "page-title", "{{title}}"),
            PageNode.Create("p", "page-lead", lead)));

        page.Nodes.Add(PageNode.Create("section", "site-section site-shell", null, null,
            PageNode.Create("article", "article-body", null, null,
                PageNode.Create("p", null, body))));

        if (footer is not null)
        {
            page.Nodes.Add(BlockReference(footer));
        }

        try
        {
            Project.Pages.Add(page);
            Project.SavePage(page);
            schema.DetailPageId = page.Id;
            Project.SaveSchema(schema);
            SelectedPageId = page.Id;
            SelectedNodeId = null;
            OpenTab(StudioMode.Design, page.Id, page.Title, "fa-shapes");
            Notify("Created the “" + schema.Name + "” page template.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    private static string? DetailFieldToken(DataSchema schema, params string[] names)
    {
        foreach (var name in names)
        {
            if (schema.Field(name) is { } field)
            {
                return "{{" + field.Name + "}}";
            }
        }

        return null;
    }

    private static string? DetailFieldTokenOfType(DataSchema schema, params string[] types)
    {
        var field = schema.Fields.FirstOrDefault(candidate => types.Contains(candidate.Type));
        return field is null ? null : "{{" + field.Name + "}}";
    }

    private static PageNode BlockReference(BlockDoc block)
    {
        var node = PageNode.Create("block");
        node.Attrs = new Dictionary<string, string> { ["block"] = block.Id };
        return node;
    }

    /// <summary>
    /// Appends a collection section for a type to the current page, then opens
    /// the design canvas with the new collection selected.
    /// </summary>
    public void ShowSchemaOnSite(DataSchema schema)
    {
        if (Project is null)
        {
            return;
        }

        PageDoc? page = Project.Page(SelectedPageId) ?? Project.HomePage;

        if (page is null)
        {
            Notify("Create a page before adding a collection.", "warning");
            return;
        }

        var collection = PageNode.Create("collection", "collection-grid");
        collection.Attrs = new Dictionary<string, string>
        {
            ["type"] = schema.Name,
            ["limit"] = "3",
        };

        var section = PageNode.Create("section", "site-section site-shell", null, null,
            PageNode.Create("div", "section-head", null, null,
                PageNode.Create("div", null, null, null,
                    PageNode.Create("span", "section-kicker", "Explore"),
                    PageNode.Create("h2", "section-title", "Latest " + schema.Plural + "."))),
            collection);

        Capture(page, "Show " + schema.Name + " on site");

        PageNode? main = page.Walk().FirstOrDefault(node => node.Tag.Equals("main", StringComparison.OrdinalIgnoreCase));
        List<PageNode> container = main?.Children ?? page.Nodes;
        int footer = container.FindIndex(node => IsFooterNode(node, Project));
        container.Insert(footer >= 0 ? footer : container.Count, section);

        schema.Public = true;
        Project.SaveSchema(schema);
        SelectedPageId = page.Id;
        SelectedNodeId = collection.Id;
        ContentMapOpen = false;
        OpenTab(StudioMode.Design, page.Id, page.Title, "fa-shapes");
        SaveCurrentPage(page, "Added " + schema.Name + " collection");
        Notify("Added a " + schema.Name + " collection to “" + page.Title + "”.", "success");
    }

    // ----------------------------------------------------------------- fields

    public void AddField(DataSchema schema, string type = FieldTypes.Text)
    {
        var baseName = type switch
        {
            FieldTypes.Relation => "related",
            FieldTypes.Boolean => "flag",
            FieldTypes.Date => "date",
            FieldTypes.Image => "image",
            FieldTypes.Number => "number",
            _ => "field",
        };

        var name = baseName;
        var index = 2;

        while (schema.Field(name) is not null)
        {
            name = baseName + index++;
        }

        var field = new DataField { Name = name, Type = type };

        if (type == FieldTypes.Relation)
        {
            field.Target = Project?.Schemas.FirstOrDefault(candidate => candidate.Name != schema.Name)?.Name;
        }

        if (type == FieldTypes.Select)
        {
            field.Options = new List<string> { "Option one", "Option two" };
        }

        try
        {
            schema.Fields.Add(field);
            Project!.SaveSchema(schema);
            SelectedFieldName = field.Name;
            AddFieldMenuOpen = false;
            Notify("Added the " + field.Display + " field.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void RenameField(DataSchema schema, DataField field, string name)
    {
        if (Project is null)
        {
            return;
        }

        var cleaned = Identifier(name, field.Name);

        if (cleaned == field.Name)
        {
            return;
        }

        if (schema.Field(cleaned) is not null)
        {
            Notify("That field name is already used.", "warning");
            return;
        }

        try
        {
            var old = field.Name;

            foreach (var entity in Project.EntityList(schema))
            {
                if (entity.Values.Remove(old, out var value))
                {
                    entity.Values[cleaned] = value;
                    Project.SaveEntity(schema, entity);
                }
            }

            field.Name = cleaned;
            Project.SaveSchema(schema);
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void SetFieldType(DataSchema schema, DataField field, string type)
    {
        try
        {
            field.Type = type;

            if (type == FieldTypes.Relation && string.IsNullOrWhiteSpace(field.Target))
            {
                field.Target = Project?.Schemas.FirstOrDefault(candidate => candidate.Name != schema.Name)?.Name;
            }

            if (type == FieldTypes.Select && (field.Options is null || field.Options.Count == 0))
            {
                field.Options = new List<string> { "Option one", "Option two" };
            }

            Project!.SaveSchema(schema);
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void SetFieldMeta(DataSchema schema, DataField field, Action<DataField> edit)
    {
        try
        {
            edit(field);
            Project!.SaveSchema(schema);
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void MoveField(DataSchema schema, DataField field, int direction)
    {
        var index = schema.Fields.IndexOf(field);
        var target = index + direction;

        if (index < 0 || target < 0 || target >= schema.Fields.Count)
        {
            return;
        }

        try
        {
            (schema.Fields[index], schema.Fields[target]) = (schema.Fields[target], schema.Fields[index]);
            Project!.SaveSchema(schema);
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void DeleteField(DataSchema schema, DataField field)
    {
        AskConfirm(new ConfirmRequest
        {
            Title = "Delete the “" + field.Display + "” field?",
            Message = "Existing values stay in the record files but will no longer be shown or published.",
            Confirm = "Delete field",
            OnConfirm = () =>
            {
                try
                {
                    schema.Fields.Remove(field);
                    Project!.SaveSchema(schema);
                    Notify("Field removed.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public List<DataSchema> RelationTargets(DataSchema schema) =>
        Project?.Schemas.Where(candidate => candidate.Name != schema.Name).ToList() ?? new List<DataSchema>();

    public List<DataSchema> IncomingRelations(DataSchema schema) =>
        Project?.Referencing(schema).ToList() ?? new List<DataSchema>();

    private static bool IsFooterNode(PageNode node, WcProject project)
    {
        if (node.Tag.Equals("footer", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return node.Tag == "block"
               && node.Attrs is not null
               && node.Attrs.TryGetValue("block", out string? id)
               && project.Block(id) is { } block
               && block.Kind == "theme"
               && block.Category.Contains("Footer", StringComparison.OrdinalIgnoreCase);
    }

    private static string Identifier(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var buffer = new System.Text.StringBuilder();
        var upper = false;

        foreach (var ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer.Append(buffer.Length == 0 ? char.ToLowerInvariant(ch) : upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            else
            {
                upper = true;
            }
        }

        if (buffer.Length == 0)
        {
            return fallback;
        }

        if (char.IsDigit(buffer[0]))
        {
            buffer.Insert(0, 'f');
        }

        return buffer.ToString();
    }
}
