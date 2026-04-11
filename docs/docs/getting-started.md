# Getting Started

## Packages

SsePulse is split into small, focused packages so you only take what you need.

| Package | Description |
|---|---|
| `SsePulse.Client` | Core SSE client. Contains `SseSource`, handler registration, and all runtime logic. Required by every app. |
| `SsePulse.Client.DependencyInjection` | Integrates `SseSource` with `IServiceCollection`. Provides the `AddSseSource()` fluent builder and `ISseSourceFactory` for named sources. |
| `SsePulse.Client.Authentication` | Authentication providers (Bearer token, Basic, API key). Usable standalone or with DI. |
| `SsePulse.Client.Authentication.DependencyInjection` | Plugs the authentication providers into the DI builder via `AddAuthentication()` extension methods. |

Install only what your project needs:

```bash
dotnet add package SsePulse.Client
dotnet add package SsePulse.Client.DependencyInjection
dotnet add package SsePulse.Client.Authentication
dotnet add package SsePulse.Client.Authentication.DependencyInjection
```

---

## Quickstart

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://my-server.example") };
await using var source = new SseSource(httpClient, new SseSourceOptions { Path = "/events" });

source.On<OrderCreated>(e => Console.WriteLine($"Order {e.Id} created"));

await source.StartConsumeAsync(CancellationToken.None);
```

That's it. `StartConsumeAsync` blocks until the stream ends or the token is cancelled.

---

## Standalone usage

### 1. Create an `SseSource`

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://my-server.example") };
var options = new SseSourceOptions { Path = "/events" };

await using var source = new SseSource(httpClient, options);
```

### 2. Register handlers

Register handlers **before** calling `StartConsumeAsync`.

**Raw string data**

```csharp
source.On("ping", (string data) => Console.WriteLine($"Ping: {data}"));
```

**Strongly-typed JSON deserialization**

```csharp
source.On<OrderCreated>((OrderCreated e) => Console.WriteLine($"Order {e.Id} created"));
```

The event name defaults to the type name transformed by the configured `DefaultEventNameCasePolicy`
(default: `PascalCase`, so `OrderCreated` maps to the SSE event `OrderCreated`).

**Full `SseItem<T>` with metadata**

```csharp
source.OnItem<OrderCreated>((SseItem<OrderCreated> item) =>
{
    Console.WriteLine($"Event id: {item.EventId}, data: {item.Data.Id}");
});
```

**Bind an events manager class**

```csharp
public class MyEventsManager : ISseEventsManager
{
    public void OnOrderCreated(OrderCreated e) => Console.WriteLine(e.Id);
    public void OnOrderShipped(OrderShipped e) => Console.WriteLine(e.Id);
}

source.Bind<MyEventsManager>();
```

`Bind<T>()` scans all public methods named `On*` with a single parameter and registers them
automatically. Use `[MapEventName(EventName = "custom-name")]` on a method to override the
derived event name.

### 3. Start consuming

```csharp
await source.StartConsumeAsync(CancellationToken.None);
```

`StartConsumeAsync` runs until the stream ends, the token is cancelled, or `Stop()`/`StopAsync()` is called.

---

## Dependency injection

Register a source on the container and resolve it at runtime. See the [Dependency Injection](dependency-injection.md) page for the full reference.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .BindEventsManager<MyEventsManager>();
```

---

## Error handling

```csharp
source
    .OnError(ex => Console.Error.WriteLine($"Handler error: {ex}"))
    .OnConnectionEstablished(() => Console.WriteLine("Connected"))
    .OnConnectionLost(ex => Console.WriteLine($"Connection lost: {ex?.Message}"));
```
