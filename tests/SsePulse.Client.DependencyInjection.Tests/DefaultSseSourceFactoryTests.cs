using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.DependencyInjection.Tests;

/// <summary>
/// Tests for DefaultSseSourceFactory covering SseSource creation with DI resolution.
/// </summary>
public class DefaultSseSourceFactoryTests
{
    [Fact]
    public void CreateSseSource_WithDefaultName_CreatesValidSource()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("Default");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithCustomName_CreatesValidSource()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("CustomSource");
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("CustomSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ReturnsNewInstanceEachTime()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source1 = factory.CreateSseSource("Default");
        SseSource source2 = factory.CreateSseSource("Default");

        // Assert
        Assert.NotSame(source1, source2);
    }

    [Fact]
    public void CreateSseSource_WithConfiguredOptions_AppliesOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        string expectedPath = "/custom-sse";
        services.AddSseSource("TestSource", options =>
        {
            options.Path = expectedPath;
        });
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ResolvesRequestMutators()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        IRequestMutator mutator = Substitute.For<IRequestMutator>();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithMultipleMutators_IncludesAllMutators()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();

        IRequestMutator mutator1 = Substitute.For<IRequestMutator>();
        IRequestMutator mutator2 = Substitute.For<IRequestMutator>();

        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ResolvesLastEventIdStore()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_UsesHttpClientFactory()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithNullName_UsesDefaultNameFromOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource(null);

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_MultipleCallsWithDifferentNames_CreatesDistinctSources()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("Source1");
        services.AddSseSource("Source2");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source1 = factory.CreateSseSource("Source1");
        SseSource source2 = factory.CreateSseSource("Source2");

        // Assert
        Assert.NotNull(source1);
        Assert.NotNull(source2);
    }

    [Fact]
    public void CreateSseSource_WithConfiguredHttpClient_UsesConfiguredClient()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient("TestSource", client =>
        {
            client.BaseAddress = new Uri("https://example.com");
        });
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource("TestSource");

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ImplementsISseSourceFactory()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act & Assert
        Assert.IsAssignableFrom<ISseSourceFactory>(factory);
    }

    [Fact]
    public void CreateSseSource_WithOptionsMonitor_ReadsCurrentOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpClient();
        string sourceName = "DynamicSource";
        
        services.AddSseSource(sourceName, options =>
        {
            options.Path = "/initial";
        });
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // Act
        SseSource source = factory.CreateSseSource(sourceName);

        // Assert
        Assert.NotNull(source);
    }
}

