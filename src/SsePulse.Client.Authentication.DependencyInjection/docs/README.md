# SsePulse.Client

**SsePulse.Client** is a .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library for consuming real-time event streams with minimal boilerplate.It offers a fluent handler-registration API, strongly-typed JSON deserialization, pluggable authentication, configurable retry and reconnect logic, and an extensible request-mutator pipeline — everything you need to integrate SSE into any .NET application, from lightweight console tools to full ASP.NET Core services backed by `Microsoft.Extensions.DependencyInjection`.

## Highlights

- **Fluent handler registration** — chain `.On()`, `.OnItem()`, and `.Bind()` calls before starting the stream.
- **Strongly-typed events** — deserialize SSE data directly into your own types via `.On<T>()`.
- **Pluggable authentication** — API key, Bearer token (with refresh), Basic Auth, or roll your own `ISseAuthenticationProvider`.
- **Last-event-id resumption** — automatically replays the last received `id` on reconnect.
- **Automatic reconnection** — configurable retry policy and restart-on-abort support.
- **Multi-framework** — targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

## This Package

**SsePulse.Client.Authentication.DependencyInjection** bridges `SsePulse.Client.Authentication` and `SsePulse.Client.DependencyInjection`. It extends `ISseSourceBuilder` with `AddAuthentication()`, `AddBearerTokenAuthentication()`, `AddBasicAuthentication()`, and `AddApiKeyAuthentication()` overloads that register an `AuthenticationRequestMutator` applying the chosen provider to every outgoing request. Providers can be supplied as instances, resolved from the DI container, or via the configuration-binding overloads.
