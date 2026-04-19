namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

/// <summary>
/// Describes the configuration needed to construct a specific <see cref="ITokenProvider"/>
/// via <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.TokenProviderFactory"/>.
/// Each implementation identifies the provider type through the <see cref="Provider"/> discriminator.
/// </summary>
public interface ITokenProviderConfiguration
{
    /// <summary>Gets the string key that identifies which token provider to instantiate.</summary>
    public string Provider { get; }
}