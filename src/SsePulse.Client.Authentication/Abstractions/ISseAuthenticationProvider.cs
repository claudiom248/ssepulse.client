namespace SsePulse.Client.Authentication.Abstractions;

/// <summary>
/// Applies authentication credentials to an outgoing SSE HTTP request.
/// Implement this interface to add custom authentication mechanisms.
/// </summary>
public interface ISseAuthenticationProvider
{
    /// <summary>
    /// Mutates <paramref name="request"/> by adding the required headers for authentication.
    /// </summary>
    /// <param name="request">The outgoing HTTP request to authenticate.</param>
    /// <param name="cancellationToken">Token to cancel asynchronous credential retrieval.</param>
    ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}