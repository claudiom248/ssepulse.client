using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SsePulse.Client;

namespace SsePulse.Client.Internal;

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
                        HttpStatusCode statusCode = response.StatusCode;
                        response.Dispose();
                        throw new HttpRequestException($"HTTP error occurred: {statusCode}")
                        {
                            Data =
                            {
                                ["HttpStatusCode"] = statusCode
                            }
                        };
                    }

                    await SetConnectedAsync().ConfigureAwait(false);
                    try
                    {
                        Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        return SseStream.Wrap(this, responseStream, response);
                    }
                    catch
                    {
                        response.Dispose();
                        throw;
                    }
                },
                _options.ConnectionRetryOptions ?? RetryOptions.None,
                shouldRetry: exception =>
                {
                    if (_options.IsTransientConnectionFailure is not null)
                    {
                        return _options.IsTransientConnectionFailure.Invoke(exception);
                    }
                    //Default logic to determine if an exception is due to a transient connection failure
                    if (exception is not HttpRequestException hre)
                    {
                        return false;
                    }
                    if (hre.Data.Contains("HttpStatusCode"))
                    {
                        return !_options.NonTransientStatusCodes.Contains((HttpStatusCode)hre.Data["HttpStatusCode"]!);
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
                cancellationToken: cancellationToken,
                timeProvider: _options.TimeProvider
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while establishing a connection with the SSE endpoint. Exception message: {Message}", ex.Message);
            await SetDisconnectedAsync(ex).ConfigureAwait(false);
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


    private async ValueTask SetConnectedAsync()
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 1, 0);
        if (wasConnected != 0) return;
        _logger.LogInformation("SSE connection established");
        await InvokeCallbackAsync(_handlers.OnConnectionEstablished, nameof(ConnectionHandlers.OnConnectionEstablished)).ConfigureAwait(false);
    }

    public async ValueTask SetDisconnectedAsync(Exception? exception = null)
    {
        int wasConnected = Interlocked.CompareExchange(ref _connected, 0, 1);
        if (wasConnected != 1) return;
        if (exception is null)
        {
            _logger.LogInformation("SSE connection closed gracefully");
            await InvokeCallbackAsync(_handlers.OnConnectionClosed, nameof(ConnectionHandlers.OnConnectionClosed)).ConfigureAwait(false);
        }
        else
        {
            _logger.LogError(exception, "SSE connection lost due to exception");
            await InvokeCallbackAsync(() => _handlers.OnConnectionLost.Invoke(exception), nameof(ConnectionHandlers.OnConnectionLost)).ConfigureAwait(false);
        }
    }

    private async ValueTask InvokeCallbackAsync(Func<ValueTask> callback, string callbackName)
    {
        try
        {
            await callback.Invoke().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The {Callback} callback threw an exception", callbackName);
        }
    }
}