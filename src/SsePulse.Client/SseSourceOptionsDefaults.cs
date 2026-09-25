using System.Net;
using SsePulse.Client.Internal;

namespace SsePulse.Client;

/// <summary>
/// Contains the default values used by <see cref="SseSourceOptions"/> when no explicit
/// configuration is provided.
/// </summary>
public static class SseSourceOptionsDefaults
{
    /// <summary>Default SSE endpoint path: <c>/sse</c>.</summary>
    public const string Path = "/sse";

    /// <summary>Default maximum degree of parallelism for event handler execution: <c>1</c>.</summary>
    public const int MaxDegreeOfParallelism = 1;

    /// <summary>Default maximum number of received events queued for a handler: <c>1024</c>.</summary>
    public const int MaxBufferedEvents = 1024;

    /// <summary>Default event name case policy: <see cref="NameCasePolicy.PascalCase"/>.</summary>
    public const NameCasePolicy DefaultEventNameCasePolicy = NameCasePolicy.PascalCase;

    /// <summary>Default connection retry options: <see cref="RetryOptions.Default"/>.</summary>
    public static readonly RetryOptions DefaultRetryOptions = RetryOptions.Default;

    /// <summary>
    /// Creates the default collection of HTTP status codes that are retried during the connection phase:
    /// <c>408</c>, <c>425</c>, <c>429</c>, <c>500</c>, <c>502</c>, <c>503</c> and <c>504</c>.
    /// </summary>
    public static ICollection<HttpStatusCode> DefaultTransientStatusCodes() =>
    [
        HttpStatusCode.RequestTimeout, (HttpStatusCode)425, HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    /// <summary>Default value for throwing when no event handler is found: <see langword="false"/>.</summary>
    public const bool ThrowWhenEventHandlerNotFound = false;

    /// <summary>Default behavior when an event handler throws: <see cref="HandlerFailureBehavior.SkipAndAdvance"/>.</summary>
    public const HandlerFailureBehavior DefaultHandlerFailureBehavior = HandlerFailureBehavior.SkipAndAdvance;

    /// <summary>Default value for restarting on connection abort: <see langword="true"/>.</summary>
    public const bool RestartOnConnectionAbort = true;
}