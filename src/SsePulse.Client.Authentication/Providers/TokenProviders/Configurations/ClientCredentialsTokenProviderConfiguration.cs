using SsePulse.Client.Authentication.Common.Credentials;

namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

public record struct ClientCredentialsTokenProviderConfiguration(Uri TokenEndpoint, ClientCredentials Credentials) : ITokenProviderConfiguration
{
    public string ProviderName => Constants.ClientCredentialsTokenProviderName;  
}