using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.Configurations;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class ApiKeyAuthenticationProviderTests
{
    private const string DefaultApiKey = "test-api-key-12345";
    private const string DefaultHeaderKey = "X-API-Key";

    [Fact]
    public async Task ApplyAsync_AddsApiKeyHeader_WithDefaultKey()
    {
        // ARRANGE
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal(DefaultApiKey, request.Headers.GetValues(DefaultHeaderKey).First());
    }

    [Fact]
    public async Task ApplyAsync_PreservesExistingHeaders()
    {
        // ARRANGE
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        request.Headers.Add("User-Agent", "CustomAgent/1.0");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.Contains("User-Agent"));
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal("CustomAgent/1.0", request.Headers.GetValues("User-Agent").First());
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyApiKey_AddsHeaderWithEmptyValue()
    {
        // ARRANGE
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = ""
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal("", request.Headers.GetValues(DefaultHeaderKey).First());
    }
    
    [Fact]
    public async Task ApplyAsync_AddsApiKeyHeader_WithCustomKey()
    {
        // ARRANGE
        const string customHeaderKey = "Authorization";
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey,
            Header = customHeaderKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.Contains(customHeaderKey));
        Assert.Equal(DefaultApiKey, request.Headers.GetValues(customHeaderKey).First());
    }

    [Fact]
    public async Task ApplyAsync_WithCustomHeader_DoesNotAddDefaultHeader()
    {
        // ARRANGE
        const string customHeaderKey = "X-Custom-Key";
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey,
            Header = customHeaderKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.False(request.Headers.Contains(DefaultHeaderKey));
        Assert.True(request.Headers.Contains(customHeaderKey));
    }

    [Fact]
    public async Task ApplyAsync_WithSpecialCharactersInKey_AddsHeaderCorrectly()
    {
        // ARRANGE
        const string apiKeyWithSpecialChars = "key-with_special.chars@123!";
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = apiKeyWithSpecialChars
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(apiKeyWithSpecialChars, request.Headers.GetValues(DefaultHeaderKey).First());
    }
    
    [Fact]
    public async Task ApplyAsync_CalledOnDifferentRequests_AddsHeaderToEach()
    {
        // ARRANGE
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // ASSERT
        Assert.Equal(DefaultApiKey, request1.Headers.GetValues(DefaultHeaderKey).First());
        Assert.Equal(DefaultApiKey, request2.Headers.GetValues(DefaultHeaderKey).First());
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // ARRANGE
        ApiKeyAuthenticationProviderConfiguration config = new()
        {
            Key = DefaultApiKey
        };
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // ACT
        await provider.ApplyAsync(request, cts.Token);

        // ASSERT
        Assert.Equal(DefaultApiKey, request.Headers.GetValues(DefaultHeaderKey).First());
    }
}

