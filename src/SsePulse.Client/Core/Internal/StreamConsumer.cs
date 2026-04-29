using System.Net.ServerSentEvents;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Common.Extensions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core.Internal;

internal class StreamConsumer
{
    private const string? ResponseAbortedMessage =
        "SSE stream ended prematurely. This may indicate that the server closed the connection unexpectedly.";

    private readonly SseHandlersDictionary _handlers;
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private readonly Action<Exception> _onError;
    private readonly ILastEventIdStore? _lastEventIdStore;

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
        ActionBlock<SseItem<string>> dispatcherBlock = CreateDispatcherBlock();
        SseParser<string> parser = SseParser.Create(stream);
        try
        {
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                using IDisposable? _ = _logger.BeginScope("EventType: {EventType}", sseItem.EventType);
                _logger.LogDebug("Received event of type '{EventType}' and Data {Data}", sseItem.EventType, sseItem.Data);
                if (_lastEventIdStore is not null && !string.IsNullOrWhiteSpace(sseItem.EventId))
                {
                    _logger.LogDebug("Set last event ID to '{EventId}'", sseItem.EventId);
                    _lastEventIdStore.Set(sseItem.EventId!);
                }

                if (dispatcherBlock.Completion is { IsFaulted: true, Exception: not null })
                {
                    _logger.LogTrace("Dispatcher block is in a faulted state. Throwing exception to stop processing incoming events.");
                    throw dispatcherBlock.Completion.Exception;
                }

                await dispatcherBlock.SendAsync(sseItem, cancellationToken).ConfigureAwait(false);
            }
        }
#if NET8_0_OR_GREATER
        catch (HttpIOException ioEx) when (ioEx.HttpRequestError == HttpRequestError.ResponseEnded)
        {
            _logger.LogError(ioEx, ResponseAbortedMessage);
            throw new ResponseAbortedException(ioEx);
        }
#endif
        catch (IOException hre) when (hre.FindInner<SocketException>() is
                                          { SocketErrorCode: SocketError.ConnectionReset })
        {
            _logger.LogError(hre, ResponseAbortedMessage);
            throw new ResponseAbortedException(hre);
        }
        finally
        {
            dispatcherBlock.Complete();
            await dispatcherBlock.Completion.ConfigureAwait(false);
        }

        return;

        ActionBlock<SseItem<string>> CreateDispatcherBlock()
        {
            return new ActionBlock<SseItem<string>>(
                Dispatch,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                });
        }
    }

    private void Dispatch(SseItem<string> @event)
    {
        string eventType = @event.EventType;
        if (!_handlers.TryGetValue(eventType, out List<ISseEventHandler>? eventHandlers))
        {
            if (_options.ThrowWhenNoEventHandlerFound)
            {
                _logger.LogError("No handler found for event type '{EventType}'", eventType);
                throw new HandlerNotFoundException(eventType);
            }

            _logger.LogWarning("No handler found for event type '{EventType}'", eventType);
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
    }
}