using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;

namespace SsePulse.Client.DependencyInjection.Extensions;

public static partial class ServiceCollectionExtensions
{
    internal class SseSourceRegistrationService
    {
        public ServiceDescriptor? DefaultDescriptor { get; private set; }
        private readonly IServiceCollection _services;
        private readonly HashSet<string> _registeredSourceNames = [];
        
        public Dictionary<string, SseSourceBuilder> SourceBuildersCache { get; } = [];

        public SseSourceRegistrationService(IServiceCollection services)
        {
            _services = services;
        }

        public bool TryRegister(string name)
        {
            if(!_registeredSourceNames.Add(name))
            {
                return false;
            }
            ServiceDescriptor descriptor = ServiceDescriptor.Describe(
                typeof(SseSource),
                ImplementationFactory,
                ServiceLifetime.Transient);
            ServiceDescriptor keyedDescriptor = ServiceDescriptor.DescribeKeyed(
                typeof(SseSource),
                name,
                (sp, _) => ImplementationFactory(sp),
                ServiceLifetime.Transient);
            if (name == Constants.DefaultSourceName)
            {
                DefaultDescriptor = descriptor;
                _services.Add(descriptor);
            }
            else if (DefaultDescriptor is not null)
            {
                _services.Insert(_services.IndexOf(DefaultDescriptor), descriptor);
            }
            else
            {
                _services.Add(descriptor);
            }
            _services.Add(keyedDescriptor);
            return true;

            SseSource ImplementationFactory(IServiceProvider sp)
            {
                ISseSourceFactory factory = sp.GetRequiredService<ISseSourceFactory>();
                return factory.CreateSseSource(name);
            }
        }
    }
}