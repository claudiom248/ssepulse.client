using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;

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
    private readonly ConnectionHandlers _connectionHandlers;

    private volatile bool _started;
    private volatile bool _disposed;

    public bool IsConnected => _connection.IsConnected;
    public Task Completion => _tcs.Task;

    private readonly ILastEventIdStore? _lastEventIdStore;

    public SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource>? logger = null)
        : this(client, options, [], null, logger ?? NullLogger<SseSource>.Instance)
    {
    }

    internal SseSource(HttpClient client, SseSourceOptions options,
        IReadOnlyCollection<IRequestMutator> requestMutators, ILastEventIdStore? lastEventIdStore = null,
        ILogger<SseSource>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<SseSource>.Instance;
        _lastEventIdStore = lastEventIdStore;
        _connectionHandlers = new ConnectionHandlers
        {
            OnConnectionEstablished = OnConnectionEstablished,
            OnConnectionClosed = OnConnectionClosed,
            OnConnectionLost = OnConnectionLost
        };
        _connection = new SseConnection(
            requestMutators,
            _connectionHandlers,
            client,
            options,
            _logger);
    }

    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _started = true;

        _logger.LogInformation("Starting SSE consumption from {Path}", _options.Path);

        CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            cancellationToken);

        try
        {
#if NET8_0_OR_GREATER
            Stream sseStream = await _connection.EstablishAsync(linkedCancellationTokenSource.Token).ConfigureAwait(false);
            await using (sseStream.ConfigureAwait(false))
            {
                _logger.LogDebug("SSE stream opened successfully");
                StreamConsumer consumer = new(_handlers, _options, _logger, OnError, _lastEventIdStore);
                await consumer.ConsumeAsync(sseStream, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
#else
            using (Stream sseStream =
                   await _connection.EstablishAsync(linkedCancellationTokenSource.Token).ConfigureAwait(false))
            {
                _logger.LogDebug("SSE stream opened successfully");
                StreamConsumer consumer = new(_handlers, _options, _logger, OnError, _lastEventIdStore);
                await consumer.ConsumeAsync(sseStream, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
#endif
            _tcs.TrySetResult(true);
            _connection.SetDisconnected();
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogInformation("SSE consumption canceled");
            _connection.SetDisconnected();
            if (linkedCancellationTokenSource.IsCancellationRequested)
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
        await _cts.CancelAsync().ConfigureAwait(false);
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
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            await Completion.ConfigureAwait(false);
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