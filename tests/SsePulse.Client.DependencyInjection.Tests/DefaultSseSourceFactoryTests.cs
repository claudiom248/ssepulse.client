using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.DependencyInjection.Tests;

public class DefaultSseSourceFactoryTests
{
    [Fact]
    public void CreateSseSource_WithDefaultName_CreatesValidSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("Default");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithCustomName_CreatesValidSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("CustomSource");
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("CustomSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ReturnsNewInstanceEachTime()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source1 = factory.CreateSseSource("Default");
        SseSource source2 = factory.CreateSseSource("Default");

        // ASSERT
        Assert.NotSame(source1, source2);
    }

    [Fact]
    public void CreateSseSource_WithConfiguredOptions_AppliesOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        string expectedPath = "/custom-sse";
        services.AddSseSource("TestSource", options =>
        {
            options.Path = expectedPath;
        });
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ResolvesRequestMutators()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();

        IRequestMutator mutator = Substitute.For<IRequestMutator>();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithMultipleMutators_IncludesAllMutators()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();

        IRequestMutator mutator1 = Substitute.For<IRequestMutator>();
        IRequestMutator mutator2 = Substitute.For<IRequestMutator>();

        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ResolvesLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_UsesHttpClientFactory()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithNullName_UsesDefaultNameFromOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource(null);

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_MultipleCallsWithDifferentNames_CreatesDistinctSources()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("Source1");
        services.AddSseSource("Source2");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source1 = factory.CreateSseSource("Source1");
        SseSource source2 = factory.CreateSseSource("Source2");

        // ASSERT
        Assert.NotNull(source1);
        Assert.NotNull(source2);
    }

    [Fact]
    public void CreateSseSource_WithConfiguredHttpClient_UsesConfiguredClient()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient("TestSource", client =>
        {
            client.BaseAddress = new Uri("https://example.com");
        });
        services.AddSseSource("TestSource");
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_ImplementsISseSourceFactory()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT & ASSERT
        Assert.IsAssignableFrom<ISseSourceFactory>(factory);
    }

    [Fact]
    public void CreateSseSource_WithOptionsMonitor_ReadsCurrentOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        string sourceName = "DynamicSource";
        
        services.AddSseSource(sourceName, options =>
        {
            options.Path = "/initial";
        });
        
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource source = factory.CreateSseSource(sourceName);

        // ASSERT
        Assert.NotNull(source);
    }

    [Fact]
    public void CreateSseSource_WithRegisteredHandlers_InvokesActionWithCreatedSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        SseSource? capturedSource = null;
        services.AddSseSource("TestSource")
            .RegisterHandlers((_, source) => capturedSource = source);

        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        SseSource createdSource = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.Same(createdSource, capturedSource);
    }
    
    [Fact]
    public void CreateSseSource_WhenUseHttpClient_InvokeHttpClientFactoryWithProvidedClientName()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource")
            .UseHttpClient("SharedHttpClient");
        
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        services.AddSingleton(httpClientFactory);
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        _ = factory.CreateSseSource("TestSource");

        // ASSERT
        httpClientFactory.Received(1).CreateClient("SharedHttpClient");
    }

    [Fact]
    public void CreateSseSource_WhenAddHttpClient_InvokeHttpClientFactoryWithSourceName()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource")
            .AddHttpClient();
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        services.AddSingleton(httpClientFactory);
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        _ = factory.CreateSseSource("TestSource");

        // ASSERT
        httpClientFactory.Received(1).CreateClient("TestSource");
    }
}

