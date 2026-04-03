using SsePulse.Client.Authentication.Providers.TokenProviders;

namespace SsePulse.Client.Authentication.Tests.TokenProviders;

public class DelegatingTokenProviderTests
{
    private const string TestToken = "test-token-12345";
    
    [Fact]
    public async Task GetAuthenticationTokenAsync_WithSynchronousDelegate_ReturnsToken()
    {
        // ARRANGE
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(TestToken));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithConstantToken_ReturnsConstant()
    {
        // ARRANGE
        const string constantToken = "constant-token";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(constantToken));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(constantToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithEmptyToken_ReturnsEmpty()
    {
        // ARRANGE
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(""));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal("", token);
    }
    
    [Fact]
    public async Task GetAuthenticationTokenAsync_WithLongToken_ReturnsFullToken()
    {
        // ARRANGE
        const string longToken = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2g3h4i5j6k7l8m9n0o1p2q3r4s5t6u7v8w9x0y1z2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p8q9r0";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(longToken));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(longToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithSpecialCharacters_ReturnsCorrectly()
    {
        // ARRANGE
        const string specialToken = "token-with_special.chars@123!+=";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(specialToken));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(specialToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithJwtToken_ReturnsJwt()
    {
        // ARRANGE
        const string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(jwtToken));

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(jwtToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithAsyncDelegate_ReturnsToken()
    {
        // ARRANGE
        DelegatingTokenProvider provider = new(async _ =>
        {
            await Task.Delay(1);
            return TestToken;
        });

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_CalledMultipleTimes_InvokesDelegateEachTime()
    {
        // ARRANGE
        int callCount = 0;
        DelegatingTokenProvider provider = new(_ =>
        {
            callCount++;
            return new ValueTask<string>($"token-{callCount}");
        });

        // ACT
        string token1 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal("token-1", token1);
        Assert.Equal("token-2", token2);
        Assert.Equal(2, callCount);
    }
    
    [Fact]
    public async Task GetAuthenticationTokenAsync_PassesCancellationTokenToDelegate()
    {
        // ARRANGE
        CancellationToken receivedToken = CancellationToken.None;
        CancellationTokenSource cts = new();
        DelegatingTokenProvider provider = new(ct =>
        {
            receivedToken = ct;
            return new ValueTask<string>(TestToken);
        });

        // ACT
        await provider.GetAuthenticationTokenAsync(cts.Token);

        // ASSERT
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_DelegateThrows_PropagatesException()
    {
        // ARRANGE
        DelegatingTokenProvider provider = new(
            _ => throw new InvalidOperationException("Delegate failed"));

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetAuthenticationTokenAsync(CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithNonCancellingToken_DoesNotThrow()
    {
        // ARRANGE
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(TestToken));
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // ACT
        // The delegate itself controls cancellation; a simple delegate ignores the token.
        string token = await provider.GetAuthenticationTokenAsync(cts.Token);

        // ASSERT
        Assert.Equal(TestToken, token);
    }
}

