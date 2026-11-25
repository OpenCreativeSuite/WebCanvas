using System.Net;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Core;

public sealed class PreviewServerTests
{
    [Fact]
    public async Task ServesTheBuildFolderWithAReferrerPolicy()
    {
        string root = NewTempFolder();

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "index.html"), "<h1>Preview</h1>");
            await File.WriteAllTextAsync(Path.Combine(root, "site.css"), "body { color: red; }");

            using var server = PreviewServer.Start(root);
            using var client = new HttpClient();

            var page = await client.GetAsync(server.BaseUrl);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Equal("text/html", page.Content.Headers.ContentType!.MediaType);
            Assert.Equal("utf-8", page.Content.Headers.ContentType.CharSet);
            Assert.True(page.Headers.TryGetValues("Referrer-Policy", out var policies));
            Assert.Contains("strict-origin-when-cross-origin", policies!);
            Assert.Contains("Preview", await page.Content.ReadAsStringAsync());

            var css = await client.GetStringAsync(server.BaseUrl + "site.css");
            Assert.Contains("color: red", css);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task MissingFilesReturnNotFound()
    {
        string root = NewTempFolder();

        try
        {
            using var server = PreviewServer.Start(root);
            using var client = new HttpClient();

            var response = await client.GetAsync(server.BaseUrl + "missing.html");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string NewTempFolder()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "WebCanvasDotNet.Tests",
            Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        return root;
    }
}
