namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

/// <summary>
/// Configuration for a Bearer token provider that reads the token from an environment variable at runtime.
/// </summary>
/// <param name="EnvironmentVariable">The name of the environment variable that holds the token.</param>
public record struct EnvironmentVariableTokenProviderConfiguration(string EnvironmentVariable) : ITokenProviderConfiguration
{
    /// <inheritdoc/>
    public string Provider => Constants.EnvironmentVariableTokenProviderName;

    /// <summary>Initializes an <see cref="EnvironmentVariableTokenProviderConfiguration"/> with an empty variable name (for configuration binding).</summary>
    public EnvironmentVariableTokenProviderConfiguration() : this("")
    {
    }
}