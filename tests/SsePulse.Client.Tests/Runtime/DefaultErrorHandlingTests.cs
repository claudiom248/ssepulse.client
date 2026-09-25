using Microsoft.Extensions.Logging;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.Runtime;

public class DefaultErrorHandlingTests
{
    [Fact]
    public async Task HandlerException_IsLoggedThroughTheLogger_WhenNoOnErrorIsConfigured()
    {
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "order", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, new SseSourceOptions { Path = "/events" }, logger);
        source.On("order", _ => throw new InvalidOperationException("boom"));

        await source.StartConsumeAsync(CancellationToken.None);

        Assert.True(logger.HasLog(LogLevel.Error, "An error occurred while processing an SSE event"));
    }
}
