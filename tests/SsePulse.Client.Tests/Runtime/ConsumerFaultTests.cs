using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class ConsumerFaultTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task HandlerNotFound_StopsTheConsumptionAndSurfacesTheOriginalException(int parallelism)
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("unknown", "1").KeepOpen()));
        await using SseSourceHarness harness = new SseSourceHarness(server, options =>
            {
                options.ThrowWhenNoEventHandlerFound = true;
                options.MaxDegreeOfParallelism = parallelism;
            })
            .Start();

        HandlerNotFoundException exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => harness.Consumption);

        Assert.Contains("unknown", exception.Message);
    }
}
