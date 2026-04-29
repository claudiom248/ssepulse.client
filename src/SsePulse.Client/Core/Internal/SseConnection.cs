using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Common.Extensions;
using SsePulse.Client.Common.Models;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Utils;

namespace SsePulse.Client.Core.Internal;

internal partial class SseConnection
{
    private readonly IReadOnlyCollection<IRequestMutator> _requestMutators;
    private readonly ConnectionHandlers _handlers;
    private readonly ILogger<SseSource> _logger;
    private readonly HttpClient _client;
    private readonly SseSourceOptions _options;
    private int _connected;

    public bool IsConnected => Convert.ToBoolean(_connected);

    public SseConnection(
        IReadOnlyCollection<IRequestMutator> requestMutators,
        ConnectionHandlers handlers,
        HttpClient client,
        SseSourceOptions options,
        ILogger<SseSource> logger)
    {
        _requestMutators = requestMutators;
        _handlers = handlers;
        _logger = logger;
        _client = client;
        _options = options;
    }

    public async Task<Stream> EstablishAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Establishing a connection with the SSE endpoint...");
            return await Execute.WithRetryAsync(
                async _ =>
                {
                    HttpRequestMessage request = await PrepareRequestAsync().ConfigureAwait(false);
                    HttpResponseMessage response = await SendRequestAsync(request).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
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
                    Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                    Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                    return SseStream.Wrap(this, responseStream);
                },
                _options.ConnectionRetryOptions ?? RetryOptions.None,
                shouldRetry: exception =>
                {
                    if (exception is not HttpRequestException hre)
                    {
                        return false;
                    }
                    if (hre.Data.Count > 0)
                    {
                        return IsTransientHttpError(hre.Data["HttpStatusCode"] as HttpStatusCode?);
                    }
                    SocketException? socketException = hre.FindInner<SocketException>();
                    if (socketException is not null)
                    {
                        return socketException.SocketErrorCode is SocketError.TimedOut
                            or SocketError.ConnectionRefused
                            or SocketError.ConnectionReset;
                    }
                    return hre.FindInner<TimeoutException>() is not null;
                },
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while establishing a connection with the SSE endpoint. Exception message: {Message}", ex.Message);
            SetDisconnected(ex);
            throw;
        }

        async Task<HttpRequestMessage> PrepareRequestAsync()
        {
            HttpRequestMessage request = new(HttpMethod.Get, _options.Path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            await TryApplyMutators(request).ConfigureAwait(false);
            return request;
        }

        async Task TryApplyMutators(HttpRequestMessage request)
        {
            _logger.LogDebug("Applying request mutators...");
            foreach (IRequestMutator requestMutator in _requestMutators)
            {
                try
                {
                    await requestMutator.ApplyAsync(request, cancellationToken).ConfigureAwait(false);
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
                    cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre,
                    "Error while establishing a connection with the SSE endpoint. Exception message: {Message}",
                    hre.Message);
                throw;
            }
        }
    }

    private static bool IsTransientHttpError(HttpStatusCode? statusCode)
    {
        if (statusCode is null) return false;
        return statusCode is not (
            HttpStatusCode.NotFound
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden);
    }


    private void SetConnected()
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 1, 0);
        if (wasConnected != 0) return;
        _logger.LogInformation("SSE connection established");
        _handlers.OnConnectionEstablished.Invoke();
    }

    public void SetDisconnected(Exception? exception = null)
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 0, 1);
        if (wasConnected != 1) return;
        if (exception is null)
        {
            _logger.LogInformation("SSE connection closed gracefully");
            _handlers.OnConnectionClosed.Invoke();
        }
        else
        {
            _logger.LogError(exception, "SSE connection lost due to exception");
            _handlers.OnConnectionLost.Invoke(exception);
        }
    }
}