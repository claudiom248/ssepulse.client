# Migrating to 2.0

This page collects the changes of 2.0 that need an action from your code.

---

## Asynchronous handlers

### `async` lambdas are now awaited

Before 2.0 a handler was an `Action<T>`. An `async` lambda passed to `On`, `On<T>`, `OnItem` or `OnItem<T>` compiled as `async void`: the source never awaited it, so events were not ordered and exceptions escaped `OnError`. In 2.0 the same lambda binds to the new `Func<T, ValueTask>` overload and is awaited.

What changes at run time:

- with `MaxDegreeOfParallelism = 1`, a handler now finishes before the next event is dispatched;
- a slow handler applies back-pressure through `MaxBufferedEvents`;
- an exception is reported to `OnError`;
- the last event id is stored after the handler completes;
- a handler that ignores cancellation delays `Stop()` until it returns.

Nothing needs to change in the code. Review handlers that relied on the old behavior, for example ones that blocked on a gate that only opens after `Stop`.

### Handlers that return a `Task`

A lambda that returns a `Task` still binds to the `Action<T>` overload, and the task is discarded. Await it:

```csharp
source.On<OrderCreated>(order => repository.SaveAsync(order));
```

becomes

```csharp
source.On<OrderCreated>(async order => await repository.SaveAsync(order));
```

### Lifecycle callbacks

`OnConnectionEstablished`, `OnConnectionClosed`, `OnConnectionLost` and `OnError` remain settable properties of type `Action`. Asynchronous versions are registered with `UseOnConnectionEstablished`, `UseOnConnectionClosed`, `UseOnConnectionLost` and `UseOnError`.

Callbacks are now awaited, and an exception they throw is logged through the `ILogger`. Before it was swallowed without a trace.

### `Bind`

`Bind` accepts methods that return `Task` or `ValueTask` and take an optional `CancellationToken`. It now throws an `InvalidOperationException` for an `async void` method, and the message for a wrong parameter list changed. See [Asynchronous Handlers](async-handlers.md).

---

## Last-Event-ID stores

### `ILastEventIdStore` is asynchronous

The synchronous members are replaced:

| 1.x                                  | 2.0                                                             |
|--------------------------------------|-----------------------------------------------------------------|
| `string? LastEventId { get; }`       | `ValueTask<string?> GetLastEventIdAsync(CancellationToken)`     |
| `void Set(string eventId)`           | `ValueTask SetLastEventIdAsync(string eventId, CancellationToken)` |

A custom store must be rewritten. There is no synchronous adapter. See [Last-Event-ID Resumption](last-event-id.md#custom-store) for the rules an implementation follows.

Code that read `store.LastEventId` or called `store.Set(...)` now awaits the new methods. The `InMemoryLastEventIdStore` no longer has a public `LastEventId` property.

### No I/O in the constructors

`FileLastEventIdStore`, `MongoLastEventIdStore` and `DistributedCacheLastEventIdStore` do not read their backend when they are created. The persisted value is loaded by the first `GetLastEventIdAsync`, which happens when the source connects. A failed read is logged and retried on the next connection.

`FileLastEventIdStore` also implements `IAsyncDisposable`.

### Uniform failure semantics

If the backend fails, every store keeps the value in memory, logs the error and persists the newest value again on the next write. `DistributedCacheLastEventIdStore` used to discard the in-memory update when a write failed.

### The ID is stored after the handler

The ID of an event is stored after its handlers complete, so a crash in the middle of a handler delivers the event again. A handler that throws no longer prevents the ID from being stored by default (`HandlerFailureBehavior.SkipAndAdvance`); set `SseSourceOptions.HandlerFailureBehavior` to `StopSource` to fault the source and get the event again after a restart.