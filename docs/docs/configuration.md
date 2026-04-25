# Configuration

Basic behaviour of `SseSource` is controlled through `SseSourceOptions`.

## SseSourceOptions reference

| Property                       | Type                    | Default                                                                      | Description                                                                                                               |
|--------------------------------|-------------------------|------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| `Path`                         | `string`                | `/sse`                                                                       | Relative or absolute URL of the SSE endpoint.                                                                             |
| `MaxDegreeOfParallelism`       | `int`                   | `1`                                                                          | Maximum number of event handlers that run concurrently.                                                                   |
| `DefaultEventNameCasePolicy`   | `NameCasePolicy`        | `PascalCase`                                                                 | Naming policy used when deriving event names from C# type names or `On*` method names.                                    |
| `ConnectionRetryOptions`       | `RetryOptions?`         | `RetryOptions.None`                                                          | Retry policy for connection failures. Set to `null` or `RetryOptions.None` to disable.                                    |
| `ThrowWhenNoEventHandlerFound` | `bool`                  | `false`                                                                      | When `true`, throws `HandlerNotFoundException` for events with no registered handler; otherwise logs a warning and skips. |
| `RestartOnConnectionAbort`     | `bool`                  | `true`                                                                       | Automatically restarts the connection loop after a `ResponseAbortedException`.                                            |
| `JsonSerializerOptions`        | `JsonSerializerOptions` | A default `JsonSerializerOptions` instance that ignores properties name case | Allow to set the options used by the JSON serializer when deserializaing event data.                                      |

---

## Naming case policies

The `DefaultEventNameCasePolicy` property controls how C# identifiers are converted to SSE event names.

| Policy | Example input | Example output |
|---|---|---|
| `PascalCase` | `OrderCreated` | `OrderCreated` |
| `CamelCase` | `OrderCreated` | `orderCreated` |
| `SnakeCase` | `OrderCreated` | `order_created` |
| `KebabCase` | `OrderCreated` | `order-created` |

---

## JsonSerializerOptions

```csharp
var options = new SseSourceOptions
{
    Path = "/events",
    JsonSerializerOptions = new JsonSerializerOptions
    {
        IgnoreNullValues = true,
        PropertyNameCaseInsensitive = false
    }
};
```

See the [JsonSerializerOptions](json-serializer-options.md) page for the full reference.

---

## Retry options

```csharp
var options = new SseSourceOptions
{
    Path = "/events",
    ConnectionRetryOptions = new RetryOptions
    {
        MaxRetries = 5,
        Delay = TimeSpan.FromSeconds(2),
        Strategy = RetryStrategy.Fixed
    }
};
```

Set `ConnectionRetryOptions` to `null` or `RetryOptions.None` to disable retries entirely.

---

## Authentication

See the [Authentication](authentication.md) page for the full reference.

---

## Last-event-id resumption

When the DI builder is configured with a `ILastEventIdStore`, the last received `EventId` value
is persisted and automatically replayed on reconnect via the `Last-Event-ID` header.

The default in-memory store is registered automatically; provide your own implementation for
distributed or persistent scenarios.