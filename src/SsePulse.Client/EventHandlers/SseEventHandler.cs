using System.Net.ServerSentEvents;
using System.Text.Json;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.EventHandlers;

internal class SseEventHandler : ISseEventHandler
{
    private readonly Action<SseItem<string>> _handler;

    public SseEventHandler(Action<SseItem<string>> handler)
    {
        _handler = handler;
    }

    public void Invoke(SseItem<string> item)
    {
        _handler.Invoke(item);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal class SseEventHandler<TEventData> : ISseEventHandler
{
    private readonly Action<SseItem<TEventData>> _handler;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    public SseEventHandler(Action<SseItem<TEventData>> handler, JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public void Invoke(SseItem<string> item)
    {
        TEventData message = JsonSerializer.Deserialize<TEventData>(
            item.Data, 
            _jsonSerializerOptions)!;
        SseItem<TEventData> adaptedItem = new(message, item.EventType);
        _handler.Invoke(adaptedItem);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}