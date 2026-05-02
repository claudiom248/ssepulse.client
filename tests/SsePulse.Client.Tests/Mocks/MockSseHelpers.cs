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

public class NetworkFailureStream : Stream
{
    private bool _firstRead = true;
    private Exception _exception = new IOException("Connection closed by remote host.");
    private readonly byte[] _data;

    public NetworkFailureStream(Exception? exception = null, string? data = null)
    {
        if (exception is not null)
        {
            _exception = exception;
        }
        _data = data is not null 
            ? Encoding.UTF8.GetBytes(data) 
            : "event: message\ndata: healthy\n\n"u8.ToArray();
    }
    
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
    {
        if (!_firstRead) throw _exception;
        _firstRead = false;
        _data.CopyTo(buffer);
        return Task.FromResult(_data.Length);
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

public class SseCrashHandler(bool failImmediately) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (failImmediately)
            throw new HttpRequestException("Server unreachable.");

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NetworkFailureStream())
        };
        return Task.FromResult(response);
    }
}

public class FixedStatusHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode));
}

public class CallCountingHttpMessageHandler(Func<int, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    private int _callCount;

    public int CallCount => _callCount;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => handler(Interlocked.Increment(ref _callCount));
}
