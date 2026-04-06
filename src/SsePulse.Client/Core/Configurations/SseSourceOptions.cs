using SsePulse.Client.Common.Models;
using SsePulse.Client.Common.NamingPolicies;

namespace SsePulse.Client.Core.Configurations;

public class SseSourceOptions
{
    public string Path { get; set; } = SseSourceOptionsDefaults.Path;
    public int MaxDegreeOfParallelism { get; set; }  = SseSourceOptionsDefaults.MaxDegreeOfParallelism;
    public NameCasePolicy DefaultEventNameCasePolicy { get; set; } = SseSourceOptionsDefaults.DefaultEventNameCasePolicy;
    public RetryOptions? RetryOptions { get; set; } = SseSourceOptionsDefaults.DefaultRetryOptions;
    public bool ThrowWhenEventHandlerNotFound { get; set; } = SseSourceOptionsDefaults.ThrowWhenEventHandlerNotFound;
}