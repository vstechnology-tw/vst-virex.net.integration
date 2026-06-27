using System.IO;
using System.Net;
using System.Text;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class RestSimulatorServer
{
    private static readonly string[] AllowedResultQueryKeys = ["waferID", "lotID", "recipe"];

    private readonly SimulatorSession _session;
    private readonly string _prefix;
    private readonly HttpListener _listener = new HttpListener();
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public RestSimulatorServer(SimulatorSession session, string prefix)
    {
        _session = session;
        _prefix = string.IsNullOrWhiteSpace(prefix) ? "http://127.0.0.1:5088/" : EnsureTrailingSlash(prefix);
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _listener.Prefixes.Add(_prefix);
        _listener.Start();
        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        _session.WriteLog("REST listening at " + _prefix);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_listener.IsListening)
            _listener.Stop();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { }
        }
        _listener.Close();
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch when (token.IsCancellationRequested || !_listener.IsListening)
            {
                return;
            }

            _ = Task.Run(() => HandleAsync(context), token);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            _session.WriteLog("REST " + context.Request.HttpMethod + " " + path);

            if (path == RestRoutes.Health)
            {
                await JsonAsync(context, new { status = "ok" }).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiStatus && context.Request.HttpMethod == "GET")
            {
                await JsonAsync(context, _session.Status).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiError && context.Request.HttpMethod == "GET")
            {
                await JsonAsync(context, _session.Error).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiProductInfo && context.Request.HttpMethod == "GET")
            {
                await JsonAsync(context, _session.ProductInfo).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiProductInfo && context.Request.HttpMethod == "POST")
            {
                var body = await ReadBodyAsync(context).ConfigureAwait(false);
                var info = ProtocolJson.Deserialize<ProductInfo>(body) ?? new ProductInfo();
                await CommandAsync(context, await _session.SetProductInfoAsync(info).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiSystemInitialize && context.Request.HttpMethod == "POST")
            {
                await CommandAsync(context, await _session.InitializeAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiSystemDeinitialize && context.Request.HttpMethod == "POST")
            {
                await CommandAsync(context, await _session.DeinitializeAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiSystemStart && context.Request.HttpMethod == "POST")
            {
                var request = await ReadOptionalJsonAsync<SystemStartRequest>(context).ConfigureAwait(false) ?? new SystemStartRequest();
                await CommandAsync(context, await _session.StartAsync(request).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiSystemStop && context.Request.HttpMethod == "POST")
            {
                var request = await ReadOptionalJsonAsync<SystemStopRequest>(context).ConfigureAwait(false) ?? new SystemStopRequest();
                await CommandAsync(context, await _session.StopAsync(request).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.ApiResults && context.Request.HttpMethod == "GET")
            {
                var invalid = context.Request.QueryString.AllKeys
                    .Where(key => !string.IsNullOrWhiteSpace(key) &&
                        !AllowedResultQueryKeys.Contains(key!, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                if (invalid.Length > 0)
                {
                    context.Response.StatusCode = 400;
                    await JsonAsync(context, new
                    {
                        error = "Only waferID, lotID, and recipe are supported query parameters.",
                        invalid,
                    }).ConfigureAwait(false);
                    return;
                }

                var items = _session.QueryResults(
                    context.Request.QueryString["waferID"],
                    context.Request.QueryString["lotID"],
                    context.Request.QueryString["recipe"]);
                await JsonAsync(context, new ResultList { Items = items, Count = items.Length }).ConfigureAwait(false);
            }
            else if (path == RestRoutes.OpenApiJson)
            {
                await JsonAsync(context, OpenApiDocument.Create(_prefix)).ConfigureAwait(false);
            }
            else if (path == RestRoutes.Scalar)
            {
                await TextAsync(context, ScalarHtml(), "text/html").ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
                await TextAsync(context, "Not found", "text/plain").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await TextAsync(context, ex.Message, "text/plain").ConfigureAwait(false);
        }
    }

    private static async Task<string> ReadBodyAsync(HttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static async Task<T?> ReadOptionalJsonAsync<T>(HttpListenerContext context)
    {
        if (!context.Request.HasEntityBody)
            return default;

        var body = await ReadBodyAsync(context).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(body) ? default : ProtocolJson.Deserialize<T>(body);
    }

    private static Task CommandAsync(HttpListenerContext context, CommandResponse response)
    {
        if (!response.Accepted)
            context.Response.StatusCode = response.ErrorCode == CommandErrorCodes.InvalidRunMode ? 400 : 409;
        return JsonAsync(context, response);
    }

    private static Task JsonAsync<T>(HttpListenerContext context, T value) =>
        TextAsync(context, ProtocolJson.Serialize(value), "application/json");

    private static async Task TextAsync(HttpListenerContext context, string text, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        context.Response.ContentType = contentType;
        context.Response.ContentEncoding = Encoding.UTF8;
        context.Response.ContentLength64 = bytes.Length;
        await WriteResponseAsync(context.Response.OutputStream, bytes).ConfigureAwait(false);
        context.Response.Close();
    }

    private static string EnsureTrailingSlash(string value) => value.Trim().TrimEnd('/') + "/";

    private static string ScalarHtml() =>
        @"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Virex.NET Simulator API</title>
</head>
<body>
  <script id=""api-reference"" data-url=""/openapi/v1.json""></script>
  <script src=""https://cdn.jsdelivr.net/npm/@scalar/api-reference""></script>
</body>
</html>";

    private static Task WriteResponseAsync(Stream stream, byte[] bytes)
    {
#if NET48
        return stream.WriteAsync(bytes, 0, bytes.Length);
#else
        return stream.WriteAsync(bytes, CancellationToken.None).AsTask();
#endif
    }
}
