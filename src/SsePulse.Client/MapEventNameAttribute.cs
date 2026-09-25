namespace SsePulse.Client;

/// <summary>
/// Overrides the SSE event name that a handler method inside an <see cref="SsePulse.Client.ISseEventsManager"/>
/// implementation is mapped to. By default the event name is derived from the method name (minus the "On" prefix)
/// and formatted using <see cref="SsePulse.Client.SseSourceOptions.DefaultEventNameCasePolicy"/>.
/// Apply this attribute when the desired event name cannot be expressed as a valid C# method name.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MapEventNameAttribute : Attribute
{
    /// <summary>Gets or sets the SSE event name this method handles.</summary>
    public string EventName { get; set; } = null!;
}