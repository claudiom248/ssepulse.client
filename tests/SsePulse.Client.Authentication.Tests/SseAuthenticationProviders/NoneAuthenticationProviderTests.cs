using SsePulse.Client.Authentication.Providers;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class NoneAuthenticationProviderTests
{

    [Fact]
    public async Task ApplyAsync_DoesNotModifyRequest()
    {
        // ARRANGE
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Empty(request.Headers);
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public void ApplyAsync_ReturnsCompletedValueTask()
    {
        // ARRANGE
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        ValueTask task = provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task ApplyAsync_WithCancelledToken_DoesNotThrow()
    {
        // ARRANGE
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();
        cts.Cancel();

        // ACT & ASSERT
        await provider.ApplyAsync(request, cts.Token);
    }
}

