# SseSource Implementation & Architecture

This document describes the internal architecture of `SseSource`, focusing on the connection establishment and event
consumption pipeline at a macro level.

## Overview

`SseSource` is a facade that orchestrates three primary responsibilities:

1. **Connection Management** — Establishing and maintaining the HTTP connection to the SSE endpoint
2. **Request Preparation** — Applying request mutators (authentication, last-event-ID headers, etc.)
3. **Event Processing** — Parsing the SSE stream, dispatching events to handlers, and managing the flow

---

## Consumption Loop & Core Components

### Flow Diagrams

#### Diagram 1 — Connection & Reconnection Loop

The outer loop managed by `SseSource.StartConsumeAsync`. Establishes the HTTP connection, delegates stream consumption
to `StreamConsumer`, and decides whether to reconnect or exit based on the outcome when an error occurs during one of
the two main phases.

```mermaid
flowchart TD
    Start([StartConsumeAsync]) --> Establish["SseConnection.EstablishAsync<br/>Apply mutators · Send HTTP GET"]
    Establish --> Check{2xx response?}
    Check -->|Yes| Stream[Open response stream]
    Check -->|No, transient| Establish
    Check -->|No, permanent| Fail([Exit: exception])
    Stream --> Consume[StreamConsumer.ConsumeAsync]
    Consume --> Outcome{Outcome}
    Outcome -->|Normal end| Success([Exit: success])
    Outcome -->|Cancelled| Cancelled([Exit: cancelled])
    Outcome -->|ResponseAbortedException| Restart{RestartOnConnectionAbort?}
    Outcome -->|Other exception| Fail
    Restart -->|true| Establish
    Restart -->|false| Fail
```

#### Diagram 2 — Event Dispatch Pipeline

What happens inside `StreamConsumer` for every received event. The parser feeds events into a bounded queue; a pool of
worker tasks (sized by `MaxDegreeOfParallelism`) takes events from it and routes each one to its registered handlers.
The event ID is stored only after the handlers of that event have completed.

```mermaid
flowchart LR
    Stream[Response Stream] --> Parser[SseParser]
    Parser -->|next SseItem| Queue["Bounded queue<br/>MaxBufferedEvents"]
    Queue --> Worker["Worker tasks<br/>MaxDegreeOfParallelism"]
    Worker --> Found{Handler found?}
    Found -->|Yes| Invoke[Invoke handlers<br/>deserialize if typed]
    Found -->|No, warn-only| Commit[Store EventId<br/>if configured]
    Found -->|No, throw| Fault([HandlerNotFoundException])
    Invoke -->|Success| Commit
    Invoke -->|Exception| OnError[OnError callback]
    OnError --> Commit
    Commit --> Parser
    Parser -->|Stream ends| Done([Done])
    Parser -->|Cancelled| Done
    Parser -->|IO exception| Abort([ResponseAbortedException])
```

### Component Responsibilities

#### 1. **SseSource** (Main Facade)

**File**: `SseSource.cs` + `SseSource.Handlers.cs`

- **State Management**
    - `IsConnected`: Boolean indicating active connection
    - `Completion`: Task that completes when the consumption loop exits
    - `_started`, `_disposed`: Volatile flags preventing reentry and use-after-disposal

- **Handler Registry**
    - Stores event handlers by event name (internal `SseHandlersDictionary`)
    - Supports four registration patterns, each with synchronous and asynchronous (`Func<..., ValueTask>`, optional `CancellationToken`) overloads:
        - Raw strings: `.On("eventName", (string data) => ...)`
        - Strongly-typed: `.On<T>((T data) => ...)` with optional custom event name
        - Full metadata: `.OnItem<T>((SseItem<T> item) => ...)`
        - Reflection-based: `.Bind<TManager>()` (scans public `On*` methods returning `void`, `Task` or `ValueTask`)
    - Every handler is adapted to a single asynchronous invocation path

- **Lifecycle Callbacks**
  All four are settable properties with synchronous delegates; the `Use*` fluent methods register asynchronous ones:
    - `OnConnectionEstablished` / `UseOnConnectionEstablished` — Invoked after the HTTP response is received and the connection is active, before the stream is read
    - `OnConnectionClosed` / `UseOnConnectionClosed` — Invoked when the server ends the stream cleanly (no error)
    - `OnConnectionLost` / `UseOnConnectionLost` — Invoked when the connection drops unexpectedly due to an error
    - `OnError` / `UseOnError` — Invoked when an exception is thrown inside an event handler
      Callbacks are awaited. Exceptions thrown by any of them are logged through the `ILogger` and never stop the consumption loop.

- **Consumption Loop** (`StartConsumeAsync`)
    - Coordinates a retry loop that establishes connections and consumes streams
    - Handles `ResponseAbortedException` based on `RestartOnConnectionAbort` option
    - Propagates other exceptions and cancellations to the `Completion` task

#### 2. **SseConnection** (Connection Management)

