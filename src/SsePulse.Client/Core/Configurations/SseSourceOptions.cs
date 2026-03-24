using SsePulse.Client.Common;
using SsePulse.Client.Common.Models;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Core.Configurations;

public class SseSourceOptions
{
    public string Path { get; set; } = "/sse";
    public int MaxDegreeOfParallelism { get; set; }  = 4;
    public NameCasePolicy DefaultEventNameCasePolicy { get; set; } = NameCasePolicy.PascalCase;
    public RetryOptions? RetryOptions { get; set; }
    public bool ThrowWhenEventHandlerNotFound { get; set; } = true;
    internal IEnumerable<IRequestMutator> RequestMutators { get; set; } = [];
}