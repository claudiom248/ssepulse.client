namespace SsePulse.Client.Authentication.Abstractions;

public interface ISseAuthenticationProvider
{
    ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}