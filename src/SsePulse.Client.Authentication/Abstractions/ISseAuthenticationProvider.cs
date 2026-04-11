namespace SsePulse.Client.Authentication.Abstractions;

/// <summary>
/// Applies authentication credentials to an outgoing SSE HTTP request.
/// Implement this interface to add custom authentication schemes.
/// Built-in implementations include <see cref="SsePulse.Client.Authentication.Providers.BearerTokenAuthenticationProvider"/>,
/// <see cref="SsePulse.Client.Authentication.Providers.BasicAuthenticationProvider"/>,
/// <see cref="SsePulse.Client.Authentication.Providers.ApiKeyAuthenticationProvider"/>, and
/// <see cref="SsePulse.Client.Authentication.Providers.NoneAuthenticationProvider"/>.
/// </summary>
public interface ISseAuthenticationProvider
{
    /// <summary>
    /// Mutates <paramref name="request"/> by adding the required authentication headers or query parameters.
    /// </summary>
    /// <param name="request">The outgoing HTTP request to authenticate.</param>
    /// <param name="cancellationToken">Token to cancel asynchronous credential retrieval.</param>
    ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}