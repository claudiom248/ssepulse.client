using System.Net.Http;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.TokenProviders;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class BearerTokenAuthenticationProviderTests
{
    private const string TestToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
    
    [Fact]
    public async Task ApplyAsync_SetsAuthorizationHeader_WithBearerScheme()
    {
        // ARRANGE
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(TestToken));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
    }

    [Fact]
    public async Task ApplyAsync_SetsTokenAsParameter()
    {
        // ARRANGE
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(TestToken));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(TestToken, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithSimpleToken_SetsCorrectly()
    {
        // ARRANGE
        const string simpleToken = "simple-token-123";
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(simpleToken));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(simpleToken, request.Headers.Authorization?.Parameter);
    }
    
    [Fact]
    public async Task ApplyAsync_WithEmptyToken_SetsEmptyParameter()
    {
        // ARRANGE
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(""));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithLongToken_SetsCorrectly()
    {
        // ARRANGE
        const string longToken = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2g3h4i5j6k7l8m9n0o1p2q3r4s5t6u7v8w9x0y1z2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p8q9r0";
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(longToken));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(longToken, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithSpecialCharactersInToken_SetsCorrectly()
    {
        // ARRANGE
        const string tokenWithSpecialChars = "token-with_special.chars@123!+=";
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ => new ValueTask<string>(tokenWithSpecialChars));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(tokenWithSpecialChars, request.Headers.Authorization?.Parameter);
    }


    [Fact]
    public async Task ApplyAsync_InvokesTokenProvider()
    {
        // ARRANGE
        bool providerInvoked = false;
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ =>
        {
            providerInvoked = true;
            return new ValueTask<string>("test-token");
        });
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(providerInvoked);
    }

    [Fact]
    public async Task ApplyAsync_InvokesTokenProviderForEachRequest()
    {
        // ARRANGE
        int invocationCount = 0;
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ =>
        {
            invocationCount++;
            return new ValueTask<string>($"token-{invocationCount}");
        });
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // ASSERT
        Assert.Equal(2, invocationCount);
        Assert.Equal("token-1", request1.Headers.Authorization?.Parameter);
        Assert.Equal("token-2", request2.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_PassesCancellationTokenToProvider()
    {
        // ARRANGE
        CancellationToken receivedToken = CancellationToken.None;
        ITokenProvider tokenProvider = new MockTokenProvider(async ct =>
        {
            receivedToken = ct;
            return await Task.FromResult("test-token");
        });
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // ACT
        await provider.ApplyAsync(request, cts.Token);

        // ASSERT
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenProviderThrows_PropagatesException()
    {
        // ARRANGE
        ITokenProvider tokenProvider = new DelegatingTokenProvider(
            _ => throw new InvalidOperationException("Token fetch failed"));
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ApplyAsync(request, CancellationToken.None).AsTask());
    }
    
    [Fact]
    public async Task ApplyAsync_CalledOnDifferentRequests_SetsTokenFromProviderEachTime()
    {
        // ARRANGE
        int callCount = 0;
        ITokenProvider tokenProvider = new DelegatingTokenProvider(_ =>
        {
            callCount++;
            return new ValueTask<string>($"token-{callCount}");
        });
        BearerTokenAuthenticationProvider provider = new(tokenProvider);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // ASSERT
        Assert.Equal("token-1", request1.Headers.Authorization?.Parameter);
        Assert.Equal("token-2", request2.Headers.Authorization?.Parameter);
    }

    // --- Helper Class ---

    private class MockTokenProvider : ITokenProvider
    {
        private readonly Func<CancellationToken, Task<string>> _getToken;

        public MockTokenProvider(Func<CancellationToken, Task<string>> getToken)
        {
            _getToken = getToken;
        }

        public async ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken)
        {
            return await _getToken(cancellationToken);
        }
    }
}

