---
_layout: landing
---

# SsePulse.Client

**SsePulse.Client** is a .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library for consuming real-time event streams with minimal boilerplate.It offers a fluent handler-registration API, strongly-typed JSON deserialization, pluggable authentication, configurable retry and reconnect logic, and an extensible request-mutator pipeline — everything you need to integrate SSE into any .NET application, from lightweight console tools to full ASP.NET Core services backed by `Microsoft.Extensions.DependencyInjection`.

## Highlights

- **Zero boilerplate** — declare what events you care about and what to do with them. SsePulse handles the rest.
- **Your domain, your types** — incoming event data is automatically deserialized into your own C# classes. No manual parsing, no raw strings.
- **Resilient by default** — built-in retry policies, automatic reconnection on stream abort, and seamless last-event-replay.
- **Pluggable authentication** — API key, Bearer token, Basic Auth, or a fully custom provider — wired in with small frictions.
- **Scales with your app** — works standalone with a plain `HttpClient`, integrates cleanly with `Microsoft.Extensions.DependencyInjection`, and supports multiple named sources side by side.
- **Broad framework support** — targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

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
- [Hosted Services](docs/hosted-services.md)
- [Authentication](docs/authentication.md)
- [Configuration](docs/configuration.md)
- [Platform Availability](docs/platform-availability.md)
- [API Reference](xref:SsePulse.Client.Core.SseSource)

