namespace SsePulse.Client.Tests.Common;

public sealed record RecordedRequest(
    int ConnectionIndex,
    string Method,
    string Target,
    IReadOnlyDictionary<string, string> Headers,
    string Body)
{
    public string? LastEventId => Headers.TryGetValue("Last-Event-ID", out string? value) ? value : null;
}
