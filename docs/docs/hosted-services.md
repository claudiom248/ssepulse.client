# Hosted Services

`SsePulse.Client.Hosting` integrates SSE consumption with `Microsoft.Extensions.Hosting` so your stream consumption can run as host-managed background services.

This is useful when the consumption of events should start automatically when the host starts and stop gracefully during shutdown.

## Default Hosted Service per source

Register a source and add the built-in hosted service:

```csharp
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"))
    .AddHostedService();
```

At runtime, the hosted service resolves the named source from `ISseSourceFactory` and starts the consume loop.

## Register default Hosted Services for all sources

If you register multiple sources, you can add hosted services for all of them in one step by calling `AddSseSourcesHostedServices`:

```csharp
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"));

services
    .AddSseSource("notifications", options => options.Path = "/notifications/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://notifications.example"));

services.AddSseSourcesHostedServices();
```

Each source runs with its own hosted service instance, enabling multiple, independent, consumption from the sources.

## Custom Hosted Service

You can register a custom hosted service when you need extra startup logic or custom error handling:

```csharp
public class OrdersHostedService : BackgroundService
{
    private readonly SseSource _source;

    public OrdersHostedService(SseSource source)
    {
        _source = source;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _source.On<OrderCreated>(e => Console.WriteLine($"Order {e.Id} created"));
        return _source.StartConsumeAsync(stoppingToken);
    }
}

//it's possible to add the custom hosted service by specifing the type parameter
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"))
    .AddHostedService<OrdersHostedService>();

//or by using a factory
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"))
    .AddHostedService(sp => new OrdersHostedService(sp.GetRequiredService<ISseSourceFactory>().Get("orders"));
```