using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SsePulse.Client.Tests.Mocks;

public static class MockSseHelpers
{
    private static readonly Uri DefaultBaseUrl = new("https://example.com");

    public static HttpClient CreateHttpClientWithSseStream(string sseData)
    {
        MockHttpMessageHandler handler = new(sseData);
        return new HttpClient(handler)
        {
            BaseAddress = DefaultBaseUrl
        };
    }
    
    public static HttpClient CreateHttpClientWithHandler(MockHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = DefaultBaseUrl
        };
    }

    public static string BuildSseStream(params SseEvent[] events)
    {
        StringBuilder sb = new();
        foreach (SseEvent evt in events)
        {
            if (!string.IsNullOrEmpty(evt.Id))
            {
                sb.AppendLine($"id: {evt.Id}");
            }
            if (!string.IsNullOrEmpty(evt.EventType))
            {
                sb.AppendLine($"event: {evt.EventType}");
            }
            if (!string.IsNullOrEmpty(evt.Data))
            {
                foreach (string line in evt.Data.Split('\n'))
                {
                    sb.AppendLine($"data: {line}");
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public class SseEvent
{
    public string? Id { get; set; }
    public string EventType { get; set; } = "message";
    public string Data { get; set; } = string.Empty;
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string? _sseData;
    private readonly Exception? _exception;
    public string? LastEventIdSent { get; private set; }

    public MockHttpMessageHandler(string sseData)
    {
        _sseData = sseData;
    }

    public MockHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        // Capture Last-Event-ID header
        if (request.Headers.TryGetValues("Last-Event-ID", out IEnumerable<string>? values))
        {
            LastEventIdSent = values.FirstOrDefault();
        }

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(_sseData ?? "")))
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");

        return Task.FromResult(response);
    }
}

// 1. Lo stream che esplode alla seconda lettura
public class NetworkFailureStream : Stream
{
    private bool _firstRead = true;
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
    {
        // Il crash avviene qui, quando il parser chiede il secondo evento
        if (!_firstRead) throw new IOException("Connessione interrotta forzatamente dal remote host.");
        _firstRead = false;
        ReadOnlySpan<byte> data = "event: message\ndata: healthy\n\n"u8; // Primo evento OK
        data.CopyTo(buffer);
        return Task.FromResult(data.Length);
    }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => 0;
    public override long Position { get; set; }
    public override void Flush() { }
    public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
    public override long Seek(long o, SeekOrigin r) => throw new NotSupportedException();
    public override void SetLength(long v) => throw new NotSupportedException();
    public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
}

// 2. L'handler che decide QUANDO crashare
public class SseCrashHandler(bool failImmediately) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (failImmediately)
            throw new HttpRequestException("Server non raggiungibile.");

        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NetworkFailureStream())
        };
        return Task.FromResult(response);
    }
}