using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Providers.Configurations;

namespace SsePulse.Client.Authentication.Providers;

public class ApiKeyAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly ApiKeyAuthenticationProviderConfiguration _configuration;

    public ApiKeyAuthenticationProvider(ApiKeyAuthenticationProviderConfiguration configuration)
    {
        _configuration = configuration;
    }
    
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