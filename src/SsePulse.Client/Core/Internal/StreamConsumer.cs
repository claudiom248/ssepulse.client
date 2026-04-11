using System.Net.ServerSentEvents;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core.Internal;

internal class StreamConsumer
{
    private const string? ConnectionResetExceptionMessage =
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
        ActionBlock<SseItem<string>> dispatcherBlock = CreateDispatcherBlock(cancellationToken);
        SseParser<string> parser = SseParser.Create(stream);
        try
        {
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_lastEventIdStore is not null && !string.IsNullOrWhiteSpace(sseItem.EventId))
                {
                    _lastEventIdStore.Set(sseItem.EventId!);
                }

                if (dispatcherBlock.Completion is { IsFaulted: true, Exception: not null })
                {
                    throw dispatcherBlock.Completion.Exception;
                }

                await dispatcherBlock.SendAsync(sseItem, cancellationToken).ConfigureAwait(false);
            }
        }
#if NET8_0_OR_GREATER
        catch (HttpIOException ioEx) when (ioEx.HttpRequestError == HttpRequestError.ResponseEnded)
        {
            _logger.LogError(ioEx, ConnectionResetExceptionMessage);
            throw new ResponseAbortedException(ioEx);
        }
#endif
        catch (IOException hre) when (hre.InnerException is SocketException {SocketErrorCode: SocketError.ConnectionReset})
        {
            _logger.LogError(hre, ConnectionResetExceptionMessage);
            throw new ResponseAbortedException(hre);
        }
        finally
        {
            dispatcherBlock.Complete();
            await dispatcherBlock.Completion.ConfigureAwait(false);
        }
    }

    private ActionBlock<SseItem<string>> CreateDispatcherBlock(CancellationToken cancellationToken)
    {
        ActionBlock<SseItem<string>> dispatcherBlock = new(
            Dispatch,
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            });
        return dispatcherBlock;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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