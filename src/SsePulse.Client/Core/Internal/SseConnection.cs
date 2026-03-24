using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Common.Models;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Utils;

namespace SsePulse.Client.Core.Internal;

internal class SseConnection
{
    private readonly SseSource _source;
    private readonly ILogger<SseSource> _logger;
    private readonly HttpClient _client;
    private readonly SseSourceOptions _options;
    private readonly Func<string?> _lastEventIdProvider;
    private volatile int _connected;

    public bool IsConnected => Convert.ToBoolean(_connected);

    public SseConnection(SseSource source, HttpClient client, SseSourceOptions options, ILogger<SseSource> logger,
        Func<string?> lastEventIdProvider)
    {
        _source = source;
        _logger = logger;
        _client = client;
        _options = options;
        _lastEventIdProvider = lastEventIdProvider;
    }

    public async Task<Stream> EstablishAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.Register(() => SetDisconnected());
            HttpRequestMessage request = await PrepareRequestAsync();
            return await Execute.WithRetryAsync(
                async _ =>
                {
                    HttpResponseMessage response = await SendRequestAsync(request);
                    if (!response.IsSuccessStatusCode && !IsTransientError(response.StatusCode))
                    {
                        _logger.LogError(
                            "Error while establishing a connection with SSE endpoint. Response StatusCode does not indicate success: {StatusCode}",
                            response.StatusCode);
                        throw new HttpRequestException($"HTTP error occurred: {response.StatusCode}")
                        {
                            Data =
                            {
                                ["HttpStatusCode"] = response.StatusCode
                            }
                        };
                    }

                    SetConnected();
#if NET8_0_OR_GREATER
                    Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
                    Stream responseStream = await response.Content.ReadAsStreamAsync();
#endif
                    return SseStream.Wrap(this, responseStream);
                },
                _options.RetryOptions ?? RetryOptions.None,
                shouldRetry: exception => IsTransientError(exception.Data["HttpStatusCode"] as HttpStatusCode?),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            SetDisconnected(ex);
            throw;
        }

        async Task<HttpRequestMessage> PrepareRequestAsync()
        {
            HttpRequestMessage request = new(HttpMethod.Get, _options.Path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            if (!string.IsNullOrEmpty(_lastEventIdProvider()))
            {
                _logger.LogDebug("Resuming SSE stream from Last-Event-ID: {LastEventId}", _lastEventIdProvider());
                request.Headers.TryAddWithoutValidation("Last-Event-ID", _lastEventIdProvider());
            }
            await TryApplyMutators(request);
            return request;
        }

        async Task TryApplyMutators(HttpRequestMessage request)
        {
            foreach (IRequestMutator requestMutator in _options.RequestMutators)
            {
                try
                {
                    await requestMutator.ApplyAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error while applying request mutator `{MutatorType}`. Exception message: {Message}",
                        requestMutator.GetType(), ex.Message);
                    throw;
                }
            }
        }
        
        async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            try
            {
                HttpResponseMessage response = await _client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                return response;
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre,
                    "Error while establishing a connection with SSE endpoint. Unexpected exception thrown {Message}",
                    hre.Message);
                throw;
            }
        }
    }

    private static bool IsTransientError(HttpStatusCode? statusCode)
    {
        if (statusCode is null) return false;
        return statusCode is not (
            HttpStatusCode.NotFound
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout);
    }


    private void SetConnected()
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 0, 1);
        if (wasConnected != 0) return;
        _connected = 1;
        _logger.LogInformation("SSE connection established");
        _source.OnConnectionEstablished.Invoke();
    }

    public void SetDisconnected(Exception? exception = null)
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 0, 1);
        _connected = 0;
        if (wasConnected != 1) return;
        if (exception is null)
        {
            _logger.LogInformation("SSE connection closed gracefully");
            _source.OnConnectionClosed.Invoke();
        }
        else
        {
            _logger.LogError(exception, "SSE connection lost due to exception");
            _source.OnConnectionLost.Invoke(exception);
        }
    }

    private class SseStream : Stream
    {
        private readonly SseConnection _connection;
        private readonly Stream _innerStream;

        private SseStream(SseConnection connection, Stream innerStream)
        {
            _innerStream = innerStream;
            _connection = connection;
        }

        public static SseStream Wrap(SseConnection connection, Stream innerStream) => new(connection, innerStream);

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _connection.SetDisconnected(ex);
                throw;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
    }
}