using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Abstractions;

namespace SsePulse.Client.DependencyInjection.Extensions;

/// <summary>
/// Extension methods on <see cref="IServiceProvider"/> for resolving SSE sources and related services.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/dependency-injection.html"/>
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Resolves the scoped <see cref="ISseSourceFactory"/> registered by
    /// <see cref="ServiceCollectionExtensions.AddScopedSseSourceFactory"/>.
    /// <br/><br/>
    /// Use this method instead of injecting <see cref="ISseSourceFactory"/> directly, because the
    /// scoped factory is registered as a keyed service and is not visible to standard
    /// constructor injection.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/dependency-injection.html"/>
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider for the current scope. Passing the root provider defeats the purpose
    /// of the scoped lifetime — always resolve from within an <see cref="IServiceScope"/>.
    /// </param>
    /// <returns>
    /// The <see cref="ISseSourceFactory"/> instance bound to the current scope.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ServiceCollectionExtensions.AddScopedSseSourceFactory"/> has not
    /// been called on the service collection.
    /// </exception>
    public static ISseSourceFactory GetScopedSseSourceFactory(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<ISseSourceFactory>(Constants.ScopedSseSourceFactoryServiceKey);
    }
}