**File**: `Internal/SseConnection.cs`

- **Request Preparation** (`EstablishAsync`)
    - Creates an HTTP GET request to the configured endpoint
    - Applies all registered request mutators in order (see [Request Mutators](request-mutators.md))
    - Sets standard SSE headers (`Accept: text/event-stream`)
    - Executes with automatic retry logic for transient failures

- **Retry Strategy**
    - Uses configured `ConnectionRetryOptions` (backoff, max attempts)
    - Delegates transient-failure detection to:
        - Custom predicate: `IsTransientConnectionFailure` option
        - Default logic: socket timeouts/resets, and HTTP error responses whose status code is **not** in
          `NonTransientStatusCodes` (defaults: 404, 401, 403, 500, 502 are never retried)

- **Connection State**
    - `IsConnected` flag (set by `SetConnected()` after successful response)
    - Cleared by `SetDisconnected()` on normal close or error

#### 3. **StreamConsumer** (Event Processing)

**File**: `Internal/StreamConsumer.cs`

- **Stream Parsing** (`ConsumeAsync`)
    - Creates an `SseParser<string>` from the response stream
    - Iterates asynchronously over each incoming `SseItem`
    - Registers each event and stores its `EventId` in the optional `ILastEventIdStore` once its handlers completed

- **Event Dispatcher**
    - Uses a bounded `System.Threading.Channels` queue (`MaxBufferedEvents`) read by `MaxDegreeOfParallelism` worker tasks (default: `1`, sequential)
    - Ensures ordered intake but allows parallel handler invocation
    - Respects the `CancellationToken` passed from `StartConsumeAsync`

- **Handler Dispatch** (`DispatchAsync`)
    - Looks up handlers by event type (`EventType` property of `SseItem`)
    - Awaits all registered handlers for that event type, passing them a token cancelled on stop or fault
    - A handler cancelled by that token is not reported to `OnError` and its event id is not stored
    - Missing handler behavior:
        - Throw `HandlerNotFoundException` if `ThrowWhenNoEventHandlerFound` is `true`
        - Log a warning and skip otherwise
    - Handler exceptions are caught, logged, and passed to the `OnError` callback

- **Error Handling**
    - Detects stream aborts (HTTP IO errors, socket disconnections)
    - Re-throws as `ResponseAbortedException` for the retry loop to handle
    - Validates dispatcher block health before sending events

---

## Request Mutators Pipeline

Request mutators are applied in order before each HTTP request. This allows:

- **Authentication** — `AuthenticationRequestMutator` adds auth headers
- **Last-Event-ID Resumption** — `LastEventIdRequestMutator` adds the `Last-Event-ID` header
- **Custom Mutators** — Any user-supplied `IRequestMutator` implementations

See [Request Mutators](request-mutators.md) for implementation details.

---

## Concurrency Model

### Thread Safety

- **Handler Registry**: The registry is not thread-safe, but handlers should be registered before starting consumption.
  Concurrent registration is not supported.
- **State Flags**: Volatile booleans and `Interlocked` operations prevent race conditions and use-after-dispose.
- **Cancellation**: Linked `CancellationTokenSource` propagates cancellation throughout the pipeline

### Parallelism

- **Handler Invocation**: Parallel via worker tasks reading a bounded channel, with configurable degree
- **Deserialization**: Happens inline during handler dispatch
- **I/O Operations**: All async/await, no blocking calls

### Back-Pressure

The queue between the parser and the handlers is bounded by `MaxBufferedEvents` (default: `1024`). When it is full the
parser stops reading the stream until a handler completes, so a slow handler slows the reader down instead of growing
memory, and the server sees the slowdown through TCP flow control. Events currently being handled are not counted in the
queue, so at most `MaxBufferedEvents` plus `MaxDegreeOfParallelism` events are held at any time.

---

## Configuration & Options

The behavior of `SseSource` is controlled via `SseSourceOptions`:

