using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core.Internal;

internal class StreamConsumer
{
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
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(cancellationToken))
            {
                if (_lastEventIdStore is not null && !string.IsNullOrWhiteSpace(sseItem.EventId))
                {
                    _lastEventIdStore.Set(sseItem.EventId!);
                }
                await dispatcherBlock.SendAsync(sseItem, cancellationToken);
            }
        }
        finally
        {
            dispatcherBlock.Complete();
            await dispatcherBlock.Completion;
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
            _onError(ex);
        }
    }
}