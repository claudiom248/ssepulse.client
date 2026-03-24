using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core;

public partial class SseSource : IDisposable
#if !NETSTANDARD2_0
    , IAsyncDisposable
#endif
{
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private CancellationTokenSource _cts = new();
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SseConnection _connection;

    private volatile bool _started;
    private volatile bool _disposed;

    public bool IsConnected => _connection.IsConnected;
    public Task Completion => _tcs.Task;

    internal readonly IEnumerable<IRequestMutator> _requestMutators = [];
    private readonly ILastEventIdStore? _lastEventIdStore;

    public SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<SseSource>.Instance;
        _connection = new SseConnection(
            this,
            client,
            options,
            _logger);
    }
    
    internal SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource> logger,
        IEnumerable<IRequestMutator> requestMutators, ILastEventIdStore? lastEventIdStore = null) : this(client, options, logger)
    {
        _requestMutators = requestMutators;
        _lastEventIdStore = lastEventIdStore;
    }

    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _started = true;

        _logger.LogInformation("Starting SSE consumption from {Path}", _options.Path);

        CancellationToken linkedCancellationToken = CreateLinkedCancellationToken();

        try
        {
#if NET8_0_OR_GREATER
            await using Stream sseStream = await _connection.EstablishAsync(linkedCancellationToken);
#else 
            using Stream sseStream = await _connection.EstablishAsync(linkedCancellationToken);
#endif
            _logger.LogDebug("SSE stream opened successfully");
            await ConsumeStream(sseStream);
            _tcs.TrySetResult(true);
            _connection.SetDisconnected();
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogInformation("SSE consumption canceled");
            _connection.SetDisconnected();
            if (_cts.IsCancellationRequested)
            {
                _tcs.TrySetResult(true);
            }
            else
            {
                _tcs.TrySetCanceled(oce.CancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during SSE consumption");
            _tcs.TrySetException(ex);
            _connection.SetDisconnected(ex);
            throw;
        }

        return;

        ActionBlock<SseItem<string>> CreateDispatcherBlock()
        {
            ActionBlock<SseItem<string>> dispatcherBlock = new(
                Dispatch,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                    CancellationToken = linkedCancellationToken
                });
            return dispatcherBlock;
        }

        async Task ConsumeStream(Stream sseStream)
        {
            ActionBlock<SseItem<string>> dispatcherBlock = CreateDispatcherBlock();
            SseParser<string> parser = SseParser.Create(sseStream);
            try
            {
                await foreach (SseItem<string> sseItem in parser.EnumerateAsync(linkedCancellationToken))
                {
                    if (_lastEventIdStore is not null && !string.IsNullOrWhiteSpace(sseItem.EventId))
                    {
                        _lastEventIdStore.Set(sseItem.EventId!);
                    }
                    await dispatcherBlock.SendAsync(sseItem, linkedCancellationToken);
                }
            }
            finally
            {
                dispatcherBlock.Complete();
                await dispatcherBlock.Completion;
            }
        }

        CancellationToken CreateLinkedCancellationToken()
        {
            using CancellationTokenSource linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
            CancellationToken linkedCancellationToken1 = linkedCancellationTokenSource.Token;
            return linkedCancellationToken1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException("SseSource not started.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotStarted()
    {
        if (_started)
        {
            throw new InvalidOperationException("SseSource already started.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SseSource));
        }
    }

    public void Stop()
    {
        AssertNotDisposed();
        AssertStarted();
        _logger.LogInformation("Stopping SSE consumption");
        _cts.Cancel();
        Completion.Wait(TimeSpan.FromSeconds(10));
    }

#if !NETSTANDARD2_0
    public async Task StopAsync()
    {
        AssertNotDisposed();
        AssertStarted();
        _logger.LogInformation("Stopping SSE consumption");
        await _cts.CancelAsync();
    }
#endif


    public void Reset()
    {
        AssertNotDisposed();
        if (!Completion.GetAwaiter().IsCompleted)
        {
            throw new InvalidOperationException("SseSource can be reset only after completion.");
        }

        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _tcs = new TaskCompletionSource<bool>();
        _started = false;
    }

    private void Dispatch(SseItem<string> @event)
    {
        string eventType = @event.EventType;
        if (!_handlers.TryGetValue(eventType, out ISseEventHandler? handler))
        {
            _logger.LogWarning("No handler found for event type '{EventType}'", eventType);
            if (_options.ThrowWhenEventHandlerNotFound)
            {
                throw new HandlerNotFoundException(eventType);
            }
            return;
        }
        try
        {
            handler.Invoke(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event '{EventType}'", eventType);
            OnError(ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_started)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        else
        {
            _tcs.TrySetResult(true);
        }

        _disposed = true;
        OnDisposed?.Invoke();
        GC.SuppressFinalize(this);
    }

#if !NETSTANDARD2_0
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_started && !Completion.IsCompleted)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            await Completion;
        }
        else
        {
            _tcs.TrySetResult(true);
        }

        _disposed = true;
        OnDisposed?.Invoke();
        GC.SuppressFinalize(this);
    }
#endif
}