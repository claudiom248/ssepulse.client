using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection.Tests;

public sealed class SseSourceBuilderExtensionsTests
{
    [Fact]
    public void AddDistributedCacheLastEventIdStore_DistributedCacheLastEventIdStore_IsResolvableAsKeyedService()
    {
        // ARRANGE
        ServiceCollection services = BuildServicesWithCache();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(options => options.Key = "my-key");
        
        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        DistributedCacheLastEventIdStore store = provider.GetRequiredKeyedService<DistributedCacheLastEventIdStore>("MySource");
        Assert.IsType<DistributedCacheLastEventIdStore>(store);
    }
    [Fact]
    public void AddDistributedCacheLastEventIdStore_OptionsLastEventIdStoreFactory_ProvidesDistributedCacheLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = BuildServicesWithCache();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(options => options.Key = "my-key");
        
        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions factoryOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");
        Assert.NotNull(factoryOptions.LastEventIdStoreFactory);
        Assert.IsType<DistributedCacheLastEventIdStore>(factoryOptions.LastEventIdStoreFactory!(provider));
    }
    [Fact]
    public void AddDistributedCacheLastEventIdStore_RequestMutatorFactory_ProvidesLastEventIdRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = BuildServicesWithCache();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(options => options.Key = "my-key");
        
        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions factoryOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");
        Assert.Single(factoryOptions.RequestMutatorsFactories);
        Assert.IsType<LastEventIdRequestMutator>(factoryOptions.RequestMutatorsFactories[0](provider));
    }
    [Fact]
    public void AddDistributedCacheLastEventIdStore_ConfigureOptions_OptionsAreBoundToBuilderName()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(options => options.Key = "my-custom-key");
        
        // ASSERT -- options must be retrievable by name, not by the default (empty) name
        ServiceProvider provider = services.BuildServiceProvider();
        DistributedCacheLastEventIdStoreOptions resolvedOptions = provider
            .GetRequiredService<IOptionsMonitor<DistributedCacheLastEventIdStoreOptions>>()
            .Get("MySource");
        Assert.Equal("my-custom-key", resolvedOptions.Key);
    }
    [Fact]
    public void AddDistributedCacheLastEventIdStore_WhenCacheFactorySet_DoesNotRequireCacheInDI()
    {
        // ARRANGE -- IDistributedCache is intentionally NOT registered in DI
        ServiceCollection services = new();
        services.AddLogging();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        bool factoryInvoked = false;
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(
            _ =>
            {
                factoryInvoked = true;
                return CreateMockCache();
            },
            options => options.Key = "my-key");
        
        // ASSERT -- store resolves via the factory without IDistributedCache in DI
        ServiceProvider provider = services.BuildServiceProvider();
        DistributedCacheLastEventIdStore store = provider.GetRequiredKeyedService<DistributedCacheLastEventIdStore>("MySource");
        Assert.IsType<DistributedCacheLastEventIdStore>(store);
        Assert.True(factoryInvoked);
    }
    [Fact]
    public void AddDistributedCacheLastEventIdStore_WhenCacheInDIAndNoFactory_ResolveCacheFromDI()
    {
        // ARRANGE
        ServiceCollection services = BuildServicesWithCache();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        
        // ACT
        builder.AddDistributedCacheLastEventIdStore(options => options.Key = "my-key");
       
        // ASSERT -- store resolves using the IDistributedCache registered in DI
        ServiceProvider provider = services.BuildServiceProvider();
        DistributedCacheLastEventIdStore store = provider.GetRequiredKeyedService<DistributedCacheLastEventIdStore>("MySource");
        Assert.IsType<DistributedCacheLastEventIdStore>(store);
    }

    private static ServiceCollection BuildServicesWithCache()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(CreateMockCache());
        return services;
    }
    
    private static IDistributedCache CreateMockCache()
    {
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.Get(Arg.Any<string>()).Returns((byte[]?)null);
        return cache;
    }
}

