namespace SsePulse.Core;

internal class HandlerNotFoundException : Exception
{
    public HandlerNotFoundException(string eventName) : base($"Handler for event '{eventName}' not found.")
    {
    }
}