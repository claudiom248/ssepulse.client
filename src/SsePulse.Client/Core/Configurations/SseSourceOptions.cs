using System.Text.Json;
using SsePulse.Client.Common.Models;
using SsePulse.Client.Common.NamingPolicies;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.Core.Configurations;

/// <summary>
/// Configuration options for a <see cref="SsePulse.Client.Core.SseSource"/> instance.
/// All properties default to the values defined in <see cref="SseSourceOptionsDefaults"/>.
/// </summary>
public class SseSourceOptions
{
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
    /// Gets or sets the naming case policy applied when resolving event names from type names
    /// or handler method names. Defaults to <see cref="NameCasePolicy.PascalCase"/>.
    /// </summary>
    public NameCasePolicy DefaultEventNameCasePolicy { get; set; } = SseSourceOptionsDefaults.DefaultEventNameCasePolicy;

    /// <summary>
    /// Gets or sets the retry options for connection failures.
    /// Set to <see langword="null"/> or <see cref="RetryOptions.None"/> to disable retries.
    /// Defaults to <see cref="RetryOptions.None"/>.
    /// </summary>
    public RetryOptions? ConnectionRetryOptions { get; set; } = SseSourceOptionsDefaults.DefaultRetryOptions;

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="SsePulse.Client.Core.HandlerNotFoundException"/>
    /// is thrown when an SSE event arrives with no registered handler.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ThrowWhenNoEventHandlerFound { get; set; } = SseSourceOptionsDefaults.ThrowWhenEventHandlerNotFound;

    /// <summary>
    /// Gets or sets a value indicating whether the connection loop automatically restarts
    /// after a <see cref="SsePulse.Client.Core.ResponseAbortedException"/>.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool RestartOnConnectionAbort { get; set; } = SseSourceOptionsDefaults.RestartOnConnectionAbort;


    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used to deserialize event data.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = SerializationOptions.DefaultJsonSerializerOptions;
}