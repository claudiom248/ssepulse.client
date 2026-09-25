using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SsePulse.Client.Internal;

internal class SseEventDataHandler : ISseEventHandler
{
    private readonly Action<string> _handler;

    public SseEventDataHandler(Action<string> handler)
    {
        _handler = handler;
    }

    public void Invoke(SseItem<string> item)
    {
        _handler.Invoke(item.Data);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal class SseEventDataHandler<TEventData> : ISseEventHandler
{
    private readonly Action<TEventData> _handler;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private JsonTypeInfo<TEventData>? _typeInfo;

    public SseEventDataHandler(Action<TEventData> handler, JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public void Invoke(SseItem<string> item)
    {
        JsonTypeInfo<TEventData> typeInfo = _typeInfo ??= JsonTypeInfoProvider.Get<TEventData>(_jsonSerializerOptions);
        TEventData message = JsonSerializer.Deserialize(item.Data, typeInfo)!;
        _handler.Invoke(message);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}