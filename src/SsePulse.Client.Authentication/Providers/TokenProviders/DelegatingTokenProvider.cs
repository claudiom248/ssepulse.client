using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Providers.TokenProviders;

/// <summary>
/// An <see cref="ITokenProvider"/> that delegates token retrieval to a caller-supplied async function.
/// Useful when you have a simple lambda or method that produces a token, without needing a full class.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
/// </summary>
public class DelegatingTokenProvider : ITokenProvider
{
    private readonly Func<CancellationToken, ValueTask<string>> _tokenProvider;

    /// <summary>
    /// Initializes a new <see cref="DelegatingTokenProvider"/> with the supplied delegate.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
    /// </summary>
    /// <param name="tokenProvider">Async function that returns the token string.</param>
    public DelegatingTokenProvider(Func<CancellationToken, ValueTask<string>> tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc/>
    public ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken)
    {
        return _tokenProvider.Invoke(cancellationToken);
    }
}