| Property                       | Default                                                                      | Description                                                                                                                                                                                                                        |
|--------------------------------|------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Path`                         | `/sse`                                                                       | Relative or absolute URL of the SSE endpoint.                                                                                                                                                                                      |
| `MaxDegreeOfParallelism`       | `1`                                                                          | Maximum number of event handlers that run concurrently.                                                                                                                                                                            |
| `DefaultEventNameCasePolicy`   | `PascalCase`                                                                 | Naming policy used when deriving event names from C# type names or `On*` method names.                                                                                                                                             |
| `ConnectionRetryOptions`       | `RetryOptions.None`                                                          | Retry policy for connection failures. Set to `null` or `RetryOptions.None` to disable.                                                                                                                                             |
| `ThrowWhenNoEventHandlerFound` | `false`                                                                      | When `true`, throws `HandlerNotFoundException` for events with no registered handler; otherwise logs a warning and skips.                                                                                                          |
| `RestartOnConnectionAbort`     | `true`                                                                       | Automatically restarts the connection loop after a `ResponseAbortedException`.                                                                                                                                                     |
| `NonTransientStatusCodes`      | `NotFound`, `InternalServerError`, `BadGateway`, `Unauthorized`, `Forbidden` | HTTP status codes treated as permanent failures. When the server responds with one of these codes, no retry is attempted regardless of `ConnectionRetryOptions`.                                                                   |
| `IsTransientConnectionFailure` | `null`                                                                       | Custom predicate that decides whether a connection-phase exception is transient and should trigger a retry. Overrides the built-in transient detection logic when set.                                                             |
| `IsResponseAborted`            | `null`                                                                       | Custom predicate that decides whether a stream-phase exception represents a connection abort. When set and returns `true`, the source treats the exception as a `ResponseAbortedException` and honours `RestartOnConnectionAbort`. |
| `JsonSerializerOptions`        | A default `JsonSerializerOptions` instance that ignores properties name case | Allow to set the options used by the JSON serializer when deserializaing event data.                                                                                                                                               |

See [Configuration](configuration.md) for details.

---

## Lifecycle & Cleanup

### Initialization

```csharp
var source = new SseSource(httpClient, options, requestMutators, lastEventIdStore, logger);
```

### Usage

```csharp
source.On<MyEvent>(e => Console.WriteLine(e));
source.OnError = ex => Console.Error.WriteLine(ex);
source.OnConnectionEstablished = () => Console.WriteLine("Connected");

await source.StartConsumeAsync(cancellationToken);
```

### Disposal

`SseSource` implements both `IDisposable` and `IAsyncDisposable`:

```csharp
await source.DisposeAsync();  // Preferred: cancels the loop and waits for graceful shutdown
```

or

```csharp
source.Dispose();  // Synchronous: cancels the loop but does not wait
```

**Note**: Calling `Dispose` or `DisposeAsync` while `StartConsumeAsync` is running cancels the internal
`CancellationTokenSource`, which causes the consumption loop to detect cancellation and complete `Completion` as
*success* (not as cancelled).

---

## Last-Event-ID & Resumption

If an `ILastEventIdStore` is provided:

1. The `StreamConsumer` stores each event's `EventId` (if present) after the handlers of that event completed; with
   parallel handlers the stored ID never moves past an event that is still being handled. The writes are awaited one
   at a time and an older ID is never written after a newer one
2. On reconnection, the `LastEventIdRequestMutator` reads the stored ID with `GetLastEventIdAsync`; the store loads
   its persisted value lazily at that point
3. The `Last-Event-ID` header is added to the reconnection request
4. The server can use this to resume from the last event

This enables resilient consumption where reconnections do not lose events.

See [Last-Event-ID Resumption](last-event-id.md) for details.

---

## Error Scenarios & Recovery

| Scenario                                    | Behavior                                                                                                |
|---------------------------------------------|---------------------------------------------------------------------------------------------------------|
| **Connection fails (transient)**            | Auto-retry per `ConnectionRetryOptions`                                                                 |
| **Connection fails (permanent, e.g., 404)** | Exception thrown, consumption loop exits                                                                |
| **Stream aborts unexpectedly**              | Re-raise as `ResponseAbortedException`; if `RestartOnConnectionAbort` is true, retry the loop           |
| **Handler throws exception**                | Log error; invoke `OnError` callback; then, per `HandlerFailureBehavior`, store the event id and continue (`SkipAndAdvance`) or fault the source without storing it (`StopSource`) |
| **Store fails**                             | Log error and continue; the in-memory value stays authoritative and the next write persists the newest id |
| **Unknown event type**                      | Log warning and skip; throw `HandlerNotFoundException` only if `ThrowWhenNoEventHandlerFound` is `true` |
| **Cancellation requested**                  | Gracefully shut down, propagate to `Completion` task                                                    |

---

## Dependency Injection Integration

When using `SsePulse.Client.DependencyInjection`:

1. `AddSseSource()` registers a builder that configures options, HTTP client, handlers, and mutators
2. Request mutators and handlers are composed at construction time
3. The factory creates `SseSource` instances with all configurations pre-applied
4. Named sources are supported via `ISseSourceFactory`

See [Dependency Injection](dependency-injection.md) for details.

---

## Summary

The `SseSource` architecture is built around a simple, layered model:

- **SseSource** (facade) — Coordinates the lifecycle and state
- **SseConnection** (connection layer) — Establishes HTTP connections with retry
- **StreamConsumer** (parsing layer) — Parses events and dispatches to handlers in parallel
- **Handler Registry** (dispatch layer) — Routes events to registered handlers with optional deserialization

This design allows for:

- ✅ Clean separation of concerns
- ✅ Testability via mocks and in-memory streams
- ✅ Extensibility via request mutators and custom handlers
- ✅ Robust error handling and recovery
- ✅ Efficient parallel event processing
- ✅ Optional persistence of event IDs for resumption

