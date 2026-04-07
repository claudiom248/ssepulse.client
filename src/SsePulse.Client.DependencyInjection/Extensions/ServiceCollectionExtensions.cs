using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Extensions;

public static partial class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public ISseSourceBuilder AddSseSource()
        {
            return services.AddSseSource(Constants.DefaultSourceName);
        }

        public ISseSourceBuilder AddSseSource(IConfiguration configuration)
        {
            return services.AddSseSource(Constants.DefaultSourceName, configuration);
        }

        public ISseSourceBuilder AddSseSource(string name, IConfiguration? configuration = null)
        {
            return services.AddSseSourceCore(name, configuration);
        }

        public ISseSourceBuilder AddSseSource(Action<SseSourceOptions> configureOptions)
        {
            return services.AddSseSource(Constants.DefaultSourceName, configureOptions);
        }

        public ISseSourceBuilder AddSseSource(string name, Action<SseSourceOptions> configureOptions)
        {
            return services.AddSseSourceCore(name, configureOptions: configureOptions);
        }

        private SseSourceBuilder AddSseSourceCore(
            string name,
            IConfiguration? configuration = null,
            Action<SseSourceOptions>? configureOptions = null)
        {
            services.TryAddSingleton<DefaultSseSourceFactory>();
            services.TryAddSingleton<ISseSourceFactory>(sp => sp.GetRequiredService<DefaultSseSourceFactory>());

            SseSourceRegistrationService registrationService = services.GetSseSourceRegistrationService();
            if (!registrationService.TryRegister(name))
            {
                return registrationService.SourceBuildersCache[name];
            }
            SseSourceBuilder sourceBuilder = configureOptions is not null
                ? new SseSourceBuilder(name, services, configureOptions)
                : new SseSourceBuilder(name, services, configuration);
            registrationService.SourceBuildersCache.Add(name, sourceBuilder);
            return sourceBuilder;
        }

        private SseSourceRegistrationService GetSseSourceRegistrationService()
        {
            SseSourceRegistrationService? registrationService =
                (SseSourceRegistrationService?)services
                    .FirstOrDefault(s => s.ServiceType == typeof(SseSourceRegistrationService))?.ImplementationInstance;
            if (registrationService is not null)
            {
                return registrationService;
            }
            registrationService = new SseSourceRegistrationService(services);
            services.AddSingleton(registrationService);
            return registrationService;
        }
    }
}