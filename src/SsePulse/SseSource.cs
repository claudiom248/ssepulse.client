using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Common.Models;
using SsePulse.Utils;

namespace SsePulse;

public partial class SseSource : IDisposable
#if !NETSTANDARD2_0
    , IAsyncDisposable
#endif
{
    private readonly HttpClient _client;
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private CancellationTokenSource _cts = new();
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private volatile bool _started;
    private volatile int _connected;
    private volatile bool _disposed;

    private string? _lastEventId;

    public bool IsConnected => Convert.ToBoolean(_connected);

    public Task Completion => _tcs.Task;

    public SseSource(HttpClient client, SseSourceOptions options, ILogger<SseSource>? logger = null)
    {
        _client = client;
        _options = options;
        _logger = logger ?? NullLogger<SseSource>.Instance;
    }

    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _started = true;

        _logger.LogInformation("Starting SSE consumption from {Path}", _options.Path);

        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        CancellationToken linkedCancellationToken =
            linkedCancellationTokenSource.Token;

        try
        {
            await Execute.WithRetryAsync(
                async _ =>
                {
                    Stream sseStream = await OpenStream(linkedCancellationToken);
                    _logger.LogDebug("SSE stream opened successfully");
                    SetConnected();
                    await ConsumeStream(sseStream);
                    _tcs.TrySetResult(true);
                    SetDisconnected();
                },
                _options.RetryOptions ?? RetryOptions.None,
                TryInvokeConnectionLostHandler,
                cancellationToken: linkedCancellationToken);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogInformation("SSE consumption canceled");
            if (_cts.IsCancellationRequested)
            {
                _tcs.TrySetResult(true);
            }
            else
            {
                _tcs.TrySetCanceled(oce.CancellationToken);
            }
            SetDisconnected();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during SSE consumption");
            _tcs.TrySetException(ex);
            SetDisconnected(ex);
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
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(linkedCancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(sseItem.EventId))          
                {
                    _lastEventId = sseItem.EventId!;
                }
                dispatcherBlock.Post(sseItem);
            }

            dispatcherBlock.Complete();
            await dispatcherBlock.Completion;
        }

        void TryInvokeConnectionLostHandler(Exception ex)
        {
            int wasConnected = Interlocked.CompareExchange(ref _connected, 0, 1);
            if (wasConnected == 1)
            {
                _logger.LogWarning(ex, "Connection lost, attempting to reconnect");
                OnConnectionLost.Invoke(ex);
            }
        }
    }

    /// <summary>
    /// Opens SSE stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Stream> OpenStream(CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(HttpMethod.Get, _options.Path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrEmpty(_lastEventId))
        {
            _logger.LogDebug("Resuming SSE stream from Last-Event-ID: {LastEventId}", _lastEventId);
            request.Headers.TryAddWithoutValidation("Last-Event-ID", _lastEventId);
        }

        HttpResponseMessage response;
        try
        {
            response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while opening SSE stream from {Path}", _options.Path);
            throw;
        }

#if NET8_0_OR_GREATER
        Stream sseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
        Stream sseStream = await response.Content.ReadAsStreamAsync();
#endif
        return sseStream;
    }

    private void SetConnected()
    {
        _connected = 1;
        _logger.LogInformation("SSE connection established");
        OnConnectionEstablished.Invoke();
    }

    private void SetDisconnected(Exception? exception = null)
    {
        _connected = 0;
        if (exception is null)
        {
            _logger.LogInformation("SSE connection closed gracefully");
            OnConnectionClosed.Invoke();
        }
        else
        {
            _logger.LogError(exception, "SSE connection lost due to exception");
            OnConnectionLost.Invoke(exception);
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
        try
        {
            if (!_handlers.TryGetValue(eventType, out ISseEventHandler? handler))
            {
                _logger.LogWarning("No handler found for event type '{EventType}'", eventType);
                throw new HandlerNotFoundException(eventType);
            }
            handler.Invoke(@event);
        }
        catch (Exception ex) when (ex is not HandlerNotFoundException)
        {
            _logger.LogError(ex, "Error occurred while dispatching event '{EventType}'", eventType);
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