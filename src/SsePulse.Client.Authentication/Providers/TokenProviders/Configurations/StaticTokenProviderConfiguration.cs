namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

public record struct StaticTokenProviderConfiguration(string Token) : ITokenProviderConfiguration
{
    public string Provider => Constants.StaticTokenProviderName;
}