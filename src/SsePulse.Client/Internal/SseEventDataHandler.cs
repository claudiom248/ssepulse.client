using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SsePulse.Client.Internal;

internal class SseEventDataHandler : ISseEventHandler
{
    private readonly Func<string, CancellationToken, ValueTask> _handler;

    public SseEventDataHandler(Func<string, CancellationToken, ValueTask> handler)
    {
        _handler = handler;
    }

    public SseEventDataHandler(Action<string> handler)
        : this(HandlerAdapter.ToAsync(handler))
    {
    }

    public ValueTask InvokeAsync(SseItem<string> item, CancellationToken cancellationToken)
    {
        return _handler.Invoke(item.Data, cancellationToken);
    }
}

internal class SseEventDataHandler<TEventData> : ISseEventHandler
{
    private readonly Func<TEventData, CancellationToken, ValueTask> _handler;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private JsonTypeInfo<TEventData>? _typeInfo;

    public SseEventDataHandler(
        Func<TEventData, CancellationToken, ValueTask> handler,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public SseEventDataHandler(Action<TEventData> handler, JsonSerializerOptions jsonSerializerOptions)
        : this(HandlerAdapter.ToAsync(handler), jsonSerializerOptions)
    {
    }

    public ValueTask InvokeAsync(SseItem<string> item, CancellationToken cancellationToken)
    {
        JsonTypeInfo<TEventData> typeInfo = _typeInfo ??= JsonTypeInfoProvider.Get<TEventData>(_jsonSerializerOptions);
        TEventData message = JsonSerializer.Deserialize(item.Data, typeInfo)!;
        return _handler.Invoke(message, cancellationToken);
    }
}
