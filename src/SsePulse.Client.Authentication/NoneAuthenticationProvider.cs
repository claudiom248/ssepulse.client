using SsePulse.Client.Authentication.Abstractions;

namespace SsePulse.Client.Authentication;

public class NoneAuthenticationProvider : ISseAuthenticationProvider
{
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}