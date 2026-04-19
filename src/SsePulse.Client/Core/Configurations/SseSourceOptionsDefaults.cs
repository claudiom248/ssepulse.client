using SsePulse.Client.Common.Models;
using SsePulse.Client.Common.NamingPolicies;

namespace SsePulse.Client.Core.Configurations;

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

    /// <summary>Default event name case policy: <see cref="NameCasePolicy.PascalCase"/>.</summary>
    public const NameCasePolicy DefaultEventNameCasePolicy = NameCasePolicy.PascalCase;

    /// <summary>Default connection retry options: <see cref="RetryOptions.None"/> (no retries).</summary>
    public static readonly RetryOptions DefaultRetryOptions = RetryOptions.None;

    /// <summary>Default value for throwing when no event handler is found: <see langword="false"/>.</summary>
    public const bool ThrowWhenEventHandlerNotFound = false;

    /// <summary>Default value for restarting on connection abort: <see langword="true"/>.</summary>
    public const bool RestartOnConnectionAbort = true;
}