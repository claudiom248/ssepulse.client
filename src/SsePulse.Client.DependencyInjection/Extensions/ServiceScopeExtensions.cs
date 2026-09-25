using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Abstractions;

namespace SsePulse.Client.DependencyInjection.Extensions;

/// <summary>
/// Extension methods on <see cref="IServiceScope"/> for resolving SSE sources and related services.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/dependency-injection.html"/>
/// </summary>
public static class ServiceScopeExtensions
{
    /// <summary>
    /// Resolves the scoped <see cref="ISseSourceFactory"/> registered by
    /// <see cref="ServiceCollectionExtensions.AddScopedSseSourceFactory"/> from within this scope.
    /// <br/><br/>
    /// Use this method instead of injecting <see cref="ISseSourceFactory"/> directly, because the
    /// scoped factory is registered as a keyed service and is not visible to standard
    /// constructor injection.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/dependency-injection.html"/>
    /// </summary>
    /// <param name="scope">The scope to resolve the <see cref="ISseSourceFactory"/> from.</param>
    /// <returns>
    /// The <see cref="ISseSourceFactory"/> instance bound to the given scope.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ServiceCollectionExtensions.AddScopedSseSourceFactory"/> has not
    /// been called on the service collection.
    /// </exception>
    public static ISseSourceFactory GetScopedSseSourceFactory(this IServiceScope scope)
    {
        return scope.ServiceProvider.GetScopedSseSourceFactory();
    }
}