namespace SsePulse.Client.Authentication.Bearer.TokenProviders.Configurations;

public interface ITokenProvider
{
    public ValueTask<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken);
}