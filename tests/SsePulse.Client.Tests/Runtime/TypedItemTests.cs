using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class TypedItemTests
{
    [Fact]
    public async Task OnItemOfT_PassesTheEventIdAndTheEventType()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "{\"id\":5}", id: "abc").Close()));
        Recorder<(string? EventId, string EventType, int OrderId)> received = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.OnItem<Order>("order", item => received.Add((item.EventId, item.EventType, item.Data.Id)));
        harness.Start();

        await harness.Consumption;

        Assert.Equal(("abc", "order", 5), Assert.Single(received.Items));
    }

    private sealed record Order(int Id);
}
