namespace SsePulse.Client.Authentication.Common.Credentials;

/// <summary>
/// Holds the client ID and secret used for OAuth 2.0 client-credentials token requests.
/// Used by <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.ClientCredentialsTokenProviderConfiguration"/>.
/// </summary>
public class ClientCredentials(string clientId, string clientSecret) : IAuthenticationCredentials
{
    /// <summary>Gets or sets the OAuth 2.0 client identifier.</summary>
    public string ClientId { get; set; } = clientId;

    /// <summary>Gets or sets the OAuth 2.0 client secret.</summary>
    public string ClientSecret { get; set; } = clientSecret;

    /// <summary>Initializes a <see cref="ClientCredentials"/> with empty values (for configuration binding).</summary>
    public ClientCredentials() : this(string.Empty, string.Empty)
    {
    }
}