using System.Net;
using System.Text;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Infrastructure;

public class SseTestServerTests
{
    private static async Task<string> ReadBodyAsync(HttpClient client, string path = "/events")
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Send_WritesWellFormedFrames()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order-created", "{\"id\":1}", id: "1")
                .Send("note", "line one\nline two", retryMilliseconds: 3000)
                .Close()));
        using HttpClient client = server.CreateClient();

        string body = await ReadBodyAsync(client);

        Assert.Equal(
            "id: 1\nevent: order-created\ndata: {\"id\":1}\n\n" +
            "retry: 3000\nevent: note\ndata: line one\ndata: line two\n\n",
            body);
    }

    [Fact]
    public async Task Comment_And_Retry_WriteTheirOwnFrames()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Comment("keep-alive").Retry(1500).Close()));
        using HttpClient client = server.CreateClient();

        string body = await ReadBodyAsync(client);

        Assert.Equal(": keep-alive\n\nretry: 1500\n\n", body);
    }

    [Fact]
    public async Task Response_HasEventStreamContentType()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s.OnConnection(c => c.Close()));
        using HttpClient client = server.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Respond_ReturnsStatusAndHeaders()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(503, ("Retry-After", "2"))));
        using HttpClient client = server.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(2), response.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Requests_AreRecordedWithMethodHeadersAndBody()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s.OnConnection(c => c.Close()));
        using HttpClient client = server.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, "/events?stream=orders")
        {
            Content = new StringContent("{\"filter\":\"all\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Last-Event-ID", "42");

        using HttpResponseMessage response = await client.SendAsync(request);

        RecordedRequest recorded = Assert.Single(server.Requests);
        Assert.Equal(0, recorded.ConnectionIndex);
        Assert.Equal("POST", recorded.Method);
        Assert.Equal("/events?stream=orders", recorded.Target);
        Assert.Equal("42", recorded.LastEventId);
        Assert.Equal("{\"filter\":\"all\"}", recorded.Body);
    }

    [Fact]
    public async Task Scripts_AreConsumedInOrder_AndUnscriptedConnectionsGetServerError()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("first", "1").Close())
            .OnConnection(c => c.Send("second", "2").Close()));
        using HttpClient client = server.CreateClient();

        string first = await ReadBodyAsync(client);
        string second = await ReadBodyAsync(client);
        using HttpResponseMessage third = await client.GetAsync("/events");

        Assert.Contains("event: first", first);
        Assert.Contains("event: second", second);
        Assert.Equal(HttpStatusCode.InternalServerError, third.StatusCode);
        Assert.Equal(3, server.Requests.Count);
    }

    [Fact]
    public async Task Drop_AbortsTheResponseAfterTheGateOpens()
    {
        SseGate gate = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("first", "1").WaitFor(gate).Drop()));
        using HttpClient client = server.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/events", HttpCompletionOption.ResponseHeadersRead);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new(stream);
        string? firstLine = await reader.ReadLineAsync();
        gate.Open();

        Assert.Equal("event: first", firstLine);
        await Assert.ThrowsAnyAsync<IOException>(async () =>
        {
            while (await reader.ReadLineAsync() is not null)
            {
            }
        });
    }

    [Fact]
    public async Task KeepOpen_SendsHeadersAndThenStalls()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s.OnConnection(c => c.KeepOpen()));
        using HttpClient client = server.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/events", HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await server.RequestLog.WaitForCountAsync(1);
    }
}
