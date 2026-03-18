using SsePulse.Common;
using SsePulse.Utils;

namespace SsePulse;

public class SseSourceOptions
{
    public string Path { get; set; } = "/sse";
    public int MaxDegreeOfParallelism { get; set; }  = 4;
    public NameCasePolicy DefaultEventNameCasePolicy { get; set; } = NameCasePolicy.PascalCase;
    public RetryOptions? RetryOptions { get; set; }
}