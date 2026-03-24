using System.Net.Http.Headers;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;
using Constants = SsePulse.Client.Authentication.Providers.TokenProviders.Constants;

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
        string token = await _tokenProvider.GetAuthenticationTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.BearerTokenSchemeName, token);
    }
}