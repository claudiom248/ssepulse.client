using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Authentication.Internal;

/// <summary>
/// An <see cref="IRequestMutator"/> that applies authentication to every outgoing SSE request
/// by delegating to a configured <see cref="ISseAuthenticationProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <see cref="ISseAuthenticationProvider"/> abstraction and the
/// <see cref="IRequestMutator"/> pipeline.
/// </para>
/// </remarks>
public class AuthenticationRequestMutator : IRequestMutator
{
    private readonly ISseAuthenticationProvider _authenticationProvider;

    /// <summary>
    /// Initializes a new <see cref="AuthenticationRequestMutator"/> with the given authentication provider.
    /// </summary>
    /// <param name="authenticationProvider">
    /// The provider responsible for authenticating outgoing requests.
    /// It is invoked once per connection attempt, just before the request is sent.
    /// </param>
    public AuthenticationRequestMutator(ISseAuthenticationProvider authenticationProvider)
    {
        _authenticationProvider = authenticationProvider;
    }

    /// <inheritdoc/>
    public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        return _authenticationProvider.ApplyAsync(message, cancellationToken);
    }
}