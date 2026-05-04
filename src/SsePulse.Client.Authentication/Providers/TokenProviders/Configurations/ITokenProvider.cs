namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

/// <summary>
/// Supplies Bearer tokens used by <see cref="SsePulse.Client.Authentication.Providers.BearerTokenAuthenticationProvider"/>.
/// Implement this interface to provide custom token acquisition logic (e.g. caching, refresh).
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Asynchronously retrieves the current authentication token.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the async retrieval.</param>
    /// <returns>The raw token string to be used as a Bearer credential.</returns>
    public ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken);
}