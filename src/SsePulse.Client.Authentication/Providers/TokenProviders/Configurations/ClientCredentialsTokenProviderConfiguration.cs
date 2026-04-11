using SsePulse.Client.Authentication.Common.Credentials;

namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

/// <summary>
/// Configuration for an OAuth 2.0 client-credentials token provider.
/// Used by <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.TokenProviderFactory"/>
/// to create a <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.ClientCredentialsTokenProvider"/>.
/// </summary>
/// <param name="TokenEndpoint">The token endpoint URI where the access token is requested.</param>
/// <param name="Credentials">The client ID and secret sent with each token request.</param>
public record struct ClientCredentialsTokenProviderConfiguration(Uri TokenEndpoint, ClientCredentials Credentials) : ITokenProviderConfiguration
{
    /// <inheritdoc/>
    public string Provider => Constants.ClientCredentialsTokenProviderName;
}