# Distributed Cache Last-Event-ID Store

[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.DistributedCache?label=SsePulse.Client.Extensions.Stores.DistributedCache)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.DistributedCache)
[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection?label=SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection)

Persists the last event ID to any `IDistributedCache` implementation (Redis, SQL Server,
in-memory, or any other backend) so the SSE connection can be resumed after a process restart or
a redeployment.

If the cache is unavailable at the time of a `Set` call, the error is logged at `Error` level but
is never surfaced to the caller. Unlike the MongoDB store, `LastEventId` is **not** updated in
memory on a failed write — the value held in memory reflects only what was successfully persisted.

---

## Packages

| Package | Purpose |
|---------|---------|
| `SsePulse.Client.Extensions.Stores.DistributedCache` | `DistributedCacheLastEventIdStore` class and options |
| `SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection` | `AddDistributedCacheLastEventIdStore` builder extensions |

## Options

Configured via `DistributedCacheLastEventIdStoreOptions`:

| Property | Default | Description |
|----------|---------|-------------|
| `Key` | `ssepulse.client.lastEventId` | The cache key under which the last event ID is stored. Use a unique key per SSE source when multiple sources share the same distributed-cache instance. |
| `AbsoluteExpirationRelativeToNow` | `null` | Optional TTL for the cached entry. When `null` the entry has no expiration and persists until explicitly evicted by the cache backend. Useful when stale last-event-ID values should expire automatically, for example after a deployment that resets the event stream. |

## Registration

Install `SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection` and call one
of the `AddDistributedCacheLastEventIdStore` overloads on the `ISseSourceBuilder`. An
`IDistributedCache` must be registered in the DI container before the source is resolved.

### With an `IDistributedCache` from the container

Use this overload when `IDistributedCache` is already registered (for example via
`AddDistributedMemoryCache()`, `AddStackExchangeRedisCache()`, or any other integration).

```csharp
services.AddDistributedMemoryCache(); // or AddStackExchangeRedisCache(...), etc.

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddDistributedCacheLastEventIdStore(opts =>
    {
        opts.Key = "my-source.lastEventId";
    });
```

### With a custom `IDistributedCache` factory

Use this overload when the cache instance requires custom configuration or must be resolved from
a named service, keyed service, or external factory.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddDistributedCacheLastEventIdStore(
        factory: sp => sp.GetRequiredKeyedService<IDistributedCache>("my-cache"),
        configureOptions: opts =>
        {
            opts.Key = "my-source.lastEventId";
        });
```

## Multiple sources

When multiple SSE sources share the same distributed cache, assign a unique `Key` per source so
each source tracks its own last-event-ID independently.

```csharp
services.AddDistributedMemoryCache();

services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddDistributedCacheLastEventIdStore(opts => opts.Key = "orders.lastEventId");

services
    .AddSseSource("inventory", options => options.Path = "/inventory/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddDistributedCacheLastEventIdStore(opts => opts.Key = "inventory.lastEventId");
```

## Platform availability

Both packages target `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

