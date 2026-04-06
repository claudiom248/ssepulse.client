using SsePulse.Client.Common.Models;
using SsePulse.Client.Common.NamingPolicies;

namespace SsePulse.Client.Core.Configurations;

public static class SseSourceOptionsDefaults
{
    public const string Path = "/sse";
    public const int MaxDegreeOfParallelism = 1;
    public const NameCasePolicy DefaultEventNameCasePolicy = NameCasePolicy.PascalCase;
    public static readonly RetryOptions DefaultRetryOptions = RetryOptions.None;
    public const bool ThrowWhenEventHandlerNotFound = false;

}