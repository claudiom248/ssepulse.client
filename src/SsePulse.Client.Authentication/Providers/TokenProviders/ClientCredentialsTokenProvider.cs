using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers.TokenProviders;

public class ClientCredentialsTokenProvider : ITokenProvider
{
    private readonly ClientCredentialsTokenProviderConfiguration _configuration;

    public ClientCredentialsTokenProvider(ClientCredentialsTokenProviderConfiguration configuration)
    {
        _configuration = configuration;
    }

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
        catch (Exception ex)
        {
            throw;
        }
    }
}