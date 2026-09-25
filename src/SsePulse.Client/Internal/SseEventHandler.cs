using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SsePulse.Client.Internal;

internal class SseEventHandler : ISseEventHandler
{
    private readonly Func<SseItem<string>, CancellationToken, ValueTask> _handler;

    public SseEventHandler(Func<SseItem<string>, CancellationToken, ValueTask> handler)
    {
        _handler = handler;
    }

    public SseEventHandler(Action<SseItem<string>> handler)
        : this(HandlerAdapter.ToAsync(handler))
    {
    }

    public ValueTask InvokeAsync(SseItem<string> item, CancellationToken cancellationToken)
    {
        return _handler.Invoke(item, cancellationToken);
    }
}

internal class SseEventHandler<TEventData> : ISseEventHandler
{
    private readonly Func<SseItem<TEventData>, CancellationToken, ValueTask> _handler;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private JsonTypeInfo<TEventData>? _typeInfo;

    public SseEventHandler(
        Func<SseItem<TEventData>, CancellationToken, ValueTask> handler,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public SseEventHandler(Action<SseItem<TEventData>> handler, JsonSerializerOptions jsonSerializerOptions)
        : this(HandlerAdapter.ToAsync(handler), jsonSerializerOptions)
    {
    }

    public ValueTask InvokeAsync(SseItem<string> item, CancellationToken cancellationToken)
    {
        JsonTypeInfo<TEventData> typeInfo = _typeInfo ??= JsonTypeInfoProvider.Get<TEventData>(_jsonSerializerOptions);
        TEventData message = JsonSerializer.Deserialize(item.Data, typeInfo)!;
        SseItem<TEventData> adaptedItem = new(message, item.EventType)
        {
            EventId = item.EventId,
            ReconnectionInterval = item.ReconnectionInterval
        };
        return _handler.Invoke(adaptedItem, cancellationToken);
    }
}
