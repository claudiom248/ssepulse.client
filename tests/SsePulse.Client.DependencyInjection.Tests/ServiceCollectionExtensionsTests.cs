using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.DependencyInjection.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSseSource_RegistersISseSourceFactory_AsSingleton()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource();

        // ASSERT
        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(ISseSourceFactory));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSseSource_ISseSourceFactory_HasImplementationFactory()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource();

        // ASSERT
        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(ISseSourceFactory));
        Assert.Equal(typeof(Func<IServiceProvider, ISseSourceFactory>), descriptor.ImplementationFactory!.GetType());
    }

    [Fact]
    public void AddSseSource_RegistersSseSource_AsTransient()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource();

        // ASSERT
        ServiceDescriptor descriptor =
            Assert.Single(services, d => d.ServiceType == typeof(SseSource) && !d.IsKeyedService);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddSseSource_CalledTwice_RegistersISseSourceFactoryOnlyOnce()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource("First");
        services.AddSseSource("Second");

        // ASSERT
        Assert.Single(services, d => d.ServiceType == typeof(ISseSourceFactory));
    }

    [Fact]
    public void AddSseSource_TwoNamedSources_RegistersTwoSseSourceTransients()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource("Events");
        services.AddSseSource("Notifications");

        // ASSERT
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(SseSource) && !d.IsKeyedService));
    }

    [Fact]
    public void AddSseSource_WithConfigureOptions_OptionsAreStoredUnderThatName()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient("MySource");

        // ACT
        services.AddSseSource("MySource", options =>
        {
            options.Path = "/api/events";
            options.MaxDegreeOfParallelism = 8;
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceOptions resolved = provider
            .GetRequiredService<IOptionsMonitor<SseSourceOptions>>()
            .Get("MySource");

        Assert.Equal("/api/events", resolved.Path);
        Assert.Equal(8, resolved.MaxDegreeOfParallelism);
    }

    [Fact]
    public void AddSseSource_WithoutName_OptionsAreStoredUnderDefaultName()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient(Constants.DefaultSourceName);

        // ACT
        services.AddSseSource(options => { options.Path = "/default-events"; });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceOptions resolved = provider
            .GetRequiredService<IOptionsMonitor<SseSourceOptions>>()
            .Get(Constants.DefaultSourceName);

        Assert.Equal("/default-events", resolved.Path);
    }

    [Fact]
    public void AddSseSource_SseSource_ResolvingTwice_ReturnsTwoDistinctInstances()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient(Constants.DefaultSourceName);
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();

        // ACT
        using SseSource first = provider.GetRequiredService<SseSource>();
        using SseSource second = provider.GetRequiredService<SseSource>();

        // ASSERT
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AddSseSource_TwoNamedSources_BothResolveToDistinctInstances()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddHttpClient("Events");
        services.AddHttpClient("Notifications");
        services.AddSseSource("Events");
        services.AddSseSource("Notifications");
        ServiceProvider provider = services.BuildServiceProvider();
        List<SseSource> sources = provider.GetServices<SseSource>().ToList();

        // ASSERT
        Assert.Equal(2, sources.Count);
        Assert.NotSame(sources[0], sources[1]);

        sources.ForEach(s => s.Dispose());
    }

    [Fact]
    public void AddSseSource_ISseSourceFactory_CreateSseSourceByName_ReturnsDistinctInstancesPerCall()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient(Constants.DefaultSourceName);
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource first = factory.CreateSseSource(Constants.DefaultSourceName);
        using SseSource second = factory.CreateSseSource(Constants.DefaultSourceName);

        // ASSERT
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AddSseSource_SameNameCalledTwice_RegistersSseSourceOnlyOnce()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource("Events");
        services.AddSseSource("Events");

        // ASSERT
        Assert.Single(services, d => d.ServiceType == typeof(SseSource) && d.ServiceKey is null);
    }

    [Fact]
    public void AddSseSource_DefaultRegisteredAfterNamed_DefaultDescriptorIsLast()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource("Events");
        services.AddSseSource();

        // ASSERT
        ServiceDescriptor[] sseDescriptors =
            services.Where(d => d.ServiceType == typeof(SseSource) && !d.IsKeyedService).ToArray();
        ServiceCollectionExtensions.SseSourceRegistrationService tracker =
            (ServiceCollectionExtensions.SseSourceRegistrationService)services
                .Single(d => d.ServiceType == typeof(ServiceCollectionExtensions.SseSourceRegistrationService))
                .ImplementationInstance!;
        Assert.Same(tracker.DefaultDescriptor, sseDescriptors.Last());
    }

    [Fact]
    public void AddSseSource_NamedRegisteredAfterDefault_DefaultDescriptorStaysLast()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddSseSource();
        services.AddSseSource("Events");

        // ASSERT
        ServiceDescriptor[] sseDescriptors =
            services.Where(d => d.ServiceType == typeof(SseSource) && !d.IsKeyedService).ToArray();
        ServiceCollectionExtensions.SseSourceRegistrationService tracker =
            (ServiceCollectionExtensions.SseSourceRegistrationService)services
                .Single(d => d.ServiceType == typeof(ServiceCollectionExtensions.SseSourceRegistrationService))
                .ImplementationInstance!;
        Assert.Same(tracker.DefaultDescriptor, sseDescriptors.Last());
    }

    [Fact]
    public void AddSseSource_DefaultAndMultipleNamed_GetRequiredService_ResolvesDefaultSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient(Constants.DefaultSourceName);
        services.AddHttpClient("Events");

        // ACT
        services.AddSseSource("Events");
        services.AddSseSource(); // default
        using ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();
        using SseSource resolvedViaRequiredService = provider.GetRequiredService<SseSource>();
        using SseSource resolvedViaFactory = factory.CreateSseSource(Constants.DefaultSourceName);

        // ASSERT
        Assert.NotSame(resolvedViaRequiredService, resolvedViaFactory);
        List<SseSource> allSources = provider.GetServices<SseSource>().ToList();
        Assert.Equal(2, allSources.Count);
        allSources.ForEach(s => s.Dispose());
    }

    [Fact]
    public void AddScopedSseSourceFactory_RegistersISseSourceFactory_AsScoped()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddScopedSseSourceFactory();

        // ASSERT
        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(ISseSourceFactory));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddScopedSseSourceFactory_RegistersDefaultSseSourceFactory_AsScoped()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddScopedSseSourceFactory();

        // ASSERT
        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(ISseSourceFactory));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddScopedSseSourceFactory_WhenCalledWithAddSseSource_BothFactoriesAreRegistered()
    {
        // ARRANGE
        ServiceCollection services = new();

        // ACT
        services.AddScopedSseSourceFactory();
        services.AddSseSource();

        // ASSERT
        ServiceDescriptor[] descriptors = services
            .Where(d => d.ServiceType == typeof(ISseSourceFactory))
            .ToArray();

        Assert.Single(descriptors,
            d => d.ServiceKey is null &&
                 d.Lifetime == ServiceLifetime.Singleton);
        Assert.Single(descriptors,
            d => d.ServiceKey?.ToString() == "ScopedSseSourceFactory" &&
                 d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddScopedSseSourceFactory_WhenResolvedWithinScope_ReturnsSameInstancePerScope()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddScopedSseSourceFactory();
        services.AddSseSource();
        using ServiceProvider rootProvider = services.BuildServiceProvider();

        // ACT
        using IServiceScope scope = rootProvider.CreateScope();
        ISseSourceFactory factory1 = scope.ServiceProvider.GetRequiredService<ISseSourceFactory>();
        ISseSourceFactory factory2 = scope.ServiceProvider.GetRequiredService<ISseSourceFactory>();

        // ASSERT
        Assert.Same(factory1, factory2);
    }

    [Fact]
    public void AddScopedSseSourceFactory_WhenResolvedFromTwoScopes_ReturnsDifferentInstances()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddScopedSseSourceFactory();
        services.AddSseSource();
        using ServiceProvider rootProvider = services.BuildServiceProvider();

        // ACT
        ISseSourceFactory factoryFromScope1;
        ISseSourceFactory factoryFromScope2;
        using (IServiceScope scope1 = rootProvider.CreateScope())
        {
            factoryFromScope1 = scope1.GetScopedSseSourceFactory();
        }

        using (IServiceScope scope2 = rootProvider.CreateScope())
        {
            factoryFromScope2 = scope2.GetScopedSseSourceFactory();
        }

        // ASSERT
        Assert.NotSame(factoryFromScope1, factoryFromScope2);
    }
}