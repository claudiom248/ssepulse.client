# SsePulse.Client

**SsePulse.Client** is a .NET [Server-Sent Events (SSE)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) client library for consuming real-time event streams with minimal boilerplate. It provides strongly-typed event handling, resilient connection management, pluggable authentication, and an extensible request pipeline so you can integrate SSE into anything from small utilities to full `Microsoft.Extensions`-based applications.

## Highlights

- **Developer-first API** — focus on business events, not low-level stream plumbing.
- **Strong typing end to end** — map incoming data directly to your domain models.
- **Resilient stream consumption** — built-in retry, reconnect, and event-resume support.
- **Flexible security model** — use built-in authentication providers or bring your own.
- **Host-ready architecture** — run SSE workloads cleanly in modern background-service applications.
- **Broad framework support** — targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

## This Package

**SsePulse.Client.Hosting** provides hosting integration for `SsePulse.Client` on top of `Microsoft.Extensions.Hosting`. It enables registering SSE consumers as host-managed background services, including single-source and multi-source scenarios, and provides a default hosted-service implementation that coordinates source start and graceful shutdown within the application lifecycle. It also supports custom hosted-service implementations when custom orchestration is required.

For further details and examples, see the [Hosted Service guide](https://github.com/claudiom248/SsePulse.Client/blob/main/docs/docs/hosted-services.md).

