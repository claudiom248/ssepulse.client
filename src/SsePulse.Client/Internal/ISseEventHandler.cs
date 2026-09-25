using System.Net.ServerSentEvents;

namespace SsePulse.Client.Internal;

internal interface ISseEventHandler
{
    ValueTask InvokeAsync(SseItem<string> item, CancellationToken cancellationToken);
}
