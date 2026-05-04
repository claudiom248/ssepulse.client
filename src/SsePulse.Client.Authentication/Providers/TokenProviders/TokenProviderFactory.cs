using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers.TokenProviders;

/// <summary>
/// Creates <see cref="ITokenProvider"/> instances from their configuration counterparts.
/// Supports <see cref="Configurations.ClientCredentialsTokenProviderConfiguration"/>,
/// <see cref="Configurations.StaticTokenProviderConfiguration"/>, and
/// <see cref="Configurations.EnvironmentVariableTokenProviderConfiguration"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
/// </summary>
public static class TokenProviderFactory
{
    /// <summary>
    /// Instantiates the appropriate <see cref="ITokenProvider"/> for the supplied <paramref name="configuration"/>.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
    /// </summary>
    /// <param name="configuration">The provider configuration that drives which implementation is created.</param>
    /// <returns>A ready-to-use <see cref="ITokenProvider"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="configuration"/> is not a recognized <see cref="ITokenProviderConfiguration"/> type.
    /// </exception>
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