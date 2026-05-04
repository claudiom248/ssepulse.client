using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Providers.Configurations;

namespace SsePulse.Client.Authentication.Providers;

/// <summary>
/// Authenticates SSE requests by adding an API key to a configurable HTTP request header.
/// Configure the key and header name via <see cref="ApiKeyAuthenticationProviderConfiguration"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
/// </summary>
public class ApiKeyAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly ApiKeyAuthenticationProviderConfiguration _configuration;

    /// <summary>
    /// Initializes a new <see cref="ApiKeyAuthenticationProvider"/> with the supplied configuration.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
    /// </summary>
    /// <param name="configuration">The API key and target header name.</param>
    public ApiKeyAuthenticationProvider(ApiKeyAuthenticationProviderConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(_configuration.Header, _configuration.Key);
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}