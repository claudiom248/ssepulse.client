using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class SseSourceFactoryServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _sharedServices = [];
    private readonly IServiceProvider _serviceProvider;

    public SseSourceFactoryServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public object? GetService(Type serviceType)
    {
        object? service;
        Type typeToShare = typeof(ILastEventIdStore);
        if (!typeToShare.IsAssignableFrom(serviceType)) return _serviceProvider.GetService(serviceType);
        if (_sharedServices.TryGetValue(typeToShare, out service))
        {
            return service;           
        }
        service = _serviceProvider.GetService(serviceType);
        if (service is not null)
        {
            _sharedServices.Add(typeToShare, service);
        }
        return service;
    }
}