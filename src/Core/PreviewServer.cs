using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebCanvasDotNet.Core;

/// <summary>
/// A tiny read-only HTTP server for preview. Serving over HTTP (instead of
/// file://) gives pages a real origin and referrer, which YouTube and other
/// embeds require.
/// </summary>
public sealed class PreviewServer : IDisposable
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".xml"] = "application/xml; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".avif"] = "image/avif",
        [".ico"] = "image/x-icon",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".otf"] = "font/otf",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".pdf"] = "application/pdf",
        [".wasm"] = "application/wasm",
    };

    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _stop = new();
    private readonly string _root;
    private readonly string _rootWithSeparator;

    private PreviewServer(HttpListener listener, string root, string baseUrl)
    {
        _listener = listener;
        _root = System.IO.Path.GetFullPath(root);
        _rootWithSeparator = _root.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        BaseUrl = baseUrl;
    }

    /// <summary>The root URL, ending in a slash.</summary>
    public string BaseUrl { get; }

    public static PreviewServer Start(string root)
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var baseUrl = "http://127.0.0.1:" + port + "/";
        var listener = new HttpListener();
        listener.Prefixes.Add(baseUrl);
        listener.Start();

        var server = new PreviewServer(listener, root, baseUrl);
        _ = Task.Run(server.AcceptLoopAsync);
        return server;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (_stop.IsCancellationRequested)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => Serve(context));
        }
    }

    private void Serve(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers["Cache-Control"] = "no-store";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            response.Headers["X-Content-Type-Options"] = "nosniff";

            if (request.HttpMethod is not ("GET" or "HEAD"))
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response.Close();
                return;
            }

            var relative = Uri.UnescapeDataString(request.Url?.AbsolutePath ?? "/").TrimStart('/');

            if (relative.Length == 0 || relative.EndsWith('/'))
            {
                relative += "index.html";
            }

            var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                _root,
                relative.Replace('/', System.IO.Path.DirectorySeparatorChar)));

            if (Directory.Exists(full))
            {
                full = System.IO.Path.Combine(full, "index.html");
            }

            if (!full.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(full, _root, StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Close();
                return;
            }

            if (!File.Exists(full))
            {
                var body = Encoding.UTF8.GetBytes("Not found");
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.ContentType = "text/plain; charset=utf-8";
                response.ContentLength64 = body.Length;
                response.OutputStream.Write(body);
                response.Close();
                return;
            }

            var bytes = File.ReadAllBytes(full);
            var extension = System.IO.Path.GetExtension(full);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = ContentTypes.TryGetValue(extension, out var type)
                ? type
                : "application/octet-stream";
            response.ContentLength64 = bytes.Length;

            if (request.HttpMethod == "GET")
            {
                response.OutputStream.Write(bytes);
            }

            response.Close();
        }
        catch (Exception)
        {
            try
            {
                context.Response.Abort();
            }
            catch
            {
                // The client may already be gone.
            }
        }
    }

    public void Dispose()
    {
        _stop.Cancel();

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // Stopping an already-closed listener is harmless.
        }
    }
}
