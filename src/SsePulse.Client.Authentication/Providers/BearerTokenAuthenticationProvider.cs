using System.Net.Http.Headers;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers;

public class BearerTokenAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly ITokenProvider _tokenProvider;

    public BearerTokenAuthenticationProvider(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }
    
    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string token = await _tokenProvider.GetAuthenticationTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.BearerTokenSchemeName, token);
    }
}