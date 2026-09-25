using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SsePulse.Client.Tests.Common;

public sealed class SseTestServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<ConnectionScript> _scripts = new();
    private readonly List<RecordedRequest> _requests = [];
    private readonly object _lock = new();
    private readonly CancellationTokenSource _shutdown = new();
    private int _connectionCount;

    private SseTestServer()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(2));
        _app = builder.Build();
        _app.Run(HandleAsync);
    }

    public Uri BaseAddress { get; private set; } = null!;

    public Recorder<RecordedRequest> RequestLog { get; } = new();

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_lock)
            {
                return _requests.ToArray();
            }
        }
    }

    public static async Task<SseTestServer> StartAsync(Action<SseTestServer>? configure = null)
    {
        SseTestServer server = new();
        configure?.Invoke(server);
        await server._app.StartAsync();
        IServerAddressesFeature addresses = server._app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!;
        server.BaseAddress = new Uri(addresses.Addresses.First());
        return server;
    }

    public SseTestServer OnConnection(Action<ConnectionScript> script)
    {
        ConnectionScript connection = new();
        script(connection);
        _scripts.Enqueue(connection);
        return this;
    }

    public SseGate CreateGate() => new();

    public HttpClient CreateClient() => new() { BaseAddress = BaseAddress };

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _shutdown.Dispose();
    }

    private async Task HandleAsync(HttpContext context)
    {
        int index = Interlocked.Increment(ref _connectionCount) - 1;
        string body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Dictionary<string, string> headers = context.Request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
        RecordedRequest recorded = new(
            index,
            context.Request.Method,
            context.Request.Path + context.Request.QueryString.ToString(),
            headers,
            body);
        lock (_lock)
        {
            _requests.Add(recorded);
        }

        RequestLog.Add(recorded);

        if (!_scripts.TryDequeue(out ConnectionScript? script))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("No scripted connection is left for this request.");
            return;
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            _shutdown.Token);
        try
        {
            await script.RunAsync(context, linked.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
