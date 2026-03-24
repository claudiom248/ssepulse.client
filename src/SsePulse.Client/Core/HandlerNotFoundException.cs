namespace SsePulse.Client.Core;

public sealed class HandlerNotFoundException : Exception
{
    internal HandlerNotFoundException(string eventName) : base($"Handler for event '{eventName}' not found.")
    {
    }
}