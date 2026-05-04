using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Authentication.Internal;

/// <summary>
/// An <see cref="IRequestMutator"/> that applies authentication to every outgoing SSE request
/// by delegating to a configured <see cref="ISseAuthenticationProvider"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
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
    private readonly ILogger<AuthenticationRequestMutator> _logger;

    /// <summary>
    /// Initializes a new <see cref="AuthenticationRequestMutator"/> with the given authentication provider.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/authentication.html"/>
    /// </summary>
    /// <param name="authenticationProvider">
    /// The provider responsible for authenticating outgoing requests.
    /// It is invoked once per connection attempt, just before the request is sent.
    /// </param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    public AuthenticationRequestMutator(ISseAuthenticationProvider authenticationProvider,
        ILogger<AuthenticationRequestMutator>? logger = null)
    {
        _authenticationProvider = authenticationProvider;
        _logger = logger ?? NullLogger<AuthenticationRequestMutator>.Instance;
    }

    /// <inheritdoc/>
    public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        using IDisposable? _ = _logger.BeginScope(
            "AuthenticationProviderType={AuthenticationProviderType}",
            _authenticationProvider.GetType().Name);
        _logger.LogDebug("Applying authentication to outgoing SSE request...");
        return _authenticationProvider.ApplyAsync(message, cancellationToken);
    }
}