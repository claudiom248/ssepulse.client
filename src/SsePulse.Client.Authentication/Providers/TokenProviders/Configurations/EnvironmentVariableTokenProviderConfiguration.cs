namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

public record struct EnvironmentVariableTokenProviderConfiguration(string EnvironmentVariable) : ITokenProviderConfiguration
{
    public string ProviderName => Constants.EnvironmentVariableTokenProviderName;

    public EnvironmentVariableTokenProviderConfiguration() : this("")
    {
        
    }
}