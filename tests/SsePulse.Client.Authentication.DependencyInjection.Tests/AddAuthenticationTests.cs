using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Authentication.DependencyInjection.Tests;


public class AddAuthenticationTests
{
    [Fact]
    public void AddAuthentication_WithoutArgs_RegistersAuthenticationRequestMutator_AsTransient()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication();

        // ASSERT
        ServiceDescriptor descriptor = Assert.Single(services,
            d => d.ServiceType == typeof(AuthenticationRequestMutator));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddAuthentication_WithoutArgs_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<ISseAuthenticationProvider>());
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication();

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddAuthentication_WithoutArgs_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<ISseAuthenticationProvider>());
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddAuthentication_WithProviderInstance_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication(Substitute.For<ISseAuthenticationProvider>());

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddAuthentication_WithProviderInstance_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication(Substitute.For<ISseAuthenticationProvider>());

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddAuthentication_WithGenericType_FactoryResolvesProviderFromContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton<FakeAuthenticationProvider>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication<FakeAuthenticationProvider>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddAuthentication_WithFactory_FactoryIsInvokedWithServiceProvider()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        IServiceProvider? capturedProvider = null;

        // ACT
        builder.AddAuthentication(sp =>
        {
            capturedProvider = sp;
            return Substitute.For<ISseAuthenticationProvider>();
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        GetOptions(provider, "MySource").RequestMutatorsFactories[0](provider);
        Assert.Same(provider, capturedProvider);
    }

    [Fact]
    public void AddAuthentication_WithFactory_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddAuthentication(_ => Substitute.For<ISseAuthenticationProvider>());

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    private static SseSourceFactoryOptions BuildAndGetOptions(ServiceCollection services, string sourceName)
        => GetOptions(services.BuildServiceProvider(), sourceName);

    private static SseSourceFactoryOptions GetOptions(ServiceProvider provider, string sourceName)
        => provider.GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>().Get(sourceName);
}

internal class FakeAuthenticationProvider : ISseAuthenticationProvider
{
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken) => default;
}
