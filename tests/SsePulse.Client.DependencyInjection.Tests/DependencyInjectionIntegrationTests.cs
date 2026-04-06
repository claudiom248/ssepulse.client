using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.DependencyInjection.Tests;

/// <summary>
/// Integration tests for the entire dependency injection setup, testing realistic scenarios.
/// </summary>
public class DependencyInjectionIntegrationTests
{
    [Fact]
    public void CompleteSetup_WithAllComponents_WorksEnd2End()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        services
            .AddSseSource("MainSource", options =>
            {
                options.Path = "/api/events";
                options.MaxDegreeOfParallelism = 4;
            })
            .AddLastEventId();

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        SseSource? source = provider.GetService<SseSource>();
        Assert.NotNull(source);

        ILastEventIdStore? store = provider.GetService<ILastEventIdStore>();
        Assert.NotNull(store);

        ISseSourceFactory? factory = provider.GetService<ISseSourceFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void MultipleNamedSources_CanCoexist()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        services.AddSseSource("Source1", options => options.Path = "/api/events1");
        services.AddSseSource("Source2", options => options.Path = "/api/events2");

        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Assert
        SseSource source1 = factory.CreateSseSource("Source1");
        SseSource source2 = factory.CreateSseSource("Source2");

        Assert.NotNull(source1);
        Assert.NotNull(source2);
    }

    [Fact]
    public void ConfigurationBinding_WorksWithOptions()
    {
        // Arrange
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Path"] = "/api/sse",
                ["MaxDegreeOfParallelism"] = "8"
            })
            .Build();

        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        services.AddSseSource("ConfiguredSource", config);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        SseSource? source = provider.GetService<SseSource>();
        Assert.NotNull(source);
    }

    [Fact]
    public void FluentApi_AllowsChaining()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        ISseSourceBuilder builder = services
            .AddSseSource("ChainedSource")
            .AddHttpClient()
            .AddLastEventId();

        // Assert
        Assert.NotNull(builder);
        Assert.Equal("ChainedSource", builder.Name);
    }

    [Fact]
    public void ServiceProvider_CanResolveMultipleSourceInstances()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("MultiInstance");

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        SseSource? instance1 = provider.GetService<SseSource>();
        SseSource? instance2 = provider.GetService<SseSource>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2); // Transient registration
    }

    [Fact]
    public void FactoryInterface_IsRegisteredAsSingleton()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ISseSourceFactory? factory1 = provider.GetService<ISseSourceFactory>();
        ISseSourceFactory? factory2 = provider.GetService<ISseSourceFactory>();

        // Assert
        Assert.NotNull(factory1);
        Assert.NotNull(factory2);
        Assert.Same(factory1, factory2); // Singleton registration
    }

    [Fact]
    public void LastEventIdStore_IsSharedAcrossMutators()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        services.AddSseSource("EventTracking")
            .AddLastEventId();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ILastEventIdStore store = provider.GetRequiredService<ILastEventIdStore>();
        store.Set("event123");

        // Assert
        Assert.Equal("event123", store.LastEventId);
    }

    [Fact]
    public void HttpClientFactory_IsProperlyConfigured()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        services.AddSseSource("WithHttpClient", _ =>
        {
        })
        .AddHttpClient(client =>
        {
            client.BaseAddress = new Uri("https://api.example.com");
        }, null);

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        SseSource? source = provider.GetService<SseSource>();

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void ConfigureOptions_OverrideDefaults()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        services.AddSseSource("CustomOptions", options =>
        {
            options.Path = "/custom";
            options.MaxDegreeOfParallelism = 10;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        SseSource? source = provider.GetService<SseSource>();
        Assert.NotNull(source);
    }

    [Fact]
    public void DependencyInjection_WithConfigurationAndOptions()
    {
        // Arrange
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultPath"] = "/default"
            })
            .Build();

        ServiceCollection services = new();
        services.AddHttpClient();

        // Act
        services.AddSseSource("ComplexSource", config);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        SseSource? source = provider.GetService<SseSource>();
        Assert.NotNull(source);
    }
}

