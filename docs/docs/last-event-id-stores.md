# Last-Event-ID Stores

SsePulse ships with several `ILastEventIdStore` implementations. The built-in ones
(`InMemoryLastEventIdStore` and `FileLastEventIdStore`) are covered in the
[Last-Event-ID Resumption](last-event-id.md) page. This page lists the additional stores
distributed as separate NuGet packages, each documented on its own page.

---

## MongoDB

[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.Mongo?label=SsePulse.Client.Extensions.Stores.Mongo)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.Mongo)
[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection?label=SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection)

Persists the last event ID to a MongoDB collection using an upsert strategy. If MongoDB is
unavailable the error is logged and the in-memory value is still updated so SSE processing
continues uninterrupted.

→ [MongoDB Store documentation](store-mongo.md)

---

## Distributed Cache

[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.DistributedCache?label=SsePulse.Client.Extensions.Stores.DistributedCache)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.DistributedCache)
[![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection?label=SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection)](https://www.nuget.org/packages/SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection)

Persists the last event ID to any `IDistributedCache` backend (Redis, SQL Server, in-memory,
etc.). If the cache write fails the error is logged and `LastEventId` is not updated in memory —
SSE processing continues uninterrupted.

→ [Distributed Cache Store documentation](store-distributed-cache.md)
