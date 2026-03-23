using SsePulse.Client.Authentication.Abstractions;

namespace SsePulse.Client.Authentication.ApiKey;

public class ApiKeyAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly ApiKeyAuthenticationProviderConfiguration _configuration;

    public ApiKeyAuthenticationProvider(ApiKeyAuthenticationProviderConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(_configuration.HeaderKey, _configuration.Key);
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}