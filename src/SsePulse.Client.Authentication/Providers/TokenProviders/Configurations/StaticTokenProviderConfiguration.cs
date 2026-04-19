namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

/// <summary>
/// Configuration for a static (hard-coded) Bearer token provider.
/// Use this when the token is fixed at configuration time and does not need to be refreshed.
/// </summary>
/// <param name="Token">The literal Bearer token string to use on every request.</param>
public record struct StaticTokenProviderConfiguration(string Token) : ITokenProviderConfiguration
{
    /// <inheritdoc/>
    public string Provider => Constants.StaticTokenProviderName;
}