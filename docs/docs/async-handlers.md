# Asynchronous Handlers

Handlers usually call a database or an HTTP API. SsePulse awaits them, so you never have to block a thread or fire and forget a task.

---

## Registering asynchronous handlers

`On`, `On<T>`, `OnItem` and `OnItem<T>` accept `Func<..., ValueTask>` delegates, with or without a `CancellationToken`.

```csharp
source.On<OrderCreated>(async order =>
{
    await repository.SaveAsync(order);
});

source.On<OrderShipped>(async (order, cancellationToken) =>
{
    await notifier.SendAsync(order, cancellationToken);
});

source.OnItem<OrderCreated>(async (item, cancellationToken) =>
{
    await audit.WriteAsync(item.EventId, item.Data, cancellationToken);
});
```

The synchronous `Action` overloads are unchanged.

> [!WARNING]
> The asynchronous overloads take `ValueTask`. A lambda that returns a `Task`, such as `order => repository.SaveAsync(order)`, does not convert to them and binds to the synchronous `Action<T>` overload: the task is discarded and its exceptions are lost. Write `async order => await repository.SaveAsync(order)` or return `new ValueTask(task)`.

---

## Ordering and parallelism

The dispatcher awaits every handler of an event before it releases the worker to the next one.

| `MaxDegreeOfParallelism` | Behavior |
|--------------------------|----------|
| `1` (default)            | Events are handled one at a time, in the order they arrive. |
| greater than `1`         | Handlers of different events run concurrently. There is **no** ordering guarantee. |

A slow handler applies back-pressure: once `MaxBufferedEvents` events are queued, the source stops reading from the stream until a worker is free.

---

## Cancellation

The token passed to a handler is cancelled when:

- `Stop()` or `StopAsync()` is called, or the source is disposed;
- the token passed to `StartConsumeAsync` is cancelled;
- event processing faults, for example with `HandlerNotFoundException`.

A handler that observes the token and throws `OperationCanceledException` is not reported to `OnError`, and its event id is **not** stored, so the event is delivered again after a restart. A handler that ignores the token delays the stop until it completes.

---

## Errors

An exception thrown by a handler, synchronous or asynchronous, is logged, passed to `OnError` and does not stop the loop. The event id is stored anyway: a failed event is not redelivered. Handle transient failures inside the handler.

---

## Lifecycle callbacks

The `OnConnectionEstablished`, `OnConnectionClosed`, `OnConnectionLost` and `OnError` properties accept synchronous delegates. To register an asynchronous callback, use the fluent `Use*` methods, which replace the previous callback:

```csharp
source
    .UseOnConnectionEstablished(async () => await health.MarkUpAsync())
    .UseOnConnectionLost(async ex => await health.MarkDownAsync(ex))
    .UseOnError(async ex => await deadLetters.WriteAsync(ex));
```

- Callbacks are awaited. `OnConnectionEstablished` completes before the first event is read.
- An exception thrown by a callback is logged through the `ILogger` and never stops the loop.
- After a `Use*` call the matching property still returns the last synchronous delegate assigned to it.

---

## Events manager methods

Methods bound with `Bind` can return `void`, `Task` or `ValueTask` and take the event data followed by an optional `CancellationToken`.

```csharp
public class OrdersManager : ISseEventsManager
{
    public async Task OnOrderCreated(OrderCreated order, CancellationToken cancellationToken)
    {
        await repository.SaveAsync(order, cancellationToken);
    }

    public ValueTask OnOrderShipped(OrderShipped order) => notifier.SendAsync(order);

    public void OnPing(string data) => Console.WriteLine(data);
}

source.Bind<OrdersManager>();
```

`Bind` rejects, with an `InvalidOperationException`:

- `async void` methods, whose completion and exceptions cannot be observed;
- `void` methods that take a `CancellationToken`;
- methods that return anything else;
- methods with a parameter list other than the event data and an optional token.
