namespace WebCanvasDotNet.Tests.Structure;

public sealed class WorkflowTests
{
    [Fact]
    public void WorkflowsDoNotReferenceTheOldProjectName()
    {
        foreach (string file in WorkflowFiles())
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("PixelDotNet", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pixeldotnet", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TestsWorkflowRunsTheWebCanvasTestProject()
    {
        string file = Path.Combine(Repository.Root(), ".github", "workflows", "tests.yml");
        string text = File.ReadAllText(file);

        Assert.Contains("WebCanvasDotNet.Tests", text);
        Assert.Contains("dotnet test", text);
        Assert.Contains("csx build", text);
    }

    [Fact]
    public void TestsWorkflowChecksOutTheDonkehFramework()
    {
        string text = File.ReadAllText(Path.Combine(Repository.Root(), ".github", "workflows", "tests.yml"));

        Assert.Contains("donkeh-ui", text);
        Assert.Contains("UI Framework", text);
    }

    [Fact]
    public void ReleaseWorkflowPublishesTheNewestTagAsAFolderBuild()
    {
        string text = File.ReadAllText(Path.Combine(Repository.Root(), ".github", "workflows", "release.yml"));

        Assert.Contains("branches:", text);
        Assert.Contains("master", text);
        Assert.Contains("git describe --tags --abbrev=0", text);
        Assert.Contains("PublishSingleFile=false", text);
        Assert.Contains("make_latest: true", text);
        Assert.Contains("overwrite_files: true", text);
        Assert.DoesNotContain("PublishSingleFile=true", text);
    }

    private static IEnumerable<string> WorkflowFiles() =>
        Directory.GetFiles(Path.Combine(Repository.Root(), ".github", "workflows"), "*.yml");
}
