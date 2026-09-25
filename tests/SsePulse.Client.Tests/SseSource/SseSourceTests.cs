using SsePulse.Client;

namespace SsePulse.Client.Tests.Source;

public class SseSourceTests : SseSourceTestBase
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // ARRANGE
        SseSourceOptions options = new() { Path = "/events" };
        using HttpClient client = new();

        // ACT
        using SseSource source = new(client, options);

        // ASSERT
        Assert.False(source.IsConnected);
    }
}