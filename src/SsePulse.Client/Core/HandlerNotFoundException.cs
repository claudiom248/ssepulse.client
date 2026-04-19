namespace SsePulse.Client.Core;

/// <summary>
/// Thrown when an SSE event is received but no handler is registered for its event type,
/// and <see cref="SsePulse.Client.Core.Configurations.SseSourceOptions.ThrowWhenNoEventHandlerFound"/> is <see langword="true"/>.
/// </summary>
public sealed class HandlerNotFoundException : Exception
{
    internal HandlerNotFoundException(string eventName) : base($"Handler for event '{eventName}' not found.")
    {
    }
}