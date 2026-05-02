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
| `NonTransientStatusCodes`      | `ICollection<HttpStatusCode>` | `NotFound`, `InternalServerError`, `BadGateway`, `Unauthorized`, `Forbidden` | HTTP status codes treated as permanent failures. When the server responds with one of these codes, no retry is attempted regardless of `ConnectionRetryOptions`. |
| `IsTransientConnectionFailure` | `Predicate<Exception>?` | `null`                                                                       | Custom predicate that decides whether a connection-phase exception is transient and should trigger a retry. Overrides the built-in transient detection logic when set. |
| `IsResponseAborted`            | `Predicate<Exception>?` | `null`                                                                       | Custom predicate that decides whether a stream-phase exception represents a connection abort. When set and returns `true`, the source treats the exception as a `ResponseAbortedException` and honours `RestartOnConnectionAbort`. |
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

## Transient exception detection

SsePulse distinguishes between two kinds of failures:

- **Connection-phase failures** – exceptions raised while establishing the HTTP connection (e.g. DNS errors, TCP timeouts, non-2xx responses).
- **Stream-phase aborts** – exceptions raised while reading an already-open response stream (e.g. the server closes the socket mid-stream).

Both kinds can be customised through `SseSourceOptions`.

### NonTransientStatusCodes

When the server returns an HTTP error response, SsePulse inspects the status code to decide whether to retry.
Status codes listed in `NonTransientStatusCodes` are treated as permanent failures – no retry is attempted,
regardless of the `ConnectionRetryOptions` setting.

The default set of non-transient codes is: `404 Not Found`, `500 Internal Server Error`, `502 Bad Gateway`, `401 Unauthorized`, and `403 Forbidden`.

```csharp
var options = new SseSourceOptions
{
    Path = "/events",
    ConnectionRetryOptions = new RetryOptions { MaxRetries = 5, Delay = TimeSpan.FromSeconds(2) },
    // Treat GatewayTimeout and RequestTimeout as permanent failures too
    NonTransientStatusCodes = [
        HttpStatusCode.NotFound,
        HttpStatusCode.Unauthorized,
        HttpStatusCode.Forbidden,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.RequestTimeout
    ]
};
```

### IsTransientConnectionFailure

By default, SsePulse applies built-in heuristics to decide whether a connection-phase exception warrants a retry
(e.g. `SocketError.TimedOut`, `SocketError.ConnectionReset`, or an inner `TimeoutException`).

Supply `IsTransientConnectionFailure` to override this logic entirely with your own predicate:

```csharp
var options = new SseSourceOptions
{
    Path = "/events",
    ConnectionRetryOptions = new RetryOptions { MaxRetries = 3, Delay = TimeSpan.FromSeconds(1) },
    // Retry on any HttpRequestException whose message contains "transient"
    IsTransientConnectionFailure = ex => ex is HttpRequestException hre && hre.Message.Contains("transient")
};
```

When the predicate returns `true` the exception is considered transient and a retry is scheduled according to
`ConnectionRetryOptions`. When it returns `false` the exception propagates immediately and the source stops.

> **Note:** `IsTransientConnectionFailure` is only evaluated when `ConnectionRetryOptions` is not `null` or
> `RetryOptions.None`. Setting it without retry options has no effect.

### IsResponseAborted

Stream-phase aborts are exceptions that occur while reading from an already-established response stream,
typically because the server closed the connection unexpectedly. By default, SsePulse recognises
`HttpIOException` with `HttpRequestError.ResponseEnded` (on .NET 8+) and `IOException` wrapping a
`SocketError.ConnectionReset` as aborts.

Supply `IsResponseAborted` to extend or replace this detection with your own predicate:

```csharp
var options = new SseSourceOptions
{
    Path = "/events",
    RestartOnConnectionAbort = true,
    // Treat any exception whose message starts with "stream closed" as an abort
    IsResponseAborted = ex => ex.Message.StartsWith("stream closed", StringComparison.OrdinalIgnoreCase)
};
```

When the predicate returns `true`, the exception is wrapped in a `ResponseAbortedException`.
If `RestartOnConnectionAbort` is `true`, the connection loop restarts automatically;
otherwise `ResponseAbortedException` propagates to the caller.

---

## Authentication

See the [Authentication](authentication.md) page for the full reference.

---

## Last-event-id resumption

When the DI builder is configured with a `ILastEventIdStore`, the last received `EventId` value
is persisted and automatically replayed on reconnect via the `Last-Event-ID` header.

The default in-memory store is registered automatically; provide your own implementation for
distributed or persistent scenarios.