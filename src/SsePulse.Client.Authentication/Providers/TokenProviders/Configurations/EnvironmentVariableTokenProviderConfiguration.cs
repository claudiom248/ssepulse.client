namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

public record struct EnvironmentVariableTokenProviderConfiguration(string EnvironmentVariable) : ITokenProviderConfiguration
{
    public string Provider => Constants.EnvironmentVariableTokenProviderName;

    public EnvironmentVariableTokenProviderConfiguration() : this("")
    {
        
    }
}