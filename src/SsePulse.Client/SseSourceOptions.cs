using System.Net;
using System.Text.Json;
using SsePulse.Client.Internal;
using SsePulse.Client.Serialization;

namespace SsePulse.Client;

/// <summary>
/// Configuration options for a <see cref="SsePulse.Client.SseSource"/> instance.
/// All properties default to the values defined in <see cref="SseSourceOptionsDefaults"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/configuration.html"/>
/// </summary>
public class SseSourceOptions
{
    /// <summary>
    /// Gets or sets the name of the SSE source. Defaults to a new GUID.
    /// </summary>
    public string Name { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the relative or absolute URL path of the SSE endpoint.
    /// Defaults to <c>/sse</c>.
    /// </summary>
    public string Path { get; set; } = SseSourceOptionsDefaults.Path;

    /// <summary>
    /// Gets or sets the maximum number of event handlers that may execute concurrently.
    /// Defaults to <c>1</c> (sequential processing).
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = SseSourceOptionsDefaults.MaxDegreeOfParallelism;

    /// <summary>
    /// Gets or sets the maximum number of received events that may be queued waiting for an event handler,
    /// in addition to the events currently being handled (see <see cref="MaxDegreeOfParallelism"/>).
    /// When the queue is full the stream is no longer read until a handler completes, which applies
    /// back-pressure to the server. Must be greater than zero. Defaults to <c>1024</c>.
    /// </summary>
    public int MaxBufferedEvents { get; set; } = SseSourceOptionsDefaults.MaxBufferedEvents;

    /// <summary>
    /// Gets or sets the naming case policy applied when resolving event names from type names
    /// or handler method names. Defaults to <see cref="NameCasePolicy.PascalCase"/>.
    /// </summary>
    public NameCasePolicy DefaultEventNameCasePolicy { get; set; } =
        SseSourceOptionsDefaults.DefaultEventNameCasePolicy;

    /// <summary>
    /// Gets or sets the retry options for connection failures.
    /// Set to <see langword="null"/> or <see cref="RetryOptions.None"/> to disable retries.
    /// Defaults to <see cref="RetryOptions.Default"/>.
    /// </summary>
    public RetryOptions? ConnectionRetryOptions { get; set; } = SseSourceOptionsDefaults.DefaultRetryOptions;

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="SsePulse.Client.HandlerNotFoundException"/>
    /// is thrown when an SSE event arrives with no registered handler.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ThrowWhenNoEventHandlerFound { get; set; } = SseSourceOptionsDefaults.ThrowWhenEventHandlerNotFound;

    /// <summary>
    /// Gets or sets what the source does when an event handler throws.
    /// Defaults to <see cref="SsePulse.Client.HandlerFailureBehavior.SkipAndAdvance"/>.
    /// </summary>
    public HandlerFailureBehavior HandlerFailureBehavior { get; set; } = SseSourceOptionsDefaults.DefaultHandlerFailureBehavior;

    /// <summary>
    /// Gets or sets a value indicating whether the connection loop automatically restarts
    /// after a <see cref="SsePulse.Client.ResponseAbortedException"/>.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool RestartOnConnectionAbort { get; set; } = SseSourceOptionsDefaults.RestartOnConnectionAbort;

    /// <summary>
    /// Gets or sets the collection of HTTP status codes that are considered transient failures during the connection
    /// phase, when the server returns a response.
    /// </summary>
    /// <remarks>
    /// When the server responds with a status code included in this collection, the source retries the connection
    /// according to <see cref="ConnectionRetryOptions"/>. Any other status code fails immediately, without a retry.
    /// Defaults to <c>408</c>, <c>425</c>, <c>429</c>, <c>500</c>, <c>502</c>, <c>503</c> and <c>504</c>.
    /// <see cref="IsTransientConnectionFailure"/> takes precedence over this collection.
    /// </remarks>
    public ICollection<HttpStatusCode> TransientStatusCodes { get; set; } = SseSourceOptionsDefaults.DefaultTransientStatusCodes();

    /// <summary>
    /// Gets or sets a predicate to determine whether an exception is considered transient during the connection phase.
    /// </summary>
    public Predicate<Exception>? IsTransientConnectionFailure { get; set; }

    /// <summary>
    /// Gets or sets a predicate to determine whether an exception denotes a connection abort.
    /// </summary>
    /// <remarks>
    /// When this predicate is set and returns <see langword="true"/> for a given exception and
    /// <see cref="RestartOnConnectionAbort"/> is <see langword="true"/>, the source will retry to connect.
    /// </remarks>
    public Predicate<Exception>? IsResponseAborted { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used to deserialize event data.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } =
        SerializationOptions.DefaultJsonSerializerOptions;

    /// <summary>
    /// Gets or sets the <see cref="System.TimeProvider"/> used for delays between connection attempts.
    /// Defaults to <see cref="System.TimeProvider.System"/>.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}