using SsePulse.Client.Core;

namespace SsePulse.Client.Abstractions;

public interface ISseSourceFactory
{
    SseSource CreateSseSource(string? name);
}