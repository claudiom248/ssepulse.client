namespace SsePulse.Client.Core.Abstractions;

/// <summary>
/// Marker interface for classes that act as SSE event managers.
/// Implementations contain handler methods (prefixed with "On") that are automatically
/// discovered and bound to SSE event types when passed to <see cref="SsePulse.Client.Core.SseSource.Bind{TManager}(TManager)"/>.
/// </summary>
public interface ISseEventsManager
{
    
}