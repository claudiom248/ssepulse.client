using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers.TokenProviders;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Tests.TokenProviders;

public class TokenProviderFactoryTests
{
    private const string TestToken = "test-token-12345";


    [Fact]
    public void Create_WithStaticTokenConfiguration_ReturnsDelegatingTokenProvider()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config = new(TestToken);

        // ACT
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ASSERT
        Assert.IsType<DelegatingTokenProvider>(provider);
    }

    [Fact]
    public async Task Create_WithStaticToken_ReturnsConfiguredToken()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config = new(TestToken);
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task Create_WithEmptyStaticToken_ReturnsEmptyString()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config = new("");
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ACT
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal("", token);
    }


    [Fact]
    public void Create_WithClientCredentialsConfiguration_ReturnsClientCredentialsTokenProvider()
    {
        // ARRANGE
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // ACT
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ASSERT
        Assert.IsType<ClientCredentialsTokenProvider>(provider);
    }

    [Fact]
    public void Create_WithEmptyClientCredentials_ReturnsClientCredentialsTokenProvider()
    {
        // ARRANGE
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("", "");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // ACT
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ASSERT
        Assert.IsType<ClientCredentialsTokenProvider>(provider);
    }


    [Fact]
    public void Create_WithEnvironmentVariableConfiguration_ReturnsDelegatingTokenProvider()
    {
        // ARRANGE
        EnvironmentVariableTokenProviderConfiguration config = new("MY_TOKEN_ENV_VAR");

        // ACT
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ASSERT
        Assert.IsType<DelegatingTokenProvider>(provider);
    }

    [Fact]
    public async Task Create_WithEnvironmentVariableConfiguration_ReadsFromEnvironment()
    {
        // ARRANGE
        const string envVarName = "TEST_SSE_TOKEN_VAR";
        const string envVarValue = "token-from-env";
        Environment.SetEnvironmentVariable(envVarName, envVarValue);
        EnvironmentVariableTokenProviderConfiguration config = new(envVarName);
        ITokenProvider provider = TokenProviderFactory.Create(config);

        try
        {
            // ACT
            string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

            // ASSERT
            Assert.Equal(envVarValue, token);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public async Task Create_WithMissingEnvironmentVariable_ReturnsNull()
    {
        // ARRANGE
        const string envVarName = "NONEXISTENT_ENV_VAR_" + nameof(Create_WithMissingEnvironmentVariable_ReturnsNull);
        EnvironmentVariableTokenProviderConfiguration config = new(envVarName);
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ACT
        string? token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Null(token);
    }


    [Fact]
    public void Create_WithInvalidConfiguration_ThrowsArgumentOutOfRangeException()
    {
        // ARRANGE
        InvalidTokenProviderConfiguration invalidConfig = new();

        // ACT & ASSERT
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenProviderFactory.Create(invalidConfig));
    }


    [Fact]
    public async Task Create_MultipleStaticConfigurations_CreateIndependentProviders()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config1 = new("token1");
        StaticTokenProviderConfiguration config2 = new("token2");
        ITokenProvider provider1 = TokenProviderFactory.Create(config1);
        ITokenProvider provider2 = TokenProviderFactory.Create(config2);

        // ACT
        string token1 = await provider1.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider2.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal("token1", token1);
        Assert.Equal("token2", token2);
    }

    [Fact]
    public async Task Create_StaticToken_CanBeCalledMultipleTimes()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config = new(TestToken);
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // ACT
        string token1 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(TestToken, token1);
        Assert.Equal(TestToken, token2);
    }


    private class InvalidTokenProviderConfiguration : ITokenProviderConfiguration
    {
        public string ProviderName => "Invalid";
    }
}
