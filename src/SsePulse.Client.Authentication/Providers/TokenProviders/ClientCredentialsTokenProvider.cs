using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers.TokenProviders;

/// <summary>
/// An <see cref="ITokenProvider"/> that acquires a Bearer token by posting an OAuth 2.0
/// client-credentials request to the configured token endpoint.
/// </summary>
public class ClientCredentialsTokenProvider : ITokenProvider
{
    private readonly ClientCredentialsTokenProviderConfiguration _configuration;

    /// <summary>
    /// Initializes a new <see cref="ClientCredentialsTokenProvider"/> with the supplied configuration.
    /// </summary>
    /// <param name="configuration">Contains the token endpoint URI and client credentials.</param>
    public ClientCredentialsTokenProvider(ClientCredentialsTokenProviderConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public async ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken)
    {
        HttpClient httpClient = new()
        {
            BaseAddress = _configuration.TokenEndpoint
        };

        HttpRequestMessage request = new(HttpMethod.Post, httpClient.BaseAddress);
        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP error occurred: {response.StatusCode}");
            }

#if NET8_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }
        catch (Exception)
        {
            throw;
        }
    }
}