using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Authentication.Internal;

internal class AuthenticationRequestMutator : IRequestMutator
{
    private readonly ISseAuthenticationProvider _authenticationProvider;

    public AuthenticationRequestMutator(ISseAuthenticationProvider authenticationProvider)
    {
        _authenticationProvider = authenticationProvider;
    }
    
    public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        return _authenticationProvider.ApplyAsync(message, cancellationToken);
    }
}