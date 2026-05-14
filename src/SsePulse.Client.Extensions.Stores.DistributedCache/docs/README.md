# SsePulse.Client

**SsePulse.Client** is a .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library for consuming real-time event streams with minimal boilerplate. It offers a fluent handler-registration API, strongly-typed JSON deserialization, pluggable authentication, configurable retry and reconnect logic, and an extensible request-mutator pipeline — everything you need to integrate SSE into any .NET application, from lightweight console tools to full ASP.NET Core services backed by `Microsoft.Extensions.DependencyInjection`.

## Highlights

- **Fluent handler registration** — chain `.On()`, `.OnItem()`, and `.Bind()` calls before starting the stream.
- **Strongly-typed events** — deserialize SSE data directly into your own types via `.On<T>()`.
- **Pluggable authentication** — API key, Bearer token (with refresh), Basic Auth, or roll your own `ISseAuthenticationProvider`.
- **Last-event-id resumption** — automatically replays the last received `id` on reconnect.
- **Automatic reconnection** — configurable retry policy and restart-on-abort support.
- **Multi-framework** — targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

## This Package

**SsePulse.Client.Extensions.Stores.DistributedCache** provides `DistributedCacheLastEventIdStore`, an `IDistributedCache`-backed implementation of `ILastEventIdStore` that persists the last received SSE event ID to any distributed cache (Redis, SQL Server, in-memory, etc.). On startup the store reads the previously saved ID so the stream can resume from where it left off, even after a process restart. Every call to `Set` writes the event ID under a configurable cache key. If the cache write fails the error is logged at `Error` level, `LastEventId` is **not** updated, and SSE processing continues uninterrupted. The store is configured via `DistributedCacheLastEventIdStoreOptions` (`Key`). Use this package when you need durable last-event-ID tracking and prefer to wire things up manually, without a dependency-injection container.

