using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.Configurations;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Authentication.DependencyInjection.Tests;

public class AddApiKeyAuthenticationTests
{
    [Fact]
    public void AddApiKeyAuthentication_WithoutArgs_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddTransient(_ => new ApiKeyAuthenticationProvider(new ApiKeyAuthenticationProviderConfiguration { Key = "k" }));
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication();

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithConfigurationInstance_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication(new ApiKeyAuthenticationProviderConfiguration { Key = "my-key", Header = "X-API-Key" });

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithConfigurationInstance_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication(new ApiKeyAuthenticationProviderConfiguration { Key = "my-key" });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddApiKeyAuthentication_WithConfigureAction_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication(cfg => { cfg.Key = "my-key"; cfg.Header = "X-API-Key"; });

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithConfigureAction_RegistersNamedConfigurationUnderSourceName()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication(cfg =>
        {
            cfg.Key = "my-key";
            cfg.Header = "X-Custom-Header";
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        ApiKeyAuthenticationProviderConfiguration resolved = provider
            .GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration>>()
            .Get("MySource");
        Assert.Equal("my-key", resolved.Key);
        Assert.Equal("X-Custom-Header", resolved.Header);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithConfigureAction_TwoNamedSources_HaveIndependentConfigurations()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder source1 = new SseSourceBuilder("Source1", services);
        ISseSourceBuilder source2 = new SseSourceBuilder("Source2", services);

        // ACT
        source1.AddApiKeyAuthentication(cfg => { cfg.Key = "key-1"; cfg.Header = "X-Key-1"; });
        source2.AddApiKeyAuthentication(cfg => { cfg.Key = "key-2"; cfg.Header = "X-Key-2"; });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration> monitor =
            provider.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration>>();
        Assert.Equal("key-1", monitor.Get("Source1").Key);
        Assert.Equal("key-2", monitor.Get("Source2").Key);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithIConfiguration_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Args:Key"] = "my-key",
                ["Args:Header"] = "X-API-Key"
            })
            .Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddApiKeyAuthentication(config);

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddApiKeyAuthentication_WithEmptyIConfiguration_ThrowsInvalidOperationException()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder().Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT & ASSERT
        Assert.Throws<InvalidOperationException>(() => builder.AddApiKeyAuthentication(config));
    }

    private static SseSourceFactoryOptions BuildAndGetOptions(ServiceCollection services, string sourceName)
        => GetOptions(services.BuildServiceProvider(), sourceName);

    private static SseSourceFactoryOptions GetOptions(ServiceProvider provider, string sourceName)
        => provider.GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>().Get(sourceName);
}
