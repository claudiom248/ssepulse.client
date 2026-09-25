using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class EventsManagerBindingTests
{
    [Fact]
    public async Task Bind_RegistersOnlyMethodsNamedOnFollowedByAnUpperCaseLetter()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("Order", "1")
                .Send("Ce", "2")
                .Send("Line", "3")
                .Close()));
        RecordingManager manager = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.Bind(manager);
        harness.Start();

        await harness.Consumption;

        Assert.Equal(["OnOrder"], manager.Calls);
    }

    [Fact]
    public async Task Bind_ThrowsForAHandlerLikeMethodWithTheWrongNumberOfParameters()
    {
        await using SseTestServer server = await SseTestServer.StartAsync();
        await using SseSourceHarness harness = new(server);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => harness.Source.Bind(new TwoParameterManager()));

        Assert.Contains("OnBoth", exception.Message);
        Assert.Contains("exactly one parameter", exception.Message);
    }

    private sealed class RecordingManager : ISseEventsManager
    {
        public List<string> Calls { get; } = [];

        public void OnOrder(string data) => Calls.Add(nameof(OnOrder));

        public void Once(string data) => Calls.Add(nameof(Once));

        public void Online(string data) => Calls.Add(nameof(Online));
    }

    private sealed class TwoParameterManager : ISseEventsManager
    {
        public void OnBoth(string first, string second)
        {
        }
    }
}
