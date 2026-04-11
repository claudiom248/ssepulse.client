---
_layout: landing
---

# SsePulse

**SsePulse** is a .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library with a fluent API, pluggable authentication, and automatic JSON deserialization.

## Highlights

- **Fluent handler registration** — chain `.On()`, `.OnItem()`, and `.Bind()` calls before starting the stream.
- **Strongly-typed events** — deserialize SSE data directly into your own types via `.On<T>()`.
- **Pluggable authentication** — API key, Bearer token (with refresh), Basic Auth, or roll your own `ISseAuthenticationProvider`.
- **Last-event-id resumption** — automatically replays the last received `id` on reconnect.
- **Automatic reconnection** — configurable retry policy and restart-on-abort support.
- **Multi-framework** — targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

## Quick start

```csharp
var client = new HttpClient { BaseAddress = new Uri("https://my-sse-server.example") };
var options = new SseSourceOptions { Path = "/events" };

await using var source = new SseSource(client, options);

source
    .On<OrderCreated>((OrderCreated e) => Console.WriteLine($"Order {e.Id} created"))
    .On<OrderShipped>((OrderShipped e) => Console.WriteLine($"Order {e.Id} shipped"))
    .OnError(ex => Console.Error.WriteLine(ex));

await source.StartConsumeAsync(CancellationToken.None);
```

## Navigation

- [Introduction to Server-Sent Events](docs/introduction-to-sse.md)
- [Getting Started](docs/getting-started.md)
- [Dependency Injection](docs/dependency-injection.md)
- [Authentication](docs/authentication.md)
- [Configuration](docs/configuration.md)
- [Platform Availability](docs/platform-availability.md)
- [API Reference](xref:SsePulse.Client.Core.SseSource)

