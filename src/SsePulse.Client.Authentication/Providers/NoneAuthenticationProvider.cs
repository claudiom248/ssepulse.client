using SsePulse.Client.Authentication.Abstractions;

namespace SsePulse.Client.Authentication.Providers;

/// <summary>
/// A no-op <see cref="ISseAuthenticationProvider"/> that leaves requests unmodified.
/// Used as the default when no authentication is configured.
/// </summary>
public class NoneAuthenticationProvider : ISseAuthenticationProvider
{
    /// <inheritdoc/>
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();    
#endif
    }
}