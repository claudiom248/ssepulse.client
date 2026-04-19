# Dependency Injection

`SsePulse.Client.DependencyInjection` integrates `SseSource` with the standard
`IServiceCollection` / Generic Host model. All registrations return an `ISseSourceBuilder`
that you chain to configure the HTTP client, handlers, and optional features.

---

## Registering a source

### Minimal registration

No options are configured; defaults from `SseSourceOptions` apply.

```csharp
services.AddSseSource();
```

### With inline options

```csharp
services.AddSseSource(options =>
{
    options.Path = "/events";
    options.MaxDegreeOfParallelism = 4;
});
```

### From configuration

Bind `SseSourceOptions` from an `IConfiguration` section. Every property in the section
that matches a property on `SseSourceOptions` is bound automatically.

```csharp
// appsettings.json
// {
//   "SseSource": {
//     "Path": "/events",
//     "MaxDegreeOfParallelism": 2
//   }
// }

services.AddSseSource(configuration.GetSection("SseSource"));
```

### Named sources

Register multiple independent sources under unique names. Resolve them at runtime through
`ISseSourceFactory`.

```csharp
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"));

services
    .AddSseSource("notifications", options => options.Path = "/notifications/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://notifications.example"));
```

Resolve by name:

```csharp
public class OrdersWorker(ISseSourceFactory factory)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using SseSource source = factory.Create("orders");
        source.On<OrderCreated>(e => Console.WriteLine(e.Id));
        await source.StartConsumeAsync(ct);
    }
}
```

---

## Configuring the HTTP client

### Register a new HTTP client

Creates and names an `HttpClient` owned by this source.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://my-server.example");
        client.Timeout = TimeSpan.FromMinutes(10);
    });
```

### Advanced HTTP client setup

Access the full `IHttpClientBuilder` for message handlers, Polly policies, etc.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(
        configureClient: client => client.BaseAddress = new Uri("https://my-server.example"),
        clientBuilder: builder => builder.AddHttpMessageHandler<MyDelegatingHandler>()
    );
```

### Reuse an existing HTTP client

Point this source at an `HttpClient` that was registered elsewhere.

```csharp
services.AddHttpClient("shared-client", client =>
    client.BaseAddress = new Uri("https://my-server.example"));

services
    .AddSseSource(options => options.Path = "/events")
    .UseHttpClient("shared-client");
```

---

## Registering handlers

### Inline handler registration

The callback receives the `IServiceProvider` so you can resolve services when wiring up handlers.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .RegisterHandlers((sp, source) =>
    {
        ILogger logger = sp.GetRequiredService<ILogger<MyWorker>>();
        source.On<OrderCreated>(e => logger.LogInformation("Order {Id} created", e.Id));
        source.On<OrderShipped>(e => logger.LogInformation("Order {Id} shipped", e.Id));
    });
```

### Bind a pre-created events manager

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .BindEventsManager(new MyEventsManager());
```

### Bind an events manager resolved from DI

The manager type must already be registered in the container.

```csharp
services.AddTransient<MyEventsManager>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .BindEventsManager<MyEventsManager>();
```

### Bind an events manager with a factory

Use a factory delegate for full control over construction.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .BindEventsManager(sp =>
    {
        IMyDependency dep = sp.GetRequiredService<IMyDependency>();
        return new MyEventsManager(dep);
    });
```

---

## Last-event-ID resumption

When the connection drops and reconnects, the `Last-Event-ID` header tells the server where
to resume. Call `AddLastEventId()` on the builder to enable this with the built-in in-memory store.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddLastEventId();
```

### Custom store

Provide your own `ILastEventIdStore` for distributed or persistent scenarios (e.g. Redis).

```csharp
services.AddSingleton<ILastEventIdStore, RedisLastEventIdStore>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddLastEventId<RedisLastEventIdStore>();
```

---

## Resolving a default source

When only one source is registered, inject `SseSource` directly.

```csharp
public class MyWorker(SseSource source)
{
    public async Task RunAsync(CancellationToken ct)
    {
        source.On<OrderCreated>(e => Console.WriteLine(e.Id));
        await source.StartConsumeAsync(ct);
    }
}
```

## Resolving named sources

Use `ISseSourceFactory` when you registered more than one source, or when you want explicit
control over the source lifetime.

```csharp
public class MyWorker(ISseSourceFactory factory)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using SseSource orders = factory.Create("orders");
        await using SseSource notifications = factory.Create("notifications");

        orders.On<OrderCreated>(e => Console.WriteLine(e.Id));
        notifications.On<Notification>(n => Console.WriteLine(n.Message));

        Task ordersTask = orders.StartConsumeAsync(ct);
        Task notificationsTask = notifications.StartConsumeAsync(ct);

        await Task.WhenAll(ordersTask, notificationsTask);
    }
}
```

