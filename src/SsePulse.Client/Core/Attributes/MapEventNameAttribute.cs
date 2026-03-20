namespace SsePulse.Client.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class MapEventNameAttribute : Attribute
{
    public string EventName { get; set; } = null!;
}