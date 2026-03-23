using SsePulse.Client.Authentication.Bearer.TokenProviders;
using SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Tests;

public class DelegatingTokenProviderTests
{
    private const string TestToken = "test-token-12345";

    // --- Gruppo: Initialization (2 tests) ---

    [Fact]
    public void Constructor_WithDelegate_CreatesInstance()
    {
        // Arrange
        ValueTask<string> TokenDelegate(CancellationToken _) => new ValueTask<string>(TestToken);

        // Act
        DelegatingTokenProvider provider = new(TokenDelegate);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithAsyncDelegate_CreatesInstance()
    {
        // Arrange
        async ValueTask<string> AsyncDelegate(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return TestToken;
        }

        // Act
        DelegatingTokenProvider provider = new(AsyncDelegate);

        // Assert
        Assert.NotNull(provider);
    }

    // --- Gruppo: GetAuthenticationTokenAsync - Synchronous Delegates (3 tests) ---

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithSynchronousDelegate_ReturnsToken()
    {
        // Arrange
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(TestToken));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithConstantToken_ReturnsConstant()
    {
        // Arrange
        const string constantToken = "constant-token";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(constantToken));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(constantToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithEmptyToken_ReturnsEmpty()
    {
        // Arrange
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(""));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("", token);
    }

    // --- Gruppo: GetAuthenticationTokenAsync - Various Tokens (3 tests) ---

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithLongToken_ReturnsFullToken()
    {
        // Arrange
        const string longToken = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2g3h4i5j6k7l8m9n0o1p2q3r4s5t6u7v8w9x0y1z2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p8q9r0";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(longToken));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(longToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithSpecialCharacters_ReturnsCorrectly()
    {
        // Arrange
        const string specialToken = "token-with_special.chars@123!+=";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(specialToken));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(specialToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithJwtToken_ReturnsJwt()
    {
        // Arrange
        const string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(jwtToken));

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(jwtToken, token);
    }

    // --- Gruppo: GetAuthenticationTokenAsync - Async Delegates (3 tests) ---

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithAsyncDelegate_ReturnsToken()
    {
        // Arrange
        DelegatingTokenProvider provider = new(async _ =>
        {
            await Task.Delay(1);
            return TestToken;
        });

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithAsyncDelegateWithDelay_ReturnsToken()
    {
        // Arrange
        DelegatingTokenProvider provider = new(async _ =>
        {
            await Task.Delay(10);
            return TestToken;
        });

        // Act
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_CalledMultipleTimes_ReturnsTokenEachTime()
    {
        // Arrange
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(TestToken));

        // Act
        string token1 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token3 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token1);
        Assert.Equal(TestToken, token2);
        Assert.Equal(TestToken, token3);
    }

    // --- Gruppo: GetAuthenticationTokenAsync - Delegate State (2 tests) ---

    [Fact]
    public async Task GetAuthenticationTokenAsync_WithStatefulDelegate_MaintainsState()
    {
        // Arrange
        int callCount = 0;
        DelegatingTokenProvider provider = new(_ =>
        {
            callCount++;
            return new ValueTask<string>($"token-{callCount}");
        });

        // Act
        string token1 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("token-1", token1);
        Assert.Equal("token-2", token2);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetAuthenticationTokenAsync_IgnoresCancellationToken()
    {
        // Arrange
        DelegatingTokenProvider provider = new(_ => new ValueTask<string>(TestToken));
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // Act & Assert - should not throw despite cancelled token
        // (the delegate itself controls cancellation behavior)
        string token = await provider.GetAuthenticationTokenAsync(cts.Token);
        Assert.Equal(TestToken, token);
    }
}

