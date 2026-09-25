using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SsePulse.Client;
using SsePulse.Client.Internal;

namespace SsePulse.Client.Tests.Common;

public sealed record ReceivedEvent(string EventType, string Data, string? Id);

public sealed class SseSourceHarness : IAsyncDisposable
{
    private readonly HttpClient _client;
    private Task? _consumption;

    public SseSourceHarness(
        SseTestServer server,
        Action<SseSourceOptions>? configure = null,
        ILastEventIdStore? lastEventIdStore = null)
    {
        Time = new FakeTimeProvider();
        SseSourceOptions options = new()
        {
            Path = "/events",
            TimeProvider = Time,
            ConnectionRetryOptions = RetryOptions.None
        };
        configure?.Invoke(options);

        IReadOnlyCollection<IRequestMutator> mutators = lastEventIdStore is null
            ? []
            : [new LastEventIdRequestMutator(lastEventIdStore)];

        _client = server.CreateClient();
        Source = new SseSource(_client, options, mutators, lastEventIdStore, NullLogger<SseSource>.Instance);
        Source.OnError = Errors.Add;
        Source.OnConnectionEstablished = () => Lifecycle.Add("established");
        Source.OnConnectionClosed = () => Lifecycle.Add("closed");
        Source.OnConnectionLost = _ => Lifecycle.Add("lost");
    }

    public SseSource Source { get; }

    public FakeTimeProvider Time { get; }

    public Recorder<ReceivedEvent> Events { get; } = new();

    public Recorder<Exception> Errors { get; } = new();

    public Recorder<string> Lifecycle { get; } = new();

    public Task Consumption => _consumption ?? throw new InvalidOperationException("The harness has not been started.");

    public SseSourceHarness Listen(params string[] eventTypes)
    {
        foreach (string eventType in eventTypes)
        {
            Source.OnItem(eventType, item => Events.Add(new ReceivedEvent(item.EventType, item.Data, item.EventId)));
        }

        return this;
    }

    public SseSourceHarness Start()
    {
        _consumption = Source.StartConsumeAsync(CancellationToken.None);
        return this;
    }

    public async Task StopAsync()
    {
        await Source.StopAsync();
        await Consumption;
    }

    public async Task AdvanceTimeUntilAsync(Func<bool> condition, TimeSpan step, TimeSpan? timeout = null)
    {
        using CancellationTokenSource limit = new(timeout ?? TimeSpan.FromSeconds(10));
        while (!condition())
        {
            limit.Token.ThrowIfCancellationRequested();
            Time.Advance(step);
            await Task.Yield();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Source.DisposeAsync();
        _client.Dispose();
    }
}
