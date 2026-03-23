using SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Bearer.TokenProviders;

public static class TokenProviderFactory
{
    public static ITokenProvider Create(ITokenProviderConfiguration configuration) =>
        configuration switch
        {
            ClientCredentialsTokenProviderConfiguration clientCredentialsTokenProviderConfiguration
                => new ClientCredentialsTokenProvider(clientCredentialsTokenProviderConfiguration),
            StaticTokenProviderConfiguration staticTokenProviderConfiguration
                => new DelegatingTokenProvider(_ => new ValueTask<string>(staticTokenProviderConfiguration.Token)),
            EnvironmentVariableTokenProviderConfiguration environmentVariableTokenProviderConfiguration
                => new DelegatingTokenProvider(_ =>
                    new ValueTask<string>(
                        Environment.GetEnvironmentVariable(environmentVariableTokenProviderConfiguration
                            .EnvironmentVariable)!)),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration), configuration, null)
        };
}