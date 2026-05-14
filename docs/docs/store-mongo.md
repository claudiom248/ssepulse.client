# MongoDB Last-Event-ID Store

[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.Mongo?label=SsePulse.Client.Extensions.Stores.Mongo)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.Mongo)
[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection?label=SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection)

Persists the last event ID to a MongoDB collection so the SSE connection can be resumed after a
process restart or a redeployment. The store uses an upsert strategy: exactly one document per
[`DocumentKey`](#options) is kept in the collection.

If MongoDB is unavailable at the time of a `Set` call, the error is logged at `Error` level but
is never surfaced to the caller — SSE event processing continues uninterrupted and the
last-event-ID is still held in memory.

---

## Packages

| Package | Purpose |
|---------|---------|
| `SsePulse.Client.Extensions.Stores.Mongo` | `MongoLastEventIdStore` class and options |
| `SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection` | `AddMongoLastEventIdStore` builder extensions |

## Options

Configured via `MongoLastEventIdStoreOptions`:

| Property | Default | Description |
|----------|---------|-------------|
| `DatabaseName` | *(required)* | Name of the MongoDB database. |
| `CollectionName` | `sse_last_event_ids` | Name of the collection used to store documents. |
| `DocumentKey` | `DefaultSseSource` | Value of the `_id` field that identifies the document for this SSE source. Use a unique key when multiple SSE sources share the same collection. |

## Registration

Install `SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection` and call one of the
`AddMongoLastEventIdStore` overloads on the `ISseSourceBuilder`.

### With a connection string

The store creates and owns an `IMongoClient` built from the supplied connection string. Clients
with the same connection string are shared across registrations and disposed when the
`IServiceProvider` is disposed.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddMongoLastEventIdStore(
        connectionString: "mongodb://localhost:27017",
        configureOptions: opts =>
        {
            opts.DatabaseName = "my_app";
            opts.CollectionName = "sse_last_event_ids";
            opts.DocumentKey = "my-source";
        });
```

### With an `IMongoClient` from the container

Use this overload when an `IMongoClient` is already registered in the DI container (for example,
by the official [MongoDB .NET Driver DI integration](https://www.mongodb.com/docs/drivers/csharp/)).

```csharp
services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddMongoLastEventIdStore(opts =>
    {
        opts.DatabaseName = "my_app";
        opts.DocumentKey = "my-source";
    });
```

### With a custom `IMongoClient` factory

Use this overload when the client requires custom configuration, must be resolved from a named
service, or comes from an external factory.

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddMongoLastEventIdStore(
        mongoClientFactory: sp => sp.GetRequiredKeyedService<IMongoClient>("my-cluster"),
        configureOptions: opts =>
        {
            opts.DatabaseName = "my_app";
            opts.DocumentKey = "my-source";
        });
```

## Multiple sources

When multiple SSE sources share the same collection, assign a unique `DocumentKey` per source so
each source tracks its own last-event-ID independently.

```csharp
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddMongoLastEventIdStore(
        connectionString: "mongodb://localhost:27017",
        configureOptions: opts =>
        {
            opts.DatabaseName = "my_app";
            opts.DocumentKey = "orders-source";
        });

services
    .AddSseSource("inventory", options => options.Path = "/inventory/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddMongoLastEventIdStore(
        connectionString: "mongodb://localhost:27017",
        configureOptions: opts =>
        {
            opts.DatabaseName = "my_app";
            opts.DocumentKey = "inventory-source";
        });
```

## Document schema

The store keeps exactly one document per `DocumentKey` in the configured collection. The schema
is:

```json
{
  "_id": "my-source",
  "lastEventId": "42",
  "updatedAt": "2025-01-01T12:00:00Z"
}
```

| Field | Description |
|-------|-------------|
| `_id` | Matches `DocumentKey`. Used as the lookup key for every read and upsert. |
| `lastEventId` | The most recently received SSE event ID for this source. |
| `updatedAt` | UTC timestamp of the last update. |

## Platform availability

Both packages target `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.  
`MongoDB.Driver` 3.x is used on `net8.0` and above; 2.x (`2.30.0`) is used on `netstandard2.0`.

