using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client;

namespace SsePulse.Client.Internal;

internal class LastEventIdRequestMutator : IRequestMutator
{
    private readonly ILastEventIdStore _lastEventIdStore;
    private readonly ILogger<SseSource> _logger;

    public LastEventIdRequestMutator(ILastEventIdStore lastEventIdStore, ILogger<SseSource>? logger = null)
    {
        _lastEventIdStore = lastEventIdStore;
        _logger = logger ?? NullLogger<SseSource>.Instance;
    }

    public async ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for Last-Event-ID to resume SSE stream...");
        string? lastEventId = await _lastEventIdStore.GetLastEventIdAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(lastEventId))
        {
            _logger.LogDebug("Resuming SSE stream from Last-Event-ID: {LastEventId}", lastEventId);
            message.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
        }
    }
}


