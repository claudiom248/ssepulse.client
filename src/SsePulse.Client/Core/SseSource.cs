using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;

namespace SsePulse.Client.Core;

public partial class SseSource: IDisposable, IAsyncDisposable
{
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private CancellationTokenSource _cts = new();
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SseConnection _connection;
    private readonly ConnectionHandlers _connectionHandlers;

    private int _started;
    private volatile bool _disposed;

    public bool IsConnected => _connection.IsConnected;
    public Task Completion => _tcs.Task;

    private readonly ILastEventIdStore? _lastEventIdStore;

    public SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource>? logger = null)
        :this(client, options, [], null, logger ?? NullLogger<SseSource>.Instance)
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
        if (Interlocked.CompareExchange(ref _started, 1, 0) == 1)
        {
            throw new InvalidOperationException($"{nameof(SseSource)} already started.");
        }
        
        CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            cancellationToken);

        while (true)
        {
            _logger.LogInformation("Starting SSE consumption from {Path}", _options.Path);
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
                using Stream sseStream = await _connection.EstablishAsync(linkedCancellationTokenSource.Token);
                _logger.LogDebug("SSE stream opened successfully");
                StreamConsumer consumer = new(_handlers, _options, _logger, OnError, _lastEventIdStore);
                await consumer.ConsumeAsync(sseStream, linkedCancellationTokenSource.Token);
#endif
                
                _tcs.TrySetResult(true);
                _connection.SetDisconnected();
                return;
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogInformation("SSE consumption canceled");
                _connection.SetDisconnected();
                if (linkedCancellationTokenSource.IsCancellationRequested)
                {
                    _tcs.TrySetResult(true);
                    return;
                }
                _tcs.TrySetCanceled(oce.CancellationToken);
                throw;
            }
            catch (ResponseAbortedException rae)
            {
                // When the connection is closed prematurely, we will restart the connection-consume loop.
                if (_options.RestartOnConnectionAbort)
                {
                    continue;
                }
                _tcs.TrySetException(rae);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during SSE consumption");
                _tcs.TrySetException(ex);
                _connection.SetDisconnected(ex);
                throw;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertStarted()
    {
        if (_started == 0)
        {
            throw new InvalidOperationException($"{nameof(SseSource)} not started.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotStarted()
    {
        if (_started == 1)
        {
            throw new InvalidOperationException($"{nameof(SseSource)} already started.");
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
    }
    
    public async Task StopAsync()
    {
        AssertNotDisposed();
        AssertStarted();
        _logger.LogInformation("Stopping SSE consumption");
#if !NETSTANDARD2_0
        await _cts.CancelAsync().ConfigureAwait(false);
#else
        _cts.Cancel();
#endif
    }

    public void Reset()
    {
        AssertNotDisposed();
        if (!Completion.GetAwaiter().IsCompleted)
        {
            throw new InvalidOperationException($"{nameof(SseSource)} can be reset only after completion.");
        }

        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _tcs = new TaskCompletionSource<bool>();
        _started = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_started == 1)
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_started == 1 && !Completion.IsCompleted)
        {
#if !NETSTANDARD2_0
            await _cts.CancelAsync().ConfigureAwait(false);
#else
            _cts.Cancel();   
#endif            
            _cts.Dispose();
            await Completion.ConfigureAwait(false);
        }
        else
        {
            _tcs.TrySetResult(true);
        }

        OnDisposed?.Invoke();
        GC.SuppressFinalize(this);
    }
}