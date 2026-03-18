namespace SsePulse;

public class SsePulseOptions
{
    public Uri BaseUrl { get; set; } = new("http://localhost:8080");
    
    public Dictionary<string, SseSourceOptions> Sources { get; set; } = [];
}