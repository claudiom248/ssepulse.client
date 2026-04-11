using System.Net.Http.Headers;
using System.Text;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Providers.TokenProviders;

namespace SsePulse.Client.Authentication.Providers;

/// <summary>
/// Authenticates SSE requests using the HTTP Basic authentication scheme (RFC 7617).
/// Encodes <see cref="BasicCredentials.Username"/> and <see cref="BasicCredentials.Password"/>
/// as a Base64 <c>Authorization</c> header.
/// </summary>
public class BasicAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly BasicCredentials _credentials;

    /// <summary>
    /// Initializes a new <see cref="BasicAuthenticationProvider"/> with the supplied credentials.
    /// </summary>
    /// <param name="credentials">The username/password pair to encode.</param>
    public BasicAuthenticationProvider(BasicCredentials credentials)
    {
        _credentials = credentials;
    }

    /// <inheritdoc/>
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_credentials.Username}:{_credentials.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.BasicSchemeName, credentials);
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}