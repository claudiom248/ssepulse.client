using System.Net.ServerSentEvents;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SsePulse.Client;

namespace SsePulse.Client.Internal;

internal class StreamConsumer
{
    private const string? ResponseAbortedMessage =
        "SSE stream ended prematurely. This may indicate that the server closed the connection unexpectedly.";

    private readonly SseHandlersDictionary _handlers;
    private readonly SseSourceOptions _options;
    private readonly ILogger<SseSource> _logger;
    private readonly Action<Exception> _onError;
    private readonly ILastEventIdStore? _lastEventIdStore;
    private readonly EventIdCommitTracker _commitTracker = new();

    public StreamConsumer(
        SseHandlersDictionary handlers,
        SseSourceOptions options,
        ILogger<SseSource> logger,
        Action<Exception> onError,
        ILastEventIdStore? lastEventIdStore = null)
    {
        _handlers = handlers;
        _options = options;
        _logger = logger;
        _onError = onError;
        _lastEventIdStore = lastEventIdStore;
    }

    public async Task ConsumeAsync(Stream stream, CancellationToken cancellationToken)
    {
        using CancellationTokenSource faultSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<DispatchItem> channel = Channel.CreateBounded<DispatchItem>(
            new BoundedChannelOptions(_options.MaxBufferedEvents)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = _options.MaxDegreeOfParallelism == 1
            });
        ExceptionDispatchInfo? fault = null;
        Task[] workers = Enumerable
            .Range(0, _options.MaxDegreeOfParallelism)
            .Select(_ => Task.Run(RunWorkerAsync, CancellationToken.None))
            .ToArray();
        SseParser<string> parser = SseParser.Create(stream);
        try
        {
            await foreach (SseItem<string> sseItem in parser.EnumerateAsync(faultSource.Token).ConfigureAwait(false))
            {
                using IDisposable? _ = _logger.BeginScope("EventType: {EventType}", sseItem.EventType);
                _logger.LogDebug("Received event of type '{EventType}' and Data {Data}", sseItem.EventType,
                    sseItem.Data);
                long sequence = _commitTracker.Register(sseItem.EventId);
                await channel.Writer.WriteAsync(new DispatchItem(sequence, sseItem), faultSource.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (HttpIOException ioEx) when (ioEx.HttpRequestError == HttpRequestError.ResponseEnded)
        {
            _logger.LogError(ioEx, ResponseAbortedMessage);
            throw new ResponseAbortedException(ioEx);
        }
        catch (IOException hre) when (hre.FindInner<SocketException>() is
                                          { SocketErrorCode: SocketError.ConnectionReset })
        {
            _logger.LogError(hre, ResponseAbortedMessage);
            throw new ResponseAbortedException(hre);
        }
        catch (Exception ex) when (IsResponseAborted(ex))
        {
            throw new ResponseAbortedException(ex);
        }
        finally
        {
            channel.Writer.TryComplete();
            await Task.WhenAll(workers).ConfigureAwait(false);
            fault?.Throw();
        }

        return;

        async Task RunWorkerAsync()
        {
            try
            {
                await foreach (DispatchItem item in channel.Reader.ReadAllAsync(faultSource.Token).ConfigureAwait(false))
                {
                    await DispatchAsync(item, faultSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogTrace("A handler failed. Stopping the processing of incoming events.");
                Interlocked.CompareExchange(ref fault, ExceptionDispatchInfo.Capture(ex), null);
                faultSource.Cancel();
            }
        }
    }    
    private bool IsResponseAborted(Exception ex)
    {
        if (_options.IsResponseAborted?.Invoke(ex) == true)
        {
            return true;
        }

        switch (ex)
        {
            case HttpIOException { HttpRequestError: HttpRequestError.ResponseEnded }:
            case IOException hre when 
                hre.FindInner<SocketException>() is { SocketErrorCode: SocketError.ConnectionReset }:
                return true;
            default:
                return false;
        } }

    private async Task DispatchAsync(DispatchItem item, CancellationToken cancellationToken)
    {
        SseItem<string> @event = item.Event;
        string eventType = @event.EventType;
        if (!_handlers.TryGetValue(eventType, out List<ISseEventHandler>? eventHandlers))
        {
            if (_options.ThrowWhenNoEventHandlerFound)
            {
                _logger.LogError("No handler found for event type '{EventType}'", eventType);
                throw new HandlerNotFoundException(eventType);
            }

            _logger.LogWarning("No handler found for event type '{EventType}'", eventType);
            Commit(item.Sequence);
            return;
        }

        try
        {
            foreach (ISseEventHandler eventHandler in eventHandlers)
            {
                await eventHandler.InvokeAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event '{EventType}'", eventType);
            _onError(ex);
        }

        Commit(item.Sequence);
    }

    private void Commit(long sequence)
    {
        if (_lastEventIdStore is null)
        {
            return;
        }

        _commitTracker.Complete(sequence, eventId =>
        {
            _logger.LogDebug("Set last event ID to '{EventId}'", eventId);
            _lastEventIdStore.Set(eventId);
        });
    }

    private readonly record struct DispatchItem(long Sequence, SseItem<string> Event);
}