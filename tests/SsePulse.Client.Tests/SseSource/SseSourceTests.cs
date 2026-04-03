using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceTests : SseSourceTestBase
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // ARRANGE
        SseSourceOptions options = new() { Path = "/events" };
        using HttpClient client = new();

        // ACT
        using Core.SseSource source = new(client, options);

        // ASSERT
        Assert.False(source.IsConnected);
    }
}