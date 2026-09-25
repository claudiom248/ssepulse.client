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
