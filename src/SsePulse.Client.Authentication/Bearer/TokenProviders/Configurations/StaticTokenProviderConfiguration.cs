namespace SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;

public record struct StaticTokenProviderConfiguration(string Token) : ITokenProviderConfiguration
{
    public string ProviderName => Constants.StaticTokenProviderName;
}