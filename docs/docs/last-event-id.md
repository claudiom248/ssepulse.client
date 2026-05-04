# Last-Event-ID Resumption

When an SSE connection is interrupted and automatically reconnected, the client should tell the
server where to resume by sending the `Last-Event-ID` header. SsePulse handles this automatically
once you register a last-event-ID store on the builder.

---

## How it works

Every time an SSE event that carries an `id:` field is received, SsePulse stores the value in the
configured `ILastEventIdStore`. On the next connection attempt, `LastEventIdRequestMutator` reads
the stored value and attaches it as the `Last-Event-ID` request header.

---

## Implementations

### `InMemoryLastEventIdStore`

Stores the last event ID in memory only. The ID is lost when the process restarts — suitable for
short-lived applications or when the server does not support resumption across restarts.

**Register on the builder:**

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddLastEventId();
```

---

### `FileLastEventIdStore`

Persists the last event ID to a local file so the stream can be resumed even after a process
restart. The file is read back automatically during construction.

Writes to disk are controlled by a configurable **flush strategy**:

| `FlushMode`            | When the file is updated     |
|------------------------|------------------------------|
| `EverySet` *(default)* | On every received event ID   |
| `AfterCount`           | Every *N* received event IDs |
| `AfterInterval`        | On a repeating timer         |

> [!IMPORTANT]
> Dispose the `SseSource` to guarantee that any pending
> `AfteCount` and `AfterInterval` flush is written before the process exits.

**Register on the builder:**

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddPersistentLastEventIdStore(opts =>
    {
        opts.FilePath = "/var/app/last-event-id.txt";
        opts.FlushMode = FlushMode.EverySet; // default — write on every event
    });
```

**With `AfterCount`:**

```csharp
.AddPersistentLastEventIdStore(opts =>
{
    opts.FilePath = "/var/app/last-event-id.txt";
    opts.FlushMode = FlushMode.AfterCount;
    opts.FlushAfterCount = 20; // flush every 20 events
});
```

**With `AfterInterval`:**

```csharp
.AddPersistentLastEventIdStore(opts =>
{
    opts.FilePath = "/var/app/last-event-id.txt";
    opts.FlushMode = FlushMode.AfterInterval;
    opts.FlushInterval = TimeSpan.FromSeconds(5); // flush every 5 seconds
});
```

---

## Custom store

Implement `ILastEventIdStore` and register it on the builder for any other persistence
strategy (Redis, database, distributed cache, etc.):

```csharp
public class RedisLastEventIdStore : ILastEventIdStore
{
    public string? LastEventId { get; private set; }

    public void Set(string eventId) => LastEventId = eventId;
    // ... persist to Redis
}
```

```csharp
services.AddSingleton<RedisLastEventIdStore>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddLastEventId<RedisLastEventIdStore>();
```

