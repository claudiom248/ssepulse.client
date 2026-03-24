using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers.TokenProviders;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Tests.TokenProviders;

public class TokenProviderFactoryTests
{
    private const string TestToken = "test-token-12345";

    // --- Gruppo: Factory Creation - Static Token (3 tests) ---

    [Fact]
    public void Create_WithStaticTokenConfiguration_ReturnsDelegatingTokenProvider()
    {
        // Arrange
        StaticTokenProviderConfiguration config = new(TestToken);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DelegatingTokenProvider>(provider);
    }

    [Fact]
    public async Task Create_WithStaticToken_ReturnsTokenFromProvider()
    {
        // Arrange
        StaticTokenProviderConfiguration config = new(TestToken);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task Create_WithEmptyStaticToken_ReturnsEmptyString()
    {
        // Arrange
        StaticTokenProviderConfiguration config = new("");

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);
        string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("", token);
    }

    // --- Gruppo: Factory Creation - Client Credentials (3 tests) ---

    [Fact]
    public void Create_WithClientCredentialsConfiguration_ReturnsClientCredentialsTokenProvider()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<ClientCredentialsTokenProvider>(provider);
    }

    [Fact]
    public void Create_WithClientCredentialsConfiguration_CreatesProvider()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Create_WithEmptyClientCredentials_CreatesProvider()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("", "");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Assert
        Assert.NotNull(provider);
    }

    // --- Gruppo: Factory Creation - Environment Variable (3 tests) ---

    [Fact]
    public void Create_WithEnvironmentVariableConfiguration_ReturnsDelegatingTokenProvider()
    {
        // Arrange
        EnvironmentVariableTokenProviderConfiguration config = new("MY_TOKEN_ENV_VAR");

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DelegatingTokenProvider>(provider);
    }

    [Fact]
    public async Task Create_WithEnvironmentVariableConfiguration_ReadsFromEnvironment()
    {
        // Arrange
        const string envVarName = "TEST_SSE_TOKEN_VAR";
        const string envVarValue = "token-from-env";
        Environment.SetEnvironmentVariable(envVarName, envVarValue);
        EnvironmentVariableTokenProviderConfiguration config = new(envVarName);

        try
        {
            // Act
            ITokenProvider provider = TokenProviderFactory.Create(config);
            string token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

            // Assert
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
        // Arrange
        const string envVarName = "NONEXISTENT_ENV_VAR_" + nameof(Create_WithMissingEnvironmentVariable_ReturnsNull);
        EnvironmentVariableTokenProviderConfiguration config = new(envVarName);

        // Act
        ITokenProvider provider = TokenProviderFactory.Create(config);
        string? token = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert - environment variable doesn't exist, so token will be null
        Assert.Null(token);
    }

    // --- Gruppo: Factory Creation - Invalid Configuration (1 test) ---

    [Fact]
    public void Create_WithInvalidConfiguration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        InvalidTokenProviderConfiguration invalidConfig = new();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenProviderFactory.Create(invalidConfig));
    }

    // --- Gruppo: Factory Creation - Multiple Providers (3 tests) ---

    [Fact]
    public async Task Create_MultipleStaticConfigurations_CreateIndependentProviders()
    {
        // Arrange
        StaticTokenProviderConfiguration config1 = new("token1");
        StaticTokenProviderConfiguration config2 = new("token2");

        // Act
        ITokenProvider provider1 = TokenProviderFactory.Create(config1);
        ITokenProvider provider2 = TokenProviderFactory.Create(config2);

        string token1 = await provider1.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider2.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("token1", token1);
        Assert.Equal("token2", token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task Create_WithDifferentConfigurationTypes_CreatesCorrectProviders()
    {
        // Arrange
        StaticTokenProviderConfiguration staticConfig = new(TestToken);
        EnvironmentVariableTokenProviderConfiguration envConfig = new("ANY_VAR");

        // Act
        ITokenProvider staticProvider = TokenProviderFactory.Create(staticConfig);
        ITokenProvider envProvider = TokenProviderFactory.Create(envConfig);

        // Assert
        Assert.NotSame(staticProvider, envProvider);
        Assert.IsType<DelegatingTokenProvider>(staticProvider);
        Assert.IsType<DelegatingTokenProvider>(envProvider);
    }

    [Fact]
    public async Task Create_StaticToken_CanBeCalledMultipleTimes()
    {
        // Arrange
        StaticTokenProviderConfiguration config = new(TestToken);
        ITokenProvider provider = TokenProviderFactory.Create(config);

        // Act
        string token1 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);
        string token2 = await provider.GetAuthenticationTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TestToken, token1);
        Assert.Equal(TestToken, token2);
    }

    // --- Helper Classes ---

    private class InvalidTokenProviderConfiguration : ITokenProviderConfiguration
    {
        public string ProviderName => "Invalid";
    }
}

