using System.Net.ServerSentEvents;
using System.Text.Json;

namespace SsePulse;

public class SseDataEventHandler : ISseEventHandler
{
    private readonly Action<string> _handler;

    public SseDataEventHandler(Action<string> handler)
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

public class SseDataEventHandler<TEventData> : ISseEventHandler
{
    private readonly Action<TEventData> _handler;

    public SseDataEventHandler(Action<TEventData> handler)
    {
        _handler = handler;
    }

    public void Invoke(SseItem<string> item)
    {
        TEventData message = JsonSerializer.Deserialize<TEventData>(item.Data)!;
        _handler.Invoke(message);
    }

    public Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}