using System.Net.ServerSentEvents;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SsePulse.Client;

namespace SsePulse.Client.Internal;

internal class StreamConsumer
{
    private const string? ResponseAbortedMessage =
        "SSE stream ended prematurely. This may indicate that the server closed the connection unexpectedly.";

    private readonly SseHandlersDictionary _handlers;
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private readonly Func<Exception, ValueTask> _onError;
    private readonly ILastEventIdStore? _lastEventIdStore;
    private readonly EventIdCommitTracker _commitTracker = new();
    private readonly SemaphoreSlim _commitLock = new(1, 1);
    private long _committedVersion;

    public StreamConsumer(
        SseHandlersDictionary handlers,
        SseSourceOptions options,
        ILogger<SseSource> logger,
        Func<Exception, ValueTask> onError,
        ILastEventIdStore? lastEventIdStore = null)
    {
        _handlers = handlers;
        _options = options;
        _logger = logger;
        _onError = onError;
        _lastEventIdStore = lastEventIdStore;
    }

    public async Task ConsumeAsync(Stream stream, CancellationToken cancellationToken)
    {
        using CancellationTokenSource faultSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<DispatchItem> channel = Channel.CreateBounded<DispatchItem>(
            new BoundedChannelOptions(_options.MaxBufferedEvents)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = _options.MaxDegreeOfParallelism == 1
            });
        ExceptionDispatchInfo? fault = null;
        Task[] workers = Enumerable
            .Range(0, _options.MaxDegreeOfParallelism)
            .Select(_ => Task.Run(RunWorkerAsync, CancellationToken.None))
            .ToArray();
        SseParser<string> parser = SseParser.Create(stream);
        try
        {
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(faultSource.Token).ConfigureAwait(false))
            {
                using IDisposable? _ = _logger.BeginScope("EventType: {EventType}", sseItem.EventType);
                _logger.LogDebug("Received event of type '{EventType}' and Data {Data}", sseItem.EventType,
                    sseItem.Data);
                long sequence = _lastEventIdStore is null ? -1 : _commitTracker.Register(sseItem.EventId);
                await channel.Writer.WriteAsync(new DispatchItem(sequence, sseItem), faultSource.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (HttpIOException ioEx) when (ioEx.HttpRequestError == HttpRequestError.ResponseEnded)
        {
            _logger.LogError(ioEx, ResponseAbortedMessage);
            throw new ResponseAbortedException(ioEx);
        }
        catch (IOException hre) when (hre.FindInner<SocketException>() is
                                          { SocketErrorCode: SocketError.ConnectionReset })
        {
            _logger.LogError(hre, ResponseAbortedMessage);
            throw new ResponseAbortedException(hre);
        }
        catch (Exception ex) when (IsResponseAborted(ex))
        {
            throw new ResponseAbortedException(ex);
        }
        finally
        {
            channel.Writer.TryComplete();
            await Task.WhenAll(workers).ConfigureAwait(false);
            fault?.Throw();
        }

        return;

        async Task RunWorkerAsync()
        {
            try
            {
                await foreach (DispatchItem item in channel.Reader.ReadAllAsync(faultSource.Token).ConfigureAwait(false))
                {
                    await DispatchAsync(item, faultSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogTrace("A handler failed. Stopping the processing of incoming events.");
                Interlocked.CompareExchange(ref fault, ExceptionDispatchInfo.Capture(ex), null);
                faultSource.Cancel();
            }
        }
    }    
    private bool IsResponseAborted(Exception ex)
    {
        if (_options.IsResponseAborted?.Invoke(ex) == true)
        {
            return true;
        }

        switch (ex)
        {
            case HttpIOException { HttpRequestError: HttpRequestError.ResponseEnded }:
            case IOException hre when 
                hre.FindInner<SocketException>() is { SocketErrorCode: SocketError.ConnectionReset }:
                return true;
            default:
                return false;
        } }

    private async Task DispatchAsync(DispatchItem item, CancellationToken cancellationToken)
    {
        SseItem<string> @event = item.Event;
        string eventType = @event.EventType;
        if (!_handlers.TryGetValue(eventType, out List<ISseEventHandler>? eventHandlers))
        {
            if (_options.ThrowWhenNoEventHandlerFound)
            {
                _logger.LogError("No handler found for event type '{EventType}'", eventType);
                throw new HandlerNotFoundException(eventType);
            }

            _logger.LogWarning("No handler found for event type '{EventType}'", eventType);
            await CommitAsync(item.Sequence).ConfigureAwait(false);
            return;
        }

        try
        {
            foreach (ISseEventHandler eventHandler in eventHandlers)
            {
                await eventHandler.InvokeAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event '{EventType}'", eventType);
            await InvokeOnErrorAsync(ex).ConfigureAwait(false);
            if (_options.HandlerFailureBehavior == HandlerFailureBehavior.StopSource)
            {
                throw;
            }
        }

        await CommitAsync(item.Sequence).ConfigureAwait(false);
    }

    private async ValueTask InvokeOnErrorAsync(Exception exception)
    {
        try
        {
            await _onError.Invoke(exception).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The OnError callback threw an exception");
        }
    }

    private async ValueTask CommitAsync(long sequence)
    {
        if (_lastEventIdStore is null)
        {
            return;
        }

        EventIdAdvance? advance = _commitTracker.Complete(sequence);
        if (advance is not { } value)
        {
            return;
        }

        await _commitLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (value.Version <= _committedVersion)
            {
                return;
            }

            _committedVersion = value.Version;
            _logger.LogDebug("Set last event ID to '{EventId}'", value.EventId);
            await _lastEventIdStore.SetLastEventIdAsync(value.EventId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store the last event ID '{EventId}'", value.EventId);
        }
        finally
        {
            _commitLock.Release();
        }
    }

    private readonly record struct DispatchItem(long Sequence, SseItem<string> Event);
}