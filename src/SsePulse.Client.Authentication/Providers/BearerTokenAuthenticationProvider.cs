using System.Net.Http.Headers;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers;

/// <summary>
/// Authenticates SSE requests using a Bearer token obtained from an <see cref="ITokenProvider"/>.
/// The token is fetched on every request.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
/// </summary>
public class BearerTokenAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly ITokenProvider _tokenProvider;

    /// <summary>
    /// Initializes a new <see cref="BearerTokenAuthenticationProvider"/> with the supplied token provider.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
    /// </summary>
    /// <param name="tokenProvider">Source of Bearer tokens (static, client-credentials, env-var, or custom).</param>
    public BearerTokenAuthenticationProvider(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc/>
    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string token = await _tokenProvider.GetAuthenticationTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.BearerTokenSchemeName, token);
    }
}