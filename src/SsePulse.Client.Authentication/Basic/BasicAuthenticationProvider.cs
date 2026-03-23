using System.Net.Http.Headers;
using System.Text;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Bearer.TokenProviders;
using SsePulse.Client.Authentication.Common.Credentials;

namespace SsePulse.Client.Authentication.Basic;

public class BasicAuthenticationProvider : ISseAuthenticationProvider
{
    private readonly BasicCredentials _credentials;

    public BasicAuthenticationProvider(BasicCredentials credentials)
    {
        _credentials = credentials;
    }
    
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_credentials.Username}:{_credentials.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.BasicSchemeName, $"{credentials }");
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}