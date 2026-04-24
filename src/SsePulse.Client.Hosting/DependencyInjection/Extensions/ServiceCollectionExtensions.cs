using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.DependencyInjection;
using SseSourceRegistrationService =
    SsePulse.Client.DependencyInjection.Extensions.ServiceCollectionExtensions.SseSourceRegistrationService;

namespace SsePulse.Client.Hosting.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering SSE hosted services on <see cref="IServiceCollection"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/SsePulse.Client/docs/hosted-services.html"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers one <see cref="IHostedService"/> per configured SSE source so that each source starts consuming on host startup.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/SsePulse.Client/docs/hosted-services.html"/>
    /// </summary>
    /// <param name="services">The service collection that already contains one or more SSE source registrations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no SSE sources were registered before calling this method.</exception>
    /// <remarks> Calling this method multiple times will result in multiple <see cref="IHostedService"/> instances being registered for the same sources.</remarks>
    public static IServiceCollection AddSseSourcesHostedServices(this IServiceCollection services)
    {
        SseSourceRegistrationService sseSourceRegistrationService = (SseSourceRegistrationService)(
            services.FirstOrDefault(d =>
                d.ServiceType == typeof(SseSourceRegistrationService))?.ImplementationInstance ??
            throw new InvalidOperationException(
                $"No {nameof(SseSource)} has been registered in the service collection."));

        foreach (SseSourceBuilder sseSourceBuilder in sseSourceRegistrationService.SourceBuildersCache.Values)
        {
            services.AddSingleton<IHostedService>(sp =>
            {
                ISseSourceFactory factory = sp.GetRequiredService<ISseSourceFactory>();
                return ActivatorUtilities.CreateInstance<SseSourceHostedService>(
                    sp,
                    factory.CreateSseSource(sseSourceBuilder.Name));
            });
        }

        return services;
    }
}