using SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;

namespace SsePulse.Client.Authentication.Bearer.TokenProviders;

public class DelegatingTokenProvider : ITokenProvider
{
    private readonly Func<CancellationToken, ValueTask<string>> _tokenProvider;

    public DelegatingTokenProvider(Func<CancellationToken, ValueTask<string>> tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken)
    {
        return _tokenProvider.Invoke(cancellationToken);
    }
}