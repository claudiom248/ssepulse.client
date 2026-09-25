using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client;

namespace SsePulse.Client.Tests.Source;

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
        ThrowWhenNoEventHandlerFound = false
    };

    internal static SseSource CreateSource(
        HttpClient? client = null,
        SseSourceOptions? options = null,
        IEnumerable<IRequestMutator>? mutators = null,
        ILastEventIdStore? lastEventIdStore = null) =>
        new(client ?? DefaultClient, options ?? DefaultOptions, mutators?.ToList() ?? [], lastEventIdStore, NullLogger<SseSource>.Instance);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    protected class TestEventData
    {
        public string Message { get; set; } = "";
    }
}

