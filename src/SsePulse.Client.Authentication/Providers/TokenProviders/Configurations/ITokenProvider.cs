namespace SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;

public interface ITokenProvider
{
    public ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken);
}