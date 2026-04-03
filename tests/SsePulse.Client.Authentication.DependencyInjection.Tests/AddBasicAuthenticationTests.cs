using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Authentication.DependencyInjection.Tests;

public class AddBasicAuthenticationTests
{
    [Fact]
    public void AddBasicAuthentication_WithoutArgs_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication();

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBasicAuthentication_WithCredentialsInstance_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication(new BasicCredentials("alice", "secret"));

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBasicAuthentication_WithCredentialsInstance_FactoryProducesAuthenticationRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication(new BasicCredentials("alice", "secret"));

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions options = GetOptions(provider, "MySource");
        Assert.IsType<AuthenticationRequestMutator>(options.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddBasicAuthentication_WithConfigureAction_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication(creds => { creds.Username = "alice"; creds.Password = "secret"; });

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBasicAuthentication_WithConfigureAction_RegistersNamedCredentialsUnderSourceName()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication(creds =>
        {
            creds.Username = "alice";
            creds.Password = "secret";
        });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        BasicCredentials resolved = provider
            .GetRequiredService<IOptionsMonitor<BasicCredentials>>()
            .Get("MySource");
        Assert.Equal("alice", resolved.Username);
        Assert.Equal("secret", resolved.Password);
    }

    [Fact]
    public void AddBasicAuthentication_WithConfigureAction_TwoNamedSources_HaveIndependentCredentials()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder source1 = new SseSourceBuilder("Source1", services);
        ISseSourceBuilder source2 = new SseSourceBuilder("Source2", services);

        // ACT
        source1.AddBasicAuthentication(creds => { creds.Username = "user1"; creds.Password = "pass1"; });
        source2.AddBasicAuthentication(creds => { creds.Username = "user2"; creds.Password = "pass2"; });

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<BasicCredentials> monitor = provider.GetRequiredService<IOptionsMonitor<BasicCredentials>>();
        Assert.Equal("user1", monitor.Get("Source1").Username);
        Assert.Equal("user2", monitor.Get("Source2").Username);
    }

    [Fact]
    public void AddBasicAuthentication_WithIConfiguration_AddsOneFactoryToNamedSourceOptions()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Username"] = "alice",
                ["Password"] = "secret"
            })
            .Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddBasicAuthentication(config);

        // ASSERT
        SseSourceFactoryOptions options = BuildAndGetOptions(services, "MySource");
        Assert.Single(options.RequestMutatorsFactories);
    }

    [Fact]
    public void AddBasicAuthentication_WithEmptyIConfiguration_ThrowsInvalidOperationException()
    {
        // ARRANGE
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder().Build();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT & ASSERT
        Assert.Throws<InvalidOperationException>(() => builder.AddBasicAuthentication(config));
    }

    private static SseSourceFactoryOptions BuildAndGetOptions(ServiceCollection services, string sourceName)
        => GetOptions(services.BuildServiceProvider(), sourceName);

    private static SseSourceFactoryOptions GetOptions(ServiceProvider provider, string sourceName)
        => provider.GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>().Get(sourceName);
}
