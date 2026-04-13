# SsePulse.Client

[![CI](https://github.com/claudiom248/SsePulse.Client/actions/workflows/ci.yml/badge.svg)](https://github.com/claudiom248/SsePulse.Client/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-informational)](https://claudiom248.github.io/SsePulse.Client/)
[![GitHub Packages](https://img.shields.io/badge/GitHub%20Packages-available-blue?logo=github)](https://github.com/claudiom248/SsePulse.Client/packages)

A .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library for consuming real-time event streams with minimal boilerplate. It offers a fluent handler-registration API, strongly-typed JSON deserialization, pluggable authentication, configurable retry and reconnect logic, and an extensible request-mutator pipeline — everything you need to integrate SSE into any .NET application, from lightweight console tools to full ASP.NET Core services backed by `Microsoft.Extensions.DependencyInjection`.

---

## Table of Contents

- [Why SsePulse instead of the raw `SseParser`?](#why-ssepulse-instead-of-the-raw-sseparser)
- [Packages](#packages)
- [Quick start](#quick-start)
- [Handler registration](#handler-registration)
- [Dependency injection](#dependency-injection)
- [Authentication](#authentication)
- [Request mutators](#request-mutators)
- [Configuration](#configuration)
- [Supported platforms](#supported-platforms)
- [Documentation](#documentation)
- [License](#license)

---

## Why SsePulse instead of the raw `SseParser`?

`System.Net.ServerSentEvents.SseParser` is a low-level primitive: given a `Stream`, it parses SSE events into `SseItem<T>` values — and that is all it does. To build a production-ready client on top of it, a significant amount of boilerplate is required to handle connection management, event routing, deserialization, authentication, and more. SsePulse abstracts all of that away behind a single `SseSource` API that you can compose and extend as needed.:

| Concern                                                |                    Raw `SseParser`                    |                 SsePulse                 |
|:-------------------------------------------------------|:-----------------------------------------------------:|:----------------------------------------:|
| HTTP connection setup                                  |                       ✍️ manual                       |                ✅ built-in                |
| Event routing by type to separate handlers             |        ✍️ manual `if`/`switch` per event type         |     ✅ fluent `.On<T>()` registration     |
| JSON deserialization per event type                    | ✍️ manual `JsonSerializer.Deserialize` per event type |        ✅ automatic via `.On<T>()`        |
| Concurrent / parallel handler dispatch                 |                       ✍️ manual                       |       ✅ TPL Dataflow `ActionBlock`       |
| Retry on connection failure                            |                 ✍️ manual retry loop                  |        ✅ `ConnectionRetryOptions`        |
| Automatic reconnect on stream abort                    |                       ✍️ manual                       |       ✅ `RestartOnConnectionAbort`       |
| Connection lifetime event handlers                     |                       ✍️ manual                       |  ✅ built-in (ex: `OnConnectionClosed` )  |
| `Last-Event-ID` replay on reconnect                    |              ✍️ manual header management              |           ✅ `AddLastEventId()`           |
| Authentication (token refresh, API key, Basic)         |        ✍️ manual header injection per request         | ✅ `ISseAuthenticationProvider` pipeline  |
| `Microsoft.Extensions.DependencyInjection` integration |       ✍️ manual factory and lifetime management       | ✅ `AddSseSource()` + `ISseSourceFactory` |

SsePulse handles all of the above through a single, composable API surface, so you can focus on writing event handlers rather than the infrastructure surrounding them.

---

## Packages

SsePulse is split into focused packages so you only take what you need.

| Package | Version | Downloads | GitHub Packages | Description |
|:---|:---:|:---:|:---:|:---|
| [`SsePulse.Client`](https://www.nuget.org/packages/SsePulse.Client) | [![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.svg)](https://www.nuget.org/packages/SsePulse.Client) | [![Downloads](https://img.shields.io/nuget/dt/SsePulse.Client.svg)](https://www.nuget.org/packages/SsePulse.Client) | [![GitHub](https://img.shields.io/badge/github-packages-blue?logo=github)](https://github.com/claudiom248/SsePulse.Client/pkgs/nuget/SsePulse.Client) | Core library — `SseSource`, handler registration, retry, and the request-mutator pipeline. Required by every app. |
| [`SsePulse.Client.DependencyInjection`](https://www.nuget.org/packages/SsePulse.Client.DependencyInjection) | [![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.DependencyInjection.svg)](https://www.nuget.org/packages/SsePulse.Client.DependencyInjection) | [![Downloads](https://img.shields.io/nuget/dt/SsePulse.Client.DependencyInjection.svg)](https://www.nuget.org/packages/SsePulse.Client.DependencyInjection) | [![GitHub](https://img.shields.io/badge/github-packages-blue?logo=github)](https://github.com/claudiom248/SsePulse.Client/pkgs/nuget/SsePulse.Client.DependencyInjection) | `AddSseSource()` extensions on `IServiceCollection`, fluent builder, and `ISseSourceFactory` for named sources. |
| [`SsePulse.Client.Authentication`](https://www.nuget.org/packages/SsePulse.Client.Authentication) | [![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Authentication.svg)](https://www.nuget.org/packages/SsePulse.Client.Authentication) | [![Downloads](https://img.shields.io/nuget/dt/SsePulse.Client.Authentication.svg)](https://www.nuget.org/packages/SsePulse.Client.Authentication) | [![GitHub](https://img.shields.io/badge/github-packages-blue?logo=github)](https://github.com/claudiom248/SsePulse.Client/pkgs/nuget/SsePulse.Client.Authentication) | Bearer token (with OAuth 2.0 client-credentials), Basic, and API-key authentication providers. |
| [`SsePulse.Client.Authentication.DependencyInjection`](https://www.nuget.org/packages/SsePulse.Client.Authentication.DependencyInjection) | [![NuGet](https://img.shields.io/nuget/v/SsePulse.Client.Authentication.DependencyInjection.svg)](https://www.nuget.org/packages/SsePulse.Client.Authentication.DependencyInjection) | [![Downloads](https://img.shields.io/nuget/dt/SsePulse.Client.Authentication.DependencyInjection.svg)](https://www.nuget.org/packages/SsePulse.Client.Authentication.DependencyInjection) | [![GitHub](https://img.shields.io/badge/github-packages-blue?logo=github)](https://github.com/claudiom248/SsePulse.Client/pkgs/nuget/SsePulse.Client.Authentication.DependencyInjection) | `AddAuthentication()` builder extensions that wire auth providers into the DI pipeline. Supports `appsettings.json`-driven configuration. |

```bash
dotnet add package SsePulse.Client
dotnet add package SsePulse.Client.DependencyInjection
dotnet add package SsePulse.Client.Authentication
dotnet add package SsePulse.Client.Authentication.DependencyInjection
```

---

## Quick start

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://my-server.example") };
await using var source = new SseSource(httpClient, new SseSourceOptions { Path = "/events" });

source
    .On<OrderCreated>(e => Console.WriteLine($"Order {e.Id} created"))
    .On<OrderShipped>(e => Console.WriteLine($"Order {e.Id} shipped"))
    .OnError(ex => Console.Error.WriteLine(ex))
    .OnConnectionLost(ex => Console.WriteLine($"Connection lost: {ex?.Message}"));

await source.StartConsumeAsync(CancellationToken.None);
```

`StartConsumeAsync` continues until the stream ends, the token is cancelled, or `StopAsync()` is called.

---

## [Handler Registration](docs/docs/getting-started.md)

Register handlers for named SSE events **before** calling `StartConsumeAsync`. Every registration method returns `SseSource` for fluent chaining.

**Raw string data**

```csharp
source.On("ping", (string data) => Console.WriteLine($"Ping: {data}"));
```

**Strongly-typed JSON deserialization** — the event name is derived from the type name using the configured `DefaultEventNameCasePolicy` (default: `PascalCase`):

```csharp
source.On<OrderCreated>(e => Console.WriteLine($"Order {e.Id} created"));
```

**Full `SseItem<T>` with metadata** (event id, event type, and data together):

```csharp
source.OnItem<OrderCreated>(item =>
    Console.WriteLine($"id={item.EventId}, data={item.Data.Id}"));
```

**Convention-based manager class** — scan all public `On*` methods automatically:

```csharp
public class MyEventsManager : ISseEventsManager
{
    public void OnOrderCreated(OrderCreated e) => Console.WriteLine(e.Id);
    public void OnOrderShipped(OrderShipped e) => Console.WriteLine(e.Id);
}

source.Bind<MyEventsManager>();
// Override the derived event name with [MapEventName(EventName = "custom-name")]
```

See the [Getting Started guide](docs/docs/getting-started.md) for the full handler reference.

---

## [Dependency Injection](docs/docs/dependency-injection.md)

Install `SsePulse.Client.DependencyInjection` and register sources on `IServiceCollection`. Each registration returns an `ISseSourceBuilder` for fluent configuration of the HTTP client, handlers, and optional features.

```csharp
// Single source — inject SseSource directly
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .BindEventsManager<MyEventsManager>()
    .AddLastEventId();   // enables Last-Event-ID replay on reconnect
```

**Multiple named sources** — resolved at runtime via `ISseSourceFactory`:

```csharp
services
    .AddSseSource("orders", options => options.Path = "/orders/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"));

services
    .AddSseSource("notifications", options => options.Path = "/notifications/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://notifications.example"));

// Consume both in a hosted worker
public class StreamWorker(ISseSourceFactory factory)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using SseSource orders = factory.Create("orders");
        await using SseSource notifications = factory.Create("notifications");

        orders.On<OrderCreated>(e => Console.WriteLine(e.Id));
        notifications.On<Notification>(n => Console.WriteLine(n.Message));

        await Task.WhenAll(
            orders.StartConsumeAsync(ct),
            notifications.StartConsumeAsync(ct));
    }
}
```

See the [Dependency Injection guide](docs/docs/dependency-injection.md) for the full reference including advanced `IHttpClientBuilder` setup and custom `ILastEventIdStore` implementations.

---

## [Authentication](docs/docs/authentication.md)

Four authentication providers ship out of the box, all implementing `ISseAuthenticationProvider` and plugging into the request-mutator pipeline.

| Provider | Scheme |
|:---|:---|
| `ApiKeyAuthenticationProvider` | Custom header (`X-API-Key` by default) |
| `BearerTokenAuthenticationProvider` | `Authorization: Bearer <token>` — supports static, environment-variable, OAuth 2.0 client-credentials, and fully custom token providers |
| `BasicAuthenticationProvider` | `Authorization: Basic <base64(user:password)>` |

**Standalone usage** (no DI):

```csharp
var source = new SseSource(httpClient, options, requestMutators:
[
    new AuthenticationRequestMutator(
        new BearerTokenAuthenticationProvider(new StaticTokenProvider("my-jwt-token")))
]);
```

**With DI** — token provider resolved from the container:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddBearerTokenAuthentication(sp => sp.GetRequiredService<IMyTokenService>());
```

**Configuration-driven** — provider type and credentials read from `appsettings.json`:

```json
{
  "SseSource": {
    "Path": "/events",
    "Authentication": {
      "Provider": "Bearer",
      "Args": {
        "TokenProvider": "ClientCredentials",
        "TokenEndpoint": "https://auth.example/token",
        "Credentials": { "ClientId": "id", "ClientSecret": "secret" }
      }
    }
  }
}
```

```csharp
services
    .AddSseSource(configuration.GetSection("SseSource"))
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddAuthentication();   // provider type resolved from the "Authentication" section
```

See the [Authentication guide](docs/docs/authentication.md) for details.

---

## [Request Mutators](docs/docs/request-mutators.md)

Request mutators are the low-level extension point for enriching every outgoing HTTP request before it is sent — custom headers, query parameters, request signing, tenant injection, and more. Implement `IRequestMutator` and register it on the source or the DI builder:

```csharp
public class CorrelationIdMutator : IRequestMutator
{
    public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        message.Headers.TryAddWithoutValidation("X-Correlation-Id", Guid.NewGuid().ToString());
        return ValueTask.CompletedTask;
    }
}

// Standalone
var source = new SseSource(httpClient, options, mutators: [new CorrelationIdMutator()]);

// DI — all three overloads (instance, generic, factory) are supported
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddRequestMutator(new CorrelationIdMutator());
```

Authentication and last-event-id tracking are themselves implemented as built-in mutators that the corresponding builder extensions register automatically. See the [Request Mutators guide](docs/docs/request-mutators.md) for details.

---

## [Configuration](docs/docs/configuration.md)

All runtime behaviour is controlled through `SseSourceOptions`:

| Property | Default | Description |
|:---|:---:|:---|
| `Path` | `/sse` | Relative or absolute URL of the SSE endpoint |
| `MaxDegreeOfParallelism` | `1` | Maximum concurrent event handler invocations |
| `DefaultEventNameCasePolicy` | `PascalCase` | Maps C# type names to SSE event names (`PascalCase`, `CamelCase`, `SnakeCase`, `KebabCase`) |
| `ConnectionRetryOptions` | `RetryOptions.None` | Retry policy for connection failures — set to `null` to disable |
| `ThrowWhenNoEventHandlerFound` | `false` | Throws `HandlerNotFoundException` for unregistered events when `true`; logs a warning and skips when `false` |
| `RestartOnConnectionAbort` | `true` | Automatically restarts the connection loop after a `ResponseAbortedException` |

See the [Configuration guide](docs/docs/configuration.md) for the full reference including naming case policies and retry strategies.

---

## Supported platforms

The library targets four frameworks. The full API surface is available on all of them; the only difference is a minor fallback on `netstandard2.0`.

| Framework | Supported | Notes |
|:---|:---:|:---|
| `net10.0` | ✅ | Full feature set |
| `net9.0` | ✅ | Full feature set |
| `net8.0` | ✅ | Full feature set |
| `netstandard2.0` | ✅ | `StopAsync()` and `DisposeAsync()` both fall back to synchronous cancellation (`CancellationTokenSource.CancelAsync` is unavailable on this target) |

See [Platform Availability](docs/docs/platform-availability.md) for the full support matrix.

---

## Documentation

Full guides and API reference are available in the [`docs/`](docs/) folder:

- [Introduction to Server-Sent Events](docs/docs/introduction-to-sse.md)
- [Getting Started](docs/docs/getting-started.md)
- [Dependency Injection](docs/docs/dependency-injection.md)
- [Authentication](docs/docs/authentication.md)
- [Request Mutators](docs/docs/request-mutators.md)
- [Configuration](docs/docs/configuration.md)
- [Platform Availability](docs/docs/platform-availability.md)

A rendered version with full API reference is hosted on [GitHub Pages](https://claudiom248.github.io/SsePulse.Client/).

---

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).

