namespace SsePulse.Client.Authentication.Providers.TokenProviders;

/// <summary>Enumerates the built-in token provider strategies available for Bearer authentication.</summary>
public enum TokenProviderType
{
    /// <summary>A hard-coded token that never changes. Configured via <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.StaticTokenProviderConfiguration"/>.</summary>
    Static,
    /// <summary>An OAuth 2.0 client-credentials flow. Configured via <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.ClientCredentialsTokenProviderConfiguration"/>.</summary>
    ClientCredentials,
    /// <summary>A token read from an environment variable at runtime. Configured via <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.EnvironmentVariableTokenProviderConfiguration"/>.</summary>
    EnvironmentVariable
}