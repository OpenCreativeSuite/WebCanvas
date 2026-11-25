using System.Reflection;
using System.Text.RegularExpressions;
using Donkeh.UILab;
using WebCanvasDotNet.Components;

namespace WebCanvasDotNet.Tests.Structure;

public sealed class ComponentStructureTests
{
    [Fact]
    public void EveryComponentOwnsItsCxCsAndDocs()
    {
        string root = Repository.Root();
        var components = typeof(ActivityRail).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.Namespace == "WebCanvasDotNet.Components"
                           && typeof(Component).IsAssignableFrom(type))
            .OrderBy(type => type.Name)
            .ToList();

        Assert.NotEmpty(components);

        foreach (var type in components)
        {
            string directory = Path.Combine(root, "src", "Components", type.Name);
            Assert.True(Directory.Exists(directory), type.Name + " needs a component directory");
            Assert.True(File.Exists(Path.Combine(directory, type.Name + ".cx")), type.Name + ".cx is missing");
            Assert.True(File.Exists(Path.Combine(directory, type.Name + ".css")), type.Name + ".css is missing");
            Assert.True(File.Exists(Path.Combine(directory, type.Name + ".md")), type.Name + ".md is missing");
        }
    }

    [Fact]
    public void EveryComponentHasATestClass()
    {
        string tests = Path.Combine(Repository.Root(), "tests", "WebCanvasDotNet.Tests", "Components");
        var components = typeof(ActivityRail).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.Namespace == "WebCanvasDotNet.Components"
                           && typeof(Component).IsAssignableFrom(type));

        foreach (var type in components)
        {
            Assert.True(
                File.Exists(Path.Combine(tests, type.Name + "Tests.cs")),
                type.Name + " needs a component test class");
        }
    }

    [Fact]
    public void EveryCxDeclaresExactlyOneComponent()
    {
        string componentsRoot = Path.Combine(Repository.Root(), "src", "Components");

        foreach (string file in Directory.GetFiles(componentsRoot, "*.cx", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "Ui.cx")
            {
                continue;
            }

            string source = File.ReadAllText(file);
            int count = Regex.Matches(
                source,
                @"class\s+\w+[^\r\n{]*:\s*Component(?:<|\b)").Count;

            Assert.True(count == 1, Path.GetFileName(file) + " should declare exactly one Component, found " + count);
        }
    }

    [Fact]
    public void EveryComponentCssIsRegisteredWithTheStylesheetLoader()
    {
        string root = Repository.Root();
        string registrations = File.ReadAllText(Path.Combine(root, "src", "Stylesheets.cs"));

        foreach (string file in Directory.GetFiles(Path.Combine(root, "src", "Components"), "*.css", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            if (relative.EndsWith("SessionContext.css", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains("[assembly: Donkeh.UILab.Stylesheet(\"" + relative + "\")]", registrations);
        }
    }
}

