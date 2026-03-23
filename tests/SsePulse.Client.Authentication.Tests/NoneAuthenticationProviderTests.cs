using SsePulse.Client.Authentication;

namespace SsePulse.Client.Authentication.Tests;

public class NoneAuthenticationProviderTests
{
    // --- Gruppo: Initialization (1 test) ---

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        NoneAuthenticationProvider provider = new();

        // Assert
        Assert.NotNull(provider);
    }

    // --- Gruppo: ApplyAsync (2 tests) ---

    [Fact]
    public async Task ApplyAsync_DoesNotModifyRequest()
    {
        // Arrange
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(request.Headers);
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task ApplyAsync_CompletesSuccessfully()
    {
        // Arrange
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // Act
        ValueTask task = provider.ApplyAsync(request, cts.Token);

        // Assert
        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_DoesNotThrow()
    {
        // Arrange
        NoneAuthenticationProvider provider = new();
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert (should not throw)
        await provider.ApplyAsync(request, cts.Token);
    }
}

