# Last-Event-ID Resumption

When an SSE connection is interrupted and automatically reconnected, the client should tell the
server where to resume by sending the `Last-Event-ID` header. SsePulse handles this automatically
once you register a last-event-ID store on the builder.

---

## How it works

Every time an SSE event that carries an `id:` field has been handled, SsePulse stores the value in the
configured `ILastEventIdStore`. On the next connection attempt, `LastEventIdRequestMutator` reads
the stored value with `GetLastEventIdAsync` and attaches it as the `Last-Event-ID` request header.

---

## Delivery guarantee

The ID of an event is stored only **after every handler of that event completed**, so delivery is
at-least-once: if the process crashes in the middle of a handler, the event is delivered again after the
resume. Make handlers idempotent.

- With `MaxDegreeOfParallelism` greater than `1`, the stored ID never moves past an event that is still
  being handled.
- A handler cancelled because the source is stopping does not store its ID.
- The IDs are written one at a time and in order, and an older ID is never written after a newer one.

### When a handler throws

`SseSourceOptions.HandlerFailureBehavior` decides what happens to the ID of an event whose handler threw.
In both cases the exception is logged and passed to `OnError`.

| `HandlerFailureBehavior`        | Behavior |
|---------------------------------|----------|
| `SkipAndAdvance` *(default)*    | The ID of the failed event is stored and the source keeps going. The event is not delivered again. |
| `StopSource`                    | The source stops and `Completion` faults with the handler's exception. The ID of the failed event is **not** stored, so the event is delivered again when the source is restarted. |

### When the store fails

A store keeps the value it received in memory, which is authoritative for the running process. If the
persistence backend fails, the error is logged and never thrown, and the next `SetLastEventIdAsync` persists
the newest value again. A failed read is logged and retried on the next connection. The built-in stores behave
in the same way.

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
restart. The file is read lazily, by the first connection attempt, and not during construction.

Writes to disk are controlled by a configurable **flush strategy**:

| `FlushMode`            | When the file is updated     |
|------------------------|------------------------------|
| `EverySet` *(default)* | On every received event ID   |
| `AfterCount`           | Every *N* received event IDs |
| `AfterInterval`        | On a repeating timer         |

> [!IMPORTANT]
> Dispose the store (`Dispose` or `DisposeAsync`) to guarantee that any pending
> `AfterCount` and `AfterInterval` flush is written before the process exits.

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
    private readonly IDatabase _database;
    private volatile string? _lastEventId;
    private int _loaded;

    public RedisLastEventIdStore(IDatabase database) => _database = database;

    public async ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _loaded) == 0)
        {
            _lastEventId ??= await _database.StringGetAsync("last-event-id");
            Volatile.Write(ref _loaded, 1);
        }

        return _lastEventId;
    }

    public async ValueTask SetLastEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        _lastEventId = eventId;
        await _database.StringSetAsync("last-event-id", eventId);
    }
}
```

An implementation should follow these rules, which the built-in stores respect:

- **No I/O in the constructor.** Load the persisted value lazily in `GetLastEventIdAsync`.
- **Ignore empty and whitespace IDs.**
- **The value in memory is authoritative.** Log a persistence failure instead of throwing it, and persist the newest value again on the next call.
- **Be thread safe.** With parallel handlers the source may call the store from several threads, although it serializes the writes.
- Implement `IAsyncDisposable` when the store owns resources such as a timer or a connection.

```csharp
services.AddSingleton<RedisLastEventIdStore>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddLastEventId<RedisLastEventIdStore>();
```

