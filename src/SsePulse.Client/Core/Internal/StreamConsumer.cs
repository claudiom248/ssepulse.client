using System.Net.ServerSentEvents;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
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
    private readonly Action<Exception> _onError;
    private readonly ILastEventIdStore? _lastEventIdStore;
    private readonly EventIdCommitTracker _commitTracker = new();

    public StreamConsumer(
        SseHandlersDictionary handlers,
        SseSourceOptions options,
        ILogger<SseSource> logger,
        Action<Exception> onError,
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
        ActionBlock<DispatchItem> dispatcherBlock = CreateDispatcherBlock();
        SseParser<string> parser = SseParser.Create(stream);
        try
        {
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                using IDisposable? _ = _logger.BeginScope("EventType: {EventType}", sseItem.EventType);
                _logger.LogDebug("Received event of type '{EventType}' and Data {Data}", sseItem.EventType,
                    sseItem.Data);
                if (dispatcherBlock.Completion is { IsFaulted: true, Exception: not null })
                {
                    _logger.LogTrace(
                        "Dispatcher block is in a faulted state. Throwing exception to stop processing incoming events.");
                    throw dispatcherBlock.Completion.Exception;
                }

                long sequence = _commitTracker.Register(sseItem.EventId);
                await dispatcherBlock.SendAsync(new DispatchItem(sequence, sseItem), cancellationToken)
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
            dispatcherBlock.Complete();
            await dispatcherBlock.Completion.ConfigureAwait(false);
        }

        return;

        ActionBlock<DispatchItem> CreateDispatcherBlock()
        {
            return new ActionBlock<DispatchItem>(
                Dispatch,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                    BoundedCapacity = _options.MaxBufferedEvents,
                    CancellationToken = cancellationToken
                });
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

    private void Dispatch(DispatchItem item)
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
            Commit(item.Sequence);
            return;
        }

        try
        {
            foreach (ISseEventHandler eventHandler in eventHandlers)
            {
                eventHandler.Invoke(@event);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event '{EventType}'", eventType);
            _onError(ex);
        }

        Commit(item.Sequence);
    }

    private void Commit(long sequence)
    {
        if (_lastEventIdStore is null)
        {
            return;
        }

        _commitTracker.Complete(sequence, eventId =>
        {
            _logger.LogDebug("Set last event ID to '{EventId}'", eventId);
            _lastEventIdStore.Set(eventId);
        });
    }

    private readonly record struct DispatchItem(long Sequence, SseItem<string> Event);
}