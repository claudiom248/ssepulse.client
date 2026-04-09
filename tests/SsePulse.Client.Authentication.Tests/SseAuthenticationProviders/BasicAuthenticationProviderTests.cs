using System.Net.Http;
using System.Text;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers;

namespace SsePulse.Client.Authentication.Tests.SseAuthenticationProviders;

public class BasicAuthenticationProviderTests
{
    private const string DefaultUsername = "testuser";
    private const string DefaultPassword = "testpass123";

    [Fact]
    public async Task ApplyAsync_SetsAuthorizationHeader()
    {
        // ARRANGE
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
    }

    [Fact]
    public async Task ApplyAsync_EncodesCredentialsCorrectly()
    {
        // ARRANGE
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{DefaultUsername}:{DefaultPassword}"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal(expectedEncoded, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithSpecialCharactersInPassword_EncodesCorrectly()
    {
        // ARRANGE
        const string specialPassword = "p@ss:w0rd!@#$%";
        BasicCredentials credentials = new(DefaultUsername, specialPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{DefaultUsername}:{specialPassword}"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyCredentials_EncodesAsColon()
    {
        // ARRANGE
        BasicCredentials credentials = new("", "");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(":"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithUsernameOnly_EncodesWithColon()
    {
        // ARRANGE
        BasicCredentials credentials = new("user", "");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes("user:"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithPasswordOnly_EncodesWithColon()
    {
        // ARRANGE
        BasicCredentials credentials = new("", "password");
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(":password"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithLongCredentials_EncodesSuccessfully()
    {
        // ARRANGE
        const string longUsername = "this_is_a_very_long_username_with_many_characters_1234567890";
        const string longPassword = "this_is_a_very_long_password_with_many_special_chars!@#$%^&*()_+{}|:<>?";
        BasicCredentials credentials = new(longUsername, longPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        string expectedEncoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{longUsername}:{longPassword}"));

        // ACT
        await provider.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.Equal(expectedEncoded, request.Headers.Authorization?.Parameter);
    }
    
    [Fact]
    public async Task ApplyAsync_CalledOnDifferentRequests_SetsHeaderOnEach()
    {
        // ARRANGE
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request1 = new(HttpMethod.Get, "https://example.com/sse");
        HttpRequestMessage request2 = new(HttpMethod.Get, "https://example.com/sse");

        // ACT
        await provider.ApplyAsync(request1, CancellationToken.None);
        await provider.ApplyAsync(request2, CancellationToken.None);

        // ASSERT
        Assert.NotNull(request1.Headers.Authorization);
        Assert.NotNull(request2.Headers.Authorization);
        Assert.Equal(request1.Headers.Authorization.Parameter, request2.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // ARRANGE
        BasicCredentials credentials = new(DefaultUsername, DefaultPassword);
        BasicAuthenticationProvider provider = new(credentials);
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/sse");
        CancellationTokenSource cts = new();

        // ACT
        await provider.ApplyAsync(request, cts.Token);

        // ASSERT
        Assert.NotNull(request.Headers.Authorization);
    }
}