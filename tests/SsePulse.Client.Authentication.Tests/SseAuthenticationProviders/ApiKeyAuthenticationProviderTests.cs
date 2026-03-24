using SsePulse.Client.Authentication.Providers;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class ApiKeyAuthenticationProviderTests
{
    private const string DefaultApiKey = "test-api-key-12345";
    private const string DefaultHeaderKey = "X-Api-Key";

    // --- Gruppo: Initialization (3 tests) ---

    [Fact]
    public void Constructor_WithConfiguration_CreatesInstance()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey);

        // Act
        ApiKeyAuthenticationProvider provider = new(config);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithCustomHeaderKey_CreatesInstance()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey, "Authorization");

        // Act
        ApiKeyAuthenticationProvider provider = new(config);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_CreatesInstance()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new("");

        // Act
        ApiKeyAuthenticationProvider provider = new(config);

        // Assert
        Assert.NotNull(provider);
    }

    // --- Gruppo: ApplyAsync - Default Header Key (3 tests) ---

    [Fact]
    public async Task ApplyAsync_AddsApiKeyHeader_WithDefaultKey()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal(DefaultApiKey, request.Headers.GetValues(DefaultHeaderKey).First());
    }

    [Fact]
    public async Task ApplyAsync_PreservesExistingHeaders()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        request.Headers.Add("User-Agent", "CustomAgent/1.0");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.True(request.Headers.Contains("User-Agent"));
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal("CustomAgent/1.0", request.Headers.GetValues("User-Agent").First());
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyApiKey_AddsHeaderWithEmptyValue()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new("");
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
        Assert.Equal("", request.Headers.GetValues(DefaultHeaderKey).First());
    }

    // --- Gruppo: ApplyAsync - Custom Header Key (3 tests) ---

    [Fact]
    public async Task ApplyAsync_AddsApiKeyHeader_WithCustomKey()
    {
        // Arrange
        const string customHeaderKey = "Authorization";
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey, customHeaderKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.True(request.Headers.Contains(customHeaderKey));
        Assert.Equal(DefaultApiKey, request.Headers.GetValues(customHeaderKey).First());
    }

    [Fact]
    public async Task ApplyAsync_WithCustomHeader_DoesNotAddDefaultHeader()
    {
        // Arrange
        const string customHeaderKey = "X-Custom-Key";
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey, customHeaderKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.False(request.Headers.Contains(DefaultHeaderKey));
        Assert.True(request.Headers.Contains(customHeaderKey));
    }

    [Fact]
    public async Task ApplyAsync_WithSpecialCharactersInKey_AddsHeaderCorrectly()
    {
        // Arrange
        const string apiKeyWithSpecialChars = "key-with_special.chars@123!";
        ApiKeyAuthenticationProviderConfiguration config = new(apiKeyWithSpecialChars);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(apiKeyWithSpecialChars, request.Headers.GetValues(DefaultHeaderKey).First());
    }

    // --- Gruppo: ApplyAsync - Multiple Calls (2 tests) ---

    [Fact]
    public async Task ApplyAsync_CalledMultipleTimes_AddsHeaderEachTime()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // Assert
        Assert.True(request1.Headers.Contains(DefaultHeaderKey));
        Assert.True(request2.Headers.Contains(DefaultHeaderKey));
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        ApiKeyAuthenticationProviderConfiguration config = new(DefaultApiKey);
        ApiKeyAuthenticationProvider provider = new(config);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // Act
        await provider.ApplyAsync(request, cts.Token);

        // Assert
        Assert.True(request.Headers.Contains(DefaultHeaderKey));
    }
}

