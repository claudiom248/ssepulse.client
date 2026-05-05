using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.DependencyInjection.Tests;

public class DefaultSseSourceFactoryTests
{
    [Fact]
    public void CreateSseSource_WithDefaultName_CreatesNonDisposedSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource source = factory.CreateSseSource("Default");

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void CreateSseSource_WithCustomName_CreatesNonDisposedSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("CustomSource");
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource source = factory.CreateSseSource("CustomSource");

        // ASSERT
        Assert.False(source.IsConnected);
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
        using SseSource source1 = factory.CreateSseSource("Default");
        using SseSource source2 = factory.CreateSseSource("Default");

        // ASSERT
        Assert.NotSame(source1, source2);
    }

    [Fact]
    public void CreateSseSource_WithConfiguredOptions_AppliesOptionsToSource()
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
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        SseSourceOptions resolvedOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceOptions>>()
            .Get("TestSource");
        Assert.Equal(expectedPath, resolvedOptions.Path);
    }

    [Fact]
    public void CreateSseSource_ResolvesRequestMutators()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void CreateSseSource_WithMultipleMutators_IncludesAllMutators()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource");
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.False(source.IsConnected);
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
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.False(source.IsConnected);
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
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.False(source.IsConnected);
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
        using SseSource source = factory.CreateSseSource(null);

        // ASSERT
        Assert.False(source.IsConnected);
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
        using SseSource source1 = factory.CreateSseSource("Source1");
        using SseSource source2 = factory.CreateSseSource("Source2");

        // ASSERT
        Assert.NotSame(source1, source2);
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
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void CreateSseSource_ImplementsISseSourceFactory()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource();
        ServiceProvider provider = services.BuildServiceProvider();

        // ACT
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
        using SseSource source = factory.CreateSseSource(sourceName);

        // ASSERT
        SseSourceOptions resolvedOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceOptions>>()
            .Get(sourceName);
        Assert.Equal("/initial", resolvedOptions.Path);
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
        using SseSource createdSource = factory.CreateSseSource("TestSource");

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
        using SseSource source = factory.CreateSseSource("TestSource");

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
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        httpClientFactory.Received(1).CreateClient("TestSource");
    }

    [Fact]
    public void CreateSseSource_WhenLastEventIdIsEnabled_StoreIsSharedBetweenSourceAndMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("TestSource")
            .AddLastEventId();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource source = factory.CreateSseSource("TestSource");

        // ASSERT
        ILastEventIdStore sourceStore = GetSourceStore(source);
        ILastEventIdStore mutatorStore = GetMutatorStore(source);

        Assert.Same(sourceStore, mutatorStore);
    }

    [Fact]
    public void CreateSseSource_WhenTransientStoreIsRegisteredForMultipleSources_EachSourceGetsItsOwnStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddTransient<TransientLastEventIdStore>();
        services.AddSseSource("SourceA")
            .AddLastEventId<TransientLastEventIdStore>();
        services.AddSseSource("SourceB")
            .AddLastEventId<TransientLastEventIdStore>();
        ServiceProvider provider = services.BuildServiceProvider();
        ISseSourceFactory factory = provider.GetRequiredService<ISseSourceFactory>();

        // ACT
        using SseSource sourceA = factory.CreateSseSource("SourceA");
        using SseSource sourceB = factory.CreateSseSource("SourceB");

        // ASSERT
        ILastEventIdStore sourceAStore = GetSourceStore(sourceA);
        ILastEventIdStore sourceBStore = GetSourceStore(sourceB);

        Assert.NotSame(sourceAStore, sourceBStore);
        Assert.Same(sourceAStore, GetMutatorStore(sourceA));
        Assert.Same(sourceBStore, GetMutatorStore(sourceB));
    }

    private static ILastEventIdStore GetSourceStore(SseSource source)
    {
        object? store = typeof(SseSource)
            .GetField("_lastEventIdStore", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(source);

        return Assert.IsAssignableFrom<ILastEventIdStore>(store);
    }

    private static ILastEventIdStore GetMutatorStore(SseSource source)
    {
        object? connection = typeof(SseSource)
            .GetField("_connection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(source);

        object? mutators = connection?.GetType()
            .GetField("_requestMutators", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(connection);

        IRequestMutator lastEventMutator = Assert.Single(Assert.IsAssignableFrom<IReadOnlyCollection<IRequestMutator>>(mutators));
        LastEventIdRequestMutator typedMutator = Assert.IsType<LastEventIdRequestMutator>(lastEventMutator);

        object? store = typeof(LastEventIdRequestMutator)
            .GetField("_lastEventIdStore", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(typedMutator);

        return Assert.IsAssignableFrom<ILastEventIdStore>(store);
    }

    private class TransientLastEventIdStore : ILastEventIdStore
    {
        public string? LastEventId { get; private set; }

        public void Set(string eventId)
        {
            LastEventId = eventId;
        }
    }
}

