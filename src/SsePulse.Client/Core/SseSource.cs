using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;

namespace SsePulse.Client.Core;

/// <summary>
/// Connects to a Server-Sent Events (SSE) endpoint, streams events, and dispatches them
/// to registered handlers. Supports connection retries, bearer/basic/API-key authentication,
/// last-event-id resumption, and parallel event processing.
/// </summary>
/// <remarks>
/// Register handlers before calling <see cref="StartConsumeAsync"/> using the fluent
/// <c>On</c> / <c>OnItem</c> / <c>Bind</c> methods defined in the partial handler file.
/// The source can be reset and reused after completion via <see cref="Reset"/>.
/// </remarks>
public partial class SseSource : ISseSourceControl, IDisposable, IAsyncDisposable
{
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private CancellationTokenSource _cts = new();
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SseConnection _connection;
    private readonly ConnectionHandlers _connectionHandlers;

    private int _started;
    private volatile bool _disposed;

    /// <summary>Gets a value indicating whether the SSE connection is currently active.</summary>
    public bool IsConnected => _connection.IsConnected;

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the consumption loop finishes,
    /// either successfully, due to cancellation, or with an exception.
    /// </summary>
    public Task Completion => _tcs.Task;

    private readonly ILastEventIdStore? _lastEventIdStore;

    /// <summary>
    /// Initializes a new <see cref="SseSource"/> using the supplied <see cref="HttpClient"/> and options.
    /// </summary>
    /// <param name="client">The HTTP client used to connect to the SSE endpoint.</param>
    /// <param name="options">Configuration options such as path, retry policy, and parallelism.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    public SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource>? logger = null)
        :this(client, options, [], null, logger ?? NullLogger<SseSource>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SseSource"/> with full control over request mutators and
    /// last-event-ID resumption. Mutators are supplied at construction time and applied to every
    /// outgoing request in the order they appear in <paramref name="requestMutators"/>.
    /// </summary>
    /// <param name="client">The HTTP client used to connect to the SSE endpoint.</param>
    /// <param name="options">Configuration options such as path, retry policy, and parallelism.</param>
    /// <param name="requestMutators">
    /// An ordered collection of <see cref="IRequestMutator"/> instances applied to every outgoing
    /// request. Pass an empty collection when no mutation is required.
    /// </param>
    /// <param name="lastEventIdStore">
    /// Optional store that persists the last received event ID. When provided, the value is
    /// automatically set via the <c>Last-Event-ID</c> header on reconnections.
    /// </param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    public SseSource(HttpClient client, SseSourceOptions options,
        IReadOnlyCollection<IRequestMutator> requestMutators, ILastEventIdStore? lastEventIdStore = null,
        ILogger<SseSource>? logger = null)
    {
        _options = options;
        _handlers = new SseHandlersDictionary(options.JsonSerializerOptions);
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

    /// <summary>
    /// Starts consuming the SSE stream and dispatches events to registered handlers.
    /// This method runs until the stream ends, <paramref name="cancellationToken"/> is canceled,
    /// or <see cref="Stop"/>/<see cref="StopAsync"/> is called.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the consumption loop from the caller's side.</param>
    /// <exception cref="InvalidOperationException">Thrown if the source has already been started.</exception>
    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        AssertNotDisposed();
        if (Interlocked.CompareExchange(ref _started, 1, 0) == 1)
        {
            throw new InvalidOperationException($"{nameof(SseSource)} already started.");
        }
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
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

    /// <summary>
    /// Signals the consumption loop to stop by cancelling the internal <see cref="System.Threading.CancellationTokenSource"/>.
    /// Returns immediately; await <see cref="Completion"/> to wait for the loop to finish.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the source has not been started.</exception>
    public void Stop()
    {
        AssertNotDisposed();
        AssertStarted();
        _logger.LogInformation("Stopping SSE consumption");
        _cts.Cancel();
    }
    
    /// <summary>
    /// Asynchronously signals the consumption loop to stop.
    /// On .NET 8+, cancellation is performed asynchronously; on earlier targets it is synchronous.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the source has not been started.</exception>
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

    /// <summary>
    /// Resets the source to its initial state so it can be started again via <see cref="StartConsumeAsync"/>.
    /// May only be called after <see cref="Completion"/> has finished.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Completion"/> has not yet completed.</exception>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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