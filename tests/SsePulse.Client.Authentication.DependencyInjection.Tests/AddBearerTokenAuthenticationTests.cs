using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Authentication.DependencyInjection.Tests;


public class AddBearerTokenAuthenticationTests
{
    [Fact]
    public void AddBearerTokenAuthentication_WithoutArgs_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ITokenProvider tokenProvider = Substitute.For<ITokenProvider>();
        services.AddSingleton(tokenProvider);
        services.AddTransient<BearerTokenAuthenticationProvider>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBearerTokenAuthentication();

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithoutArgs_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ITokenProvider tokenProvider = Substitute.For<ITokenProvider>();
        services.AddSingleton(tokenProvider);
        services.AddTransient<BearerTokenAuthenticationProvider>();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBearerTokenAuthentication();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithTokenProvider_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBearerTokenAuthentication(Substitute.For<ITokenProvider>());

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithTokenProvider_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBearerTokenAuthentication(Substitute.For<ITokenProvider>());

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        IRequestMutator requestMutator = options.RequestMutatorsFactories[0](provider);
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithStaticTokenConfiguration_AddsOneFactory()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProviderName"] = "StaticTokenProvider",
                ["Token"] = "static-bearer-token"
            })
            .Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBearerTokenAuthentication(config);

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithEnvironmentVariableConfiguration_AddsOneFactory()
    {
        // ARRANGE
        const string envVarName = "SSEPULSE_TEST_BEARER_TOKEN";
        Environment.SetEnvironmentVariable(envVarName, "env-token");
        try
        {
            ServiceCollection services = new();
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProviderName"] = "EnvironmentVariableTokenProvider",
                    ["EnvironmentVariable"] = envVarName
                })
                .Build();
            ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

            // ACT
            builder.AddBearerTokenAuthentication(config);

            // ASSERT
            SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
            Assert.Single(options.RequestMutatorsFactories);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void AddBearerTokenAuthentication_WithUnknownProviderName_ThrowsArgumentOutOfRangeException()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ProviderName"] = "UnknownProvider" })
            .Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => builder.AddBearerTokenAuthentication(config));
    }

    private static SseSourceFactoryOptions BuildAndGetOptions(ServiceCollection services, string sourceName)
        => GetOptions(services.BuildServiceProvider(), sourceName);

    private static SseSourceFactoryOptions GetOptions(ServiceProvider provider, string sourceName)
        => provider.GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>().Get(sourceName);
}
