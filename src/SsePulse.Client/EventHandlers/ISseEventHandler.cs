using System.Net.ServerSentEvents;

namespace SsePulse.Client.EventHandlers;

internal interface ISseEventHandler
{
    void Invoke(SseItem<string> item);
    Task InvokeAsync(SseItem<string> item, CancellationToken cancellationToken = default);
}