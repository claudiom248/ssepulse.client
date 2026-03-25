using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceTests : SseSourceTestBase
{

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        using HttpClient client = new();
        SseSourceOptions options = new() { Path = "/events" };
        using Core.SseSource source = new(client, options);
        Assert.False(source.IsConnected);
    }
}