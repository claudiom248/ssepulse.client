using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SsePulse.Client.DependencyInjection.Extensions;
using SsePulse.Client.Hosting.DependencyInjection.Extensions;

namespace SsePulse.Client.Hosting.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSseSourcesHostedServices_AddsConsumersHostedServices()
    {
        //ARRANGE
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSseSource("First");
        services.AddSseSource("Second");
        
        //ACT
        services.AddSseSourcesHostedServices();
        
        //ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IHostedService> resolvedServices = provider.GetServices<IHostedService>();
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IHostedService)));
        Assert.Equal(2, resolvedServices.OfType<SseSourceHostedService>().Count());
    }
    
    [Fact]
    public void AddSseSourcesHostedServices_WhenNoSourceIsRegistered_ThrowsException()
    {
        //ARRANGE
        ServiceCollection services = new();
        
        //ACT & ASSERT
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(services.AddSseSourcesHostedServices);
        Assert.Equal($"No SseSource has been registered in the service collection.", exception.Message);
    }
}