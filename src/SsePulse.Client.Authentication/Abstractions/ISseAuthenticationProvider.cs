namespace SsePulse.Client.Authentication.Abstractions;

public interface ISseAuthenticationProvider
{
    ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    
#if NET8_0_OR_GREATER
    public static NoneAuthenticationProvider None = new();
#endif
}