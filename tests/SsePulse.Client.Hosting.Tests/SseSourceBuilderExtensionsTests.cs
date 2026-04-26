using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SsePulse.Client.Core;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;
using SsePulse.Client.Hosting.DependencyInjection.Extensions;

namespace SsePulse.Client.Hosting.Tests;

public class SseSourceBuilderExtensionsTests
{
    [Fact]
    public void AddHostedService_AddsHostedServiceToContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder  = services
            .AddSseSource("MySource")
            .AddHttpClient();
        
        // ACT
        builder.AddHostedService();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IHostedService service = provider.GetRequiredService<IHostedService>();
        Assert.Single(services, s => s.ServiceType == typeof(IHostedService));
        Assert.IsType<SseSourceHostedService>(service);
    }

    [Fact]
    public void AddHostedService_GenericOverload_AddsHostedServiceOfSpecifiedTypeToContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder  = services
            .AddSseSource("MySource")
            .AddHttpClient();
        
        // ACT
        builder.AddHostedService<MockBackgroundService>();

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IHostedService service = provider.GetRequiredService<IHostedService>();
        Assert.Single(services, s => s.ServiceType == typeof(IHostedService));
        Assert.IsType<MockBackgroundService>(service);
    }
    
    [Fact]
    public void AddHostedService_FactoryOverload_AddsHostedServiceOfSpecifiedTypeToContainer()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder  = services
            .AddSseSource("MySource")
            .AddHttpClient();
        
        // ACT
        builder.AddHostedService((sp) => new MockBackgroundService(sp.GetRequiredService<SseSource>()));

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IHostedService service = provider.GetRequiredService<IHostedService>();
        Assert.Single(services, s => s.ServiceType == typeof(IHostedService));
        Assert.IsType<MockBackgroundService>(service);
    }
    
    private class MockBackgroundService : BackgroundService
    {
        private readonly SseSource _source;

        public MockBackgroundService(SseSource source)
        {
            _source = source;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}