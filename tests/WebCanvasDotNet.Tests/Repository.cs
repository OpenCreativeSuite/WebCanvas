namespace WebCanvasDotNet.Tests;

public static class Repository
{
    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WebCanvasDotNet.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the WebCanvasDotNet repository root.");
    }
}
