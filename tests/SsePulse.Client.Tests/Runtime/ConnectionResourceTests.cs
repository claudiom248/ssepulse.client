using System.Net;
using System.Net.Http.Headers;
using System.Text;
using SsePulse.Client;

namespace SsePulse.Client.Tests.Runtime;

public class ConnectionResourceTests
{
    [Fact]
    public async Task ResponseIsDisposed_WhenTheStatusIsNotSuccessful()
    {
        TrackingHandler handler = new(HttpStatusCode.Unauthorized, string.Empty);
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://localhost") };
        await using SseSource source = new(client, new SseSourceOptions { Path = "/events" });

        await Assert.ThrowsAsync<HttpRequestException>(() => source.StartConsumeAsync(CancellationToken.None));

        Assert.True(handler.Content!.IsDisposed);
    }

    [Fact]
    public async Task ResponseIsDisposed_WhenTheStreamEnds()
    {
        TrackingHandler handler = new(HttpStatusCode.OK, "event: order\ndata: 1\n\n");
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://localhost") };
        await using SseSource source = new(client, new SseSourceOptions { Path = "/events" });
        source.On("order", _ => { });

        await source.StartConsumeAsync(CancellationToken.None);

        Assert.True(handler.Content!.IsDisposed);
    }

    private sealed class TrackingHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public TrackingContent? Content { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Content = new TrackingContent(body);
            HttpResponseMessage response = new(statusCode) { Content = Content };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            return Task.FromResult(response);
        }
    }

    private sealed class TrackingContent(string body) : StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(body)))
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
