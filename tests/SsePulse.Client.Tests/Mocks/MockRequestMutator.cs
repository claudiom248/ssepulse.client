using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Tests.Mocks;

public class MockRequestMutator : IRequestMutator
{
    private readonly Func<HttpRequestMessage, Task> _modifyRequest;

    public MockRequestMutator(Func<HttpRequestMessage, Task> modifyRequest)
    {
        _modifyRequest = modifyRequest;
    }

    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _modifyRequest(request);
    }
}