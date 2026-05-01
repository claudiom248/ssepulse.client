using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Tests;

public class SseSourceBuilderExtensionsTests
{
    [Fact]
    public void AddLastEventId_ILastEventIdStore_IsResolvableAsInMemoryLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        ILastEventIdStore store = provider.GetRequiredService<ILastEventIdStore>();

        Assert.IsType<InMemoryLastEventIdStore>(store);
    }

    [Fact]
    public void AddLastEventId_OptionsLastEventIdStoreFactory_ProvidesInMemoryLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.NotNull(options.LastEventIdStoreFactory);
        ILastEventIdStore store = options.LastEventIdStoreFactory!(provider);
        Assert.IsType<InMemoryLastEventIdStore>(store);
    }

    [Fact]
    public void AddLastEventId_RequestMutatorFactory_ProvidesLastEventIdRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator mutator = options.RequestMutatorsFactories[0](provider);
        Assert.IsType<LastEventIdRequestMutator>(mutator);
    }

    [Fact]
    public void AddLastEventId_CalledTwice_StoreIsRegisteredOnlyOnce()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId();
        builder.AddLastEventId();

        // ASSERT — TryAddTransient semantics: ILastEventIdStore registered only once
        Assert.Single(services, d => d.ServiceType == typeof(ILastEventIdStore));
    }

    [Fact]
    public void AddLastEventId_OnNamedSource_DoesNotPolluteMutatorListOfOtherSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder source1 = new SseSourceBuilder("Source1", services);
        _ = new SseSourceBuilder("Source2", services); // just registers options

        // ACT
        source1.AddLastEventId();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options2 = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("Source2");

        Assert.Empty(options2.RequestMutatorsFactories);
        Assert.Null(options2.LastEventIdStoreFactory);
    }


    [Fact]
    public void AddLastEventId_WithCustomStore_OptionsLastEventIdStoreFactory_ProvidesCustomStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<CustomLastEventIdStore>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId<CustomLastEventIdStore>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.NotNull(options.LastEventIdStoreFactory);
        ILastEventIdStore store = options.LastEventIdStoreFactory!(provider);
        Assert.IsType<CustomLastEventIdStore>(store);
    }

    [Fact]
    public void AddLastEventId_WithCustomStore_RequestMutatorUsesCustomStoreInstance()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<CustomLastEventIdStore>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddLastEventId<CustomLastEventIdStore>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator mutator = options.RequestMutatorsFactories[0](provider);
        Assert.IsType<LastEventIdRequestMutator>(mutator);
    }

    [Fact]
    public void AddFileLastEventIdStore_FileLastEventIdStore_IsResolvable()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddFileLastEventIdStore(options =>
        {
            options.FilePath = "path";
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        ILastEventIdStore store = provider.GetRequiredKeyedService<FileLastEventIdStore>("MySource");

        Assert.IsType<FileLastEventIdStore>(store);
    }
    
    [Fact]
    public void AddFileLastEventIdStore_OptionsLastEventIdStoreFactory_ProvidesFileLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddFileLastEventIdStore(options =>
        {
            options.FilePath = "path";
        });
        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.NotNull(options.LastEventIdStoreFactory);
        ILastEventIdStore store = options.LastEventIdStoreFactory!(provider);
        Assert.IsType<FileLastEventIdStore>(store);
    }
    
    [Fact]
    public void AddFileLastEventIdStore_RequestMutatorFactory_ProvidesLastEventIdRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddFileLastEventIdStore(options =>
        {
            options.FilePath = "path";
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator mutator = options.RequestMutatorsFactories[0](provider);
        Assert.IsType<LastEventIdRequestMutator>(mutator);
    }
    
    [Fact]
    public void BindEventsManager_WithInstance_OptionsEventManagerFactories_HasExactlyOneEntry()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        MockEventsManager manager = new();

        // ACT
        builder.BindEventsManager(manager);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.Single(options.EventManagerFactories);
    }

    [Fact]
    public void BindEventsManager_WithInstance_EventManagerFactory_ReturnsSameInstance()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        MockEventsManager manager = new();

        // ACT
        builder.BindEventsManager(manager);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        ISseEventsManager resolved = options.EventManagerFactories[0](provider);
        Assert.Same(manager, resolved);
    }

    [Fact]
    public void BindEventsManager_Generic_OptionsEventManagerFactories_HasExactlyOneEntry()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<MockEventsManager>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.BindEventsManager<MockEventsManager>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.Single(options.EventManagerFactories);
    }

    [Fact]
    public void BindEventsManager_Generic_EventManagerFactory_ResolvesRegisteredType()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<MockEventsManager>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.BindEventsManager<MockEventsManager>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        ISseEventsManager manager = options.EventManagerFactories[0](provider);
        Assert.IsType<MockEventsManager>(manager);
    }

    [Fact]
    public void BindEventsManager_Generic_WithDependency_DependencyIsInjectedByContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<IFakeDependency, FakeDependency>();
        services.AddTransient<ManagerWithDependency>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.BindEventsManager<ManagerWithDependency>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        ISseEventsManager manager = options.EventManagerFactories[0](provider);
        ManagerWithDependency typedManager = Assert.IsType<ManagerWithDependency>(manager);
        Assert.IsType<FakeDependency>(typedManager.Dependency);
    }

    [Fact]
    public void BindEventsManager_OnNamedSource_DoesNotPollutEventManagerListOfOtherSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<MockEventsManager>();
        ISseSourceBuilder source1 = new SseSourceBuilder("Source1", services);
        _ = new SseSourceBuilder("Source2", services);

        // ACT
        source1.BindEventsManager<MockEventsManager>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options2 = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("Source2");

        Assert.Empty(options2.EventManagerFactories);
    }

    private class CustomLastEventIdStore : ILastEventIdStore
    {
        public string? LastEventId { get; private set; }

        public void Set(string eventId) => LastEventId = eventId;
    }

    private class MockEventsManager : ISseEventsManager { }

    private interface IFakeDependency { }

    private class FakeDependency : IFakeDependency { }

    private class ManagerWithDependency(IFakeDependency dependency) : ISseEventsManager
    {
        public IFakeDependency Dependency { get; } = dependency;
    }
}
