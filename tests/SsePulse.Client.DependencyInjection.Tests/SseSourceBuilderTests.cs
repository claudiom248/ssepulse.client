using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Tests;

public class SseSourceBuilderTests
{
    [Fact]
    public void AddRequestMutator_WithInstance_OptionsContainOneFactory()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        FakeRequestMutator mutator = new();

        // ACT
        builder.AddRequestMutator(mutator);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddRequestMutator_WithInstance_FactoryProducesTheProvidedMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        FakeRequestMutator mutator = new();

        // ACT
        builder.AddRequestMutator(mutator);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator resolved = options.RequestMutatorsFactories[0](provider);
        Assert.Same(mutator, resolved);
    }

    [Fact]
    public void AddRequestMutator_WithType_FactoryResolvesInstanceFromContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<FakeRequestMutator>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddRequestMutator<FakeRequestMutator>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator resolved = options.RequestMutatorsFactories[0](provider);
        Assert.IsType<FakeRequestMutator>(resolved);
    }

    [Fact]
    public void AddRequestMutator_WithDelegate_DelegateIsInvokedWithServiceProvider()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient<FakeRequestMutator>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        IServiceProvider? capturedProvider = null;

        // ACT
        builder.AddRequestMutator(sp =>
        {
            capturedProvider = sp;
            return sp.GetRequiredService<FakeRequestMutator>();
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        options.RequestMutatorsFactories[0](provider);
        Assert.Same(provider, capturedProvider);
    }
    
    [Fact]
    public void AddRequestMutator_WithType_FactoryResolvesSameInstances_WhenRegisteredAsSingleton()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton<FakeRequestMutator>();
        ISseSourceBuilder source1 = new SseSourceBuilder("MySource", services);
        ISseSourceBuilder source2 = new SseSourceBuilder("MySource1", services);

        // ACT
        source1.AddRequestMutator<FakeRequestMutator>();
        source2.AddRequestMutator<FakeRequestMutator>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options1 = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");
        SseSourceFactoryOptions options2 = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        IRequestMutator m1 = options1.RequestMutatorsFactories[0](provider);
        IRequestMutator m2 = options2.RequestMutatorsFactories[0](provider);
        Assert.Same(m1, m2);
    }

    [Fact]
    public void AddRequestMutator_CalledMultipleTimes_AllMutatorsStoredInOrder()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        FakeRequestMutator first = new();
        FakeRequestMutator second = new();

        // ACT
        builder.AddRequestMutator(first);
        builder.AddRequestMutator(second);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.Equal(2, options.RequestMutatorsFactories.Count);
        Assert.Same(first, options.RequestMutatorsFactories[0](provider));
        Assert.Same(second, options.RequestMutatorsFactories[1](provider));
    }

    [Fact]
    public void AddRequestMutator_OnNamedSource_DoesNotPolluteMutatorListOfOtherSource()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder source1 = new SseSourceBuilder("Source1", services);
        ISseSourceBuilder source2 = new SseSourceBuilder("Source2", services);

        // ACT
        source1.AddRequestMutator(new FakeRequestMutator());

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options2 = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("Source2");

        Assert.Empty(options2.RequestMutatorsFactories);
    }

    [Fact]
    public void AddHttpClient_WithDelegate_HttpClientContainsBearer()
    {
        // ARRANGE
        ServiceCollection services = new();
        SseSourceBuilder builder = new("MySource", services);

        // ACT
        builder.AddHttpClient();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("MySource");

        Assert.NotNull(client);
    }

    [Fact]
    public void AddHttpClient_WithConfigureCallback_CallbackIsApplied()
    {
        // ARRANGE
        ServiceCollection services = new();
        SseSourceBuilder builder = new("MySource", services);

        // ACT
        builder.AddHttpClient(client =>
        {
            client.BaseAddress = new Uri("https://api.example.com");
        }, null);

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("MySource");

        Assert.Equal(new Uri("https://api.example.com"), client.BaseAddress);
    }
    
    private class FakeRequestMutator : IRequestMutator
    {
        public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
            => default;
    }
}