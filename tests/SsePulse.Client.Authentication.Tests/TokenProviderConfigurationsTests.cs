using SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;
using SsePulse.Client.Authentication.Common.Credentials;

namespace SsePulse.Client.Authentication.Tests;

public class TokenProviderConfigurationsTests
{
    // --- Gruppo: StaticTokenProviderConfiguration (3 tests) ---

    [Fact]
    public void StaticConfiguration_WithToken_CreatesInstance()
    {
        // Arrange
        const string token = "test-token-12345";

        // Act
        StaticTokenProviderConfiguration config = new(token);

        // Assert
        Assert.Equal(token, config.Token);
    }

    [Fact]
    public void StaticConfiguration_ProviderName_ReturnsCorrectName()
    {
        // Arrange
        StaticTokenProviderConfiguration config = new("test-token");

        // Act
        string providerName = config.ProviderName;

        // Assert
        Assert.Equal("StaticTokenProvider", providerName);
    }

    [Fact]
    public void StaticConfiguration_WithEmptyToken_CreatesInstance()
    {
        // Arrange & Act
        StaticTokenProviderConfiguration config = new("");

        // Assert
        Assert.Equal("", config.Token);
    }

    // --- Gruppo: EnvironmentVariableTokenProviderConfiguration (4 tests) ---

    [Fact]
    public void EnvironmentVariableConfiguration_WithVariableName_CreatesInstance()
    {
        // Arrange
        const string varName = "MY_ENV_VAR";

        // Act
        EnvironmentVariableTokenProviderConfiguration config = new(varName);

        // Assert
        Assert.Equal(varName, config.EnvironmentVariable);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_ProviderName_ReturnsCorrectName()
    {
        // Arrange
        EnvironmentVariableTokenProviderConfiguration config = new("MY_VAR");

        // Act
        string providerName = config.ProviderName;

        // Assert
        Assert.Equal("EnvironmentVariableTokenProvider", providerName);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_DefaultConstructor_CreatesInstance()
    {
        // Act
        EnvironmentVariableTokenProviderConfiguration config = new();

        // Assert
        Assert.Equal("", config.EnvironmentVariable);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_WithEmptyVariableName_CreatesInstance()
    {
        // Act
        EnvironmentVariableTokenProviderConfiguration config = new("");

        // Assert
        Assert.Equal("", config.EnvironmentVariable);
    }

    // --- Gruppo: ClientCredentialsTokenProviderConfiguration (4 tests) ---

    [Fact]
    public void ClientCredentialsConfiguration_WithEndpointAndCredentials_CreatesInstance()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");

        // Act
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Assert
        Assert.Equal(tokenEndpoint, config.TokenEndpoint);
        Assert.Equal("client-id", config.Credentials.ClientId);
        Assert.Equal("client-secret", config.Credentials.ClientSecret);
    }

    [Fact]
    public void ClientCredentialsConfiguration_ProviderName_ReturnsCorrectName()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Act
        string providerName = config.ProviderName;

        // Assert
        Assert.Equal("ClientCredentialsTokenProvider", providerName);
    }

    [Fact]
    public void ClientCredentialsConfiguration_WithEmptyCredentials_CreatesInstance()
    {
        // Arrange
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("", "");

        // Act
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // Assert
        Assert.Equal("", config.Credentials.ClientId);
        Assert.Equal("", config.Credentials.ClientSecret);
    }

    [Fact]
    public void ClientCredentialsConfiguration_WithDifferentEndpoints_CreatesInstances()
    {
        // Arrange
        Uri endpoint1 = new("https://auth.example.com/token");
        Uri endpoint2 = new("https://oauth.example.com/token");
        ClientCredentials credentials = new("client-id", "secret");

        // Act
        ClientCredentialsTokenProviderConfiguration config1 = new(endpoint1, credentials);
        ClientCredentialsTokenProviderConfiguration config2 = new(endpoint2, credentials);

        // Assert
        Assert.NotEqual(config1.TokenEndpoint, config2.TokenEndpoint);
    }

    // --- Gruppo: Configuration Comparison (2 tests) ---

    [Fact]
    public void StaticConfigurations_WithSameToken_AreEqual()
    {
        // Arrange
        const string token = "same-token";
        StaticTokenProviderConfiguration config1 = new(token);
        StaticTokenProviderConfiguration config2 = new(token);

        // Act & Assert
        Assert.Equal(config1, config2);
    }

    [Fact]
    public void StaticConfigurations_WithDifferentTokens_AreNotEqual()
    {
        // Arrange
        StaticTokenProviderConfiguration config1 = new("token1");
        StaticTokenProviderConfiguration config2 = new("token2");

        // Act & Assert
        Assert.NotEqual(config1, config2);
    }
}

