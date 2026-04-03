using System.Net.ServerSentEvents;
using System.Text.Json;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.EventHandlers;

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

    public SseEventDataHandler(Action<TEventData> handler)
    {
        _handler = handler;
    }

    public void Invoke(SseItem<string> item)
    {
        TEventData message = JsonSerializer.Deserialize<TEventData>(item.Data, SerializationOptions.EventDataJsonSerializerOptions)!;
        _handler.Invoke(message);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}