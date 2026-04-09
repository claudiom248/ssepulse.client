using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.Tests.SseSource;

public abstract class SseSourceTestBase
{
    protected const int DefaultCancellationTokenDelay = 500;

    private static readonly HttpClient DefaultClient = new()
    {
        BaseAddress = new Uri("https://example.com")
    };

    private static readonly SseSourceOptions DefaultOptions = new()
    {
        Path = "/sse",
        MaxDegreeOfParallelism = 1,
        ThrowWhenEventHandlerNotFound = false
    };

    internal static Core.SseSource CreateSource(
        HttpClient? client = null,
        SseSourceOptions? options = null,
        IEnumerable<IRequestMutator>? mutators = null,
        ILastEventIdStore? lastEventIdStore = null) =>
        new(client ?? DefaultClient, options ?? DefaultOptions, mutators?.ToList() ?? [], lastEventIdStore, NullLogger<Core.SseSource>.Instance);

#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    protected class TestEventData
    {
        public string Message { get; set; } = "";
    }
}

