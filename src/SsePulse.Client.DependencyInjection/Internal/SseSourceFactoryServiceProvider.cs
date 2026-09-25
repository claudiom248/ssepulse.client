using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class SseSourceFactoryServiceProvider : IKeyedServiceProvider
{
    private readonly IServiceProvider _serviceProvider;

    private IKeyedServiceProvider KeyedServiceProvider =>
        _serviceProvider as IKeyedServiceProvider
        ?? throw new InvalidOperationException("The service provider does not support keyed services.");
    private readonly Dictionary<Type, object> _sharedServices = [];
    private readonly Dictionary<(Type, object?), object> _sharedKeyedServices = [];

    private static readonly HashSet<Type> SharedTypes = [typeof(ILastEventIdStore)];

    public SseSourceFactoryServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetService(Type serviceType) =>
        IsSharedService(serviceType, out Type? sharedServiceType)
            ? GetOrAdd(_sharedServices, sharedServiceType ?? serviceType, () => _serviceProvider.GetService(serviceType))
            : _serviceProvider.GetService(serviceType);

    public object? GetKeyedService(Type serviceType, object? serviceKey) =>
        IsSharedService(serviceType, out Type? sharedServiceType)
            ? GetOrAdd(_sharedKeyedServices, (sharedServiceType ?? serviceType, serviceKey), 
                       () => KeyedServiceProvider.GetKeyedService(serviceType, serviceKey))
            : KeyedServiceProvider.GetKeyedService(serviceType, serviceKey);

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        IsSharedService(serviceType, out Type? sharedServiceType)
            ? GetOrAdd(_sharedKeyedServices, (sharedServiceType ?? serviceType, serviceKey), 
                       () => KeyedServiceProvider.GetRequiredKeyedService(serviceType, serviceKey))
            : KeyedServiceProvider.GetRequiredKeyedService(serviceType, serviceKey);

    private static bool IsSharedService(
        Type serviceType,     
        out Type? sharedServiceType)
    {
        Type? type = SharedTypes.FirstOrDefault(t => t.IsAssignableFrom(serviceType));
        if (type is not null)
        {
            sharedServiceType = type;
            return true;
        }
        sharedServiceType = null;
        return false;
    }

    private static TValue GetOrAdd<TKey, TValue>(
        Dictionary<TKey, TValue> cache,
        TKey key,
        Func<TValue?> factory)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out TValue? existing))
            return existing;

        TValue? created = factory();
        if (created is not null)
            cache[key] = created;

        return created!;
    }
}
