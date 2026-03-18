namespace SsePulse;

public class HandlerNotFoundException : Exception
{
    public HandlerNotFoundException(string eventName) : base($"Handler for event '{eventName}' not found.")
    {
    }
}