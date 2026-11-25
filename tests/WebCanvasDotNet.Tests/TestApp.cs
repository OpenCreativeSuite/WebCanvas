using Donkeh.Testing;
using Donkeh.UILab;
using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;
using static Donkeh.UILab._;

namespace WebCanvasDotNet.Tests;

/// <summary>
/// A fresh on-disk project and isolated <see cref="StudioSession"/> for one test.
/// Preferences never touch the developer's real app data.
/// </summary>
public sealed class TestApp : IDisposable
{
    public TestApp(bool starter = false)
    {
        Root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "WebCanvasDotNet.Tests",
            Guid.NewGuid().ToString("n"));

        Directory.CreateDirectory(Root);
        Session = new StudioSession(System.IO.Path.Combine(Root, "state"));
        Session.SetProject(WcProject.Create(Root, "Test project", starter));
    }

    public string Root { get; }

    public StudioSession Session { get; }

    public WcProject Project => Session.Project!;

    public ComponentTestHarness Render<T>(Action<ComponentTestOptions>? configure = null)
        where T : Component, new()
    {
        Node tree = Provider(SessionContext.Value, Session, C<T>());
        return ComponentTest.Render(tree, configure);
    }

    public ComponentTestHarness Render<T, TProps>(
        TProps props,
        Action<ComponentTestOptions>? configure = null)
        where T : Component<TProps>, new()
    {
        Node tree = Provider(SessionContext.Value, Session, C<T, TProps>(props));
        return ComponentTest.Render(tree, configure);
    }

    public PageDoc AddPage(string title, params PageNode[] nodes)
    {
        var page = new PageDoc
        {
            Title = title,
            Slug = Ids.Slug(title),
            Nodes = nodes.ToList(),
        };

        Project.Pages.Add(page);
        Project.SavePage(page);
        return page;
    }

    public DataSchema AddSchema(string name = "Article", params DataField[] fields)
    {
        var schema = new DataSchema
        {
            Name = name,
            Plural = Ids.Plural(name),
            Fields = fields.Length == 0
                ? new List<DataField>
                {
                    new() { Name = "title", Type = FieldTypes.Text, Required = true },
                    new() { Name = "slug", Type = FieldTypes.Text },
                    new() { Name = "body", Type = FieldTypes.RichText },
                }
                : fields.ToList(),
        };

        Project.Schemas.Add(schema);
        Project.SaveSchema(schema);
        Session.SelectSchema(schema.Name);
        return schema;
    }

    public EntityDoc AddEntity(DataSchema schema, params (string Field, string Value)[] values)
    {
        var entity = new EntityDoc();

        foreach (var (field, value) in values)
        {
            entity.Set(field, value);
        }

        Project.EntityList(schema).Add(entity);
        Project.SaveEntity(schema, entity);
        return entity;
    }

    public void Dispose()
    {
        Session.StopPreviewServer();

        try
        {
            Directory.Delete(Root, true);
        }
        catch
        {
            // Test cleanup should never mask an assertion.
        }
    }
}
