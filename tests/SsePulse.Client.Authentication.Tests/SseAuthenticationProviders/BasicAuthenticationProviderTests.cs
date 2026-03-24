using System.Text;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class BasicAuthenticationProviderTests
{
    private const string DefaultUsername = "testuser";
    private const string DefaultPassword = "testpass123";

    // --- Gruppo: Initialization (2 tests) ---

    [Fact]
    public void Constructor_WithCredentials_CreatesInstance()
    {
        // Arrange
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);

        // Act
        BasicAuthenticationProvider provider = new(credentials);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithEmptyCredentials_CreatesInstance()
    {
        // Arrange
        BasicCredentials credentials = new("", "");

        // Act
        BasicAuthenticationProvider provider = new(credentials);

        // Assert
        Assert.NotNull(provider);
    }

    // --- Gruppo: ApplyAsync - Header Application (4 tests) ---

    [Fact]
    public async Task ApplyAsync_SetsAuthorizationHeader()
    {
        // Arrange
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
    }

    [Fact]
    public async Task ApplyAsync_EncodesCredentialsCorrectly()
    {
        // Arrange
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{DefaultUsername}:{DefaultPassword}"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal(expectedEncoded, request.Headers.Authorization.Parameter?.TrimEnd());
    }

    [Fact]
    public async Task ApplyAsync_WithSpecialCharactersInPassword_EncodesCorrectly()
    {
        // Arrange
        const string specialPassword = "p@ss:w0rd!@#$%";
        BasicCredentials credentials = new(DefaultUsername, specialPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{DefaultUsername}:{specialPassword}"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter?.TrimEnd());
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyCredentials_EncodesAsColon()
    {
        // Arrange
        BasicCredentials credentials = new("", "");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(":"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter?.TrimEnd());
    }

    // --- Gruppo: ApplyAsync - Various Credentials (3 tests) ---

    [Fact]
    public async Task ApplyAsync_WithUsernameOnly_EncodesWithColon()
    {
        // Arrange
        BasicCredentials credentials = new("user", "");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes("user:"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter?.TrimEnd());
    }

    [Fact]
    public async Task ApplyAsync_WithPasswordOnly_EncodesWithColon()
    {
        // Arrange
        BasicCredentials credentials = new("", "password");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(":password"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter?.TrimEnd());
    }

    [Fact]
    public async Task ApplyAsync_WithLongCredentials_EncodesSuccessfully()
    {
        // Arrange
        const string longUsername = "this_is_a_very_long_username_with_many_characters_1234567890";
        const string longPassword = "this_is_a_very_long_password_with_many_special_chars!@#$%^&*()_+{}|:<>?";
        BasicCredentials credentials = new(longUsername, longPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{longUsername}:{longPassword}"));

        // Act
        await provider.ApplyAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter?.TrimEnd());
    }

    // --- Gruppo: ApplyAsync - Multiple Calls (2 tests) ---

    [Fact]
    public async Task ApplyAsync_CalledMultipleTimes_SetsHeaderEachTime()
    {
        // Arrange
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // Act
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // Assert
        Assert.NotNull(request1.Headers.Authorization);
        Assert.NotNull(request2.Headers.Authorization);
        Assert.Equal(request1.Headers.Authorization.Parameter, request2.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // Act
        await provider.ApplyAsync(request, cts.Token);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
    }
}

