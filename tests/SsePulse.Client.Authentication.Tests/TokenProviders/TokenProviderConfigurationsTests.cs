using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Tests.TokenProviders;

public class TokenProviderConfigurationsTests
{
    [Fact]
    public void StaticConfiguration_StoresToken()
    {
        // ARRANGE
        const string token = "test-token-12345";

        // ACT
        StaticTokenProviderConfiguration config = new(token);

        // ASSERT
        Assert.Equal(token, config.Token);
    }

    [Fact]
    public void StaticConfiguration_ProviderName_ReturnsCorrectName()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config = new("test-token");

        // ACT
        string providerName = config.Provider;

        // ASSERT
        Assert.Equal(Constants.StaticTokenProviderName, providerName);
    }

    [Fact]
    public void StaticConfiguration_WithEmptyToken_StoresEmptyString()
    {
        // ARRANGE
        const string token = "";

        // ACT
        StaticTokenProviderConfiguration config = new(token);

        // ASSERT
        Assert.Equal("", config.Token);
    }
    
    [Fact]
    public void EnvironmentVariableConfiguration_StoresVariableName()
    {
        // ARRANGE
        const string varName = "MY_ENV_VAR";

        // ACT
        EnvironmentVariableTokenProviderConfiguration config = new(varName);

        // ASSERT
        Assert.Equal(varName, config.EnvironmentVariable);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_ProviderName_ReturnsCorrectName()
    {
        // ARRANGE
        EnvironmentVariableTokenProviderConfiguration config = new("MY_VAR");

        // ACT
        string providerName = config.Provider;

        // ASSERT
        Assert.Equal(Constants.EnvironmentVariableTokenProviderName, providerName);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_DefaultConstructor_StoresEmptyString()
    {
        // ACT
        EnvironmentVariableTokenProviderConfiguration config = new();

        // ASSERT
        Assert.Equal("", config.EnvironmentVariable);
    }

    [Fact]
    public void EnvironmentVariableConfiguration_WithEmptyVariableName_StoresEmptyString()
    {
        // ACT
        EnvironmentVariableTokenProviderConfiguration config = new("");

        // ASSERT
        Assert.Equal("", config.EnvironmentVariable);
    }

    [Fact]
    public void ClientCredentialsConfiguration_StoresEndpointAndCredentials()
    {
        // ARRANGE
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");

        // ACT
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // ASSERT
        Assert.Equal(tokenEndpoint, config.TokenEndpoint);
        Assert.Equal("client-id", config.Credentials.ClientId);
        Assert.Equal("client-secret", config.Credentials.ClientSecret);
    }

    [Fact]
    public void ClientCredentialsConfiguration_ProviderName_ReturnsCorrectName()
    {
        // ARRANGE
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("client-id", "client-secret");

        // ACT
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // ASSERT
        Assert.Equal(Constants.ClientCredentialsTokenProviderName, config.Provider);
    }

    [Fact]
    public void ClientCredentialsConfiguration_WithEmptyCredentials_StoresEmpty()
    {
        // ARRANGE
        Uri tokenEndpoint = new("https://auth.example.com/token");
        ClientCredentials credentials = new("", "");

        // ACT
        ClientCredentialsTokenProviderConfiguration config = new(tokenEndpoint, credentials);

        // ASSERT
        Assert.Equal("", config.Credentials.ClientId);
        Assert.Equal("", config.Credentials.ClientSecret);
    }

    [Fact]
    public void ClientCredentialsConfiguration_DifferentEndpoints_StoredIndependently()
    {
        // ARRANGE
        Uri endpoint1 = new("https://auth.example.com/token");
        Uri endpoint2 = new("https://oauth.example.com/token");
        ClientCredentials credentials = new("client-id", "secret");

        // ACT
        ClientCredentialsTokenProviderConfiguration config1 = new(endpoint1, credentials);
        ClientCredentialsTokenProviderConfiguration config2 = new(endpoint2, credentials);

        // ASSERT
        Assert.NotEqual(config1.TokenEndpoint, config2.TokenEndpoint);
    }
    
    [Fact]
    public void StaticConfigurations_WithSameToken_AreEqual()
    {
        // ARRANGE
        const string token = "same-token";
        StaticTokenProviderConfiguration config1 = new(token);
        StaticTokenProviderConfiguration config2 = new(token);

        // ACT & ASSERT
        Assert.Equal(config1, config2);
    }

    [Fact]
    public void StaticConfigurations_WithDifferentTokens_AreNotEqual()
    {
        // ARRANGE
        StaticTokenProviderConfiguration config1 = new("token1");
        StaticTokenProviderConfiguration config2 = new("token2");

        // ACT & ASSERT
        Assert.NotEqual(config1, config2);
    }
}