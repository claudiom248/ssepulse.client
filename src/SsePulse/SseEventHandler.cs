using System.Net.ServerSentEvents;
using System.Text.Json;

namespace SsePulse;

public class SseEventHandler : ISseEventHandler
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

public class SseEventHandler<TEventData> : ISseEventHandler
{
    private readonly Action<SseItem<TEventData>> _handler;

    public SseEventHandler(Action<SseItem<TEventData>> handler)
    {
        _handler = handler;
    }

    public void Invoke(SseItem<string> item)
    {
        TEventData message = JsonSerializer.Deserialize<TEventData>(item.Data)!;
        SseItem<TEventData> adaptedItem = new(message, item.EventType);
        _handler.Invoke(adaptedItem);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}