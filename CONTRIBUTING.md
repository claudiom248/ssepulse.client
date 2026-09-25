# Contributing

## Workflow

`master` is the only long-lived branch and is always releasable.

1. Create a short-lived branch from `master` (`feature/<topic>` or `fix/<topic>`).
2. Open a pull request against `master`. CI must pass.
3. Pull requests are squash-merged, so the PR title becomes the commit message and must follow [Conventional Commits](https://www.conventionalcommits.org/), for example:
   - `feat(core): add async event handlers`
   - `fix(stores): flush pending ids on dispose`
   - `feat(di)!: replace the scoped factory` (a `!` marks a breaking change)

The commit types feed the generated release notes: `feat`, `fix`, `perf`, `refactor`, `doc` and `chore`.

## Building and testing

```bash
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet test -c Release --no-build --filter "Category!=IntegrationTests"
```

Integration tests use Testcontainers and need Docker. Package versions are pinned with lock files: after changing a dependency run `dotnet restore --force-evaluate` and commit the updated `packages.lock.json` files.

## Versioning and releases

Versions are calculated from git tags by [MinVer](https://github.com/adamralph/minver). Commits on `master` produce preview versions (`2.0.0-preview.0.<height>`), which are pushed to GitHub Packages.

To release, push a tag:

```bash
git tag v2.0.0-rc.1
git push origin v2.0.0-rc.1
```

The `release` workflow then runs the tests, builds the packages, waits for approval on the `nuget` environment, publishes to nuget.org and creates a GitHub release with notes generated from the commits. Stable tags also publish the documentation site.

Publishing to nuget.org uses NuGet trusted publishing: the `NUGET_USER` repository variable holds the nuget.org profile name, and a trusted publishing policy for this repository and the `release.yml` workflow has to exist on nuget.org.

## Writing tests

Tests must be deterministic: `Task.Delay` and `Thread.Sleep` are banned in test projects (see `tests/BannedSymbols.txt`) and the build fails if they are used. Everything you need to avoid real waiting lives in `tests/SsePulse.Client.Tests.Common`.

### Scripted SSE server

`SseTestServer` is an in-process server that plays one script per incoming connection and records every request (method, headers, body, `Last-Event-ID`). A connection that has no script left gets a `500`.

```csharp
await using SseTestServer server = await SseTestServer.StartAsync(s => s
    .OnConnection(c => c.Send("order", "1", id: "1").WaitFor(gate).Drop())
    .OnConnection(c => c.Respond(503, ("Retry-After", "2")))
    .OnConnection(c => c.Send("order", "2", id: "2").Close()));
```

Available steps: `Send`, `Comment`, `Retry`, `Raw`, `WaitFor(gate)`, `Respond(status, headers)`, `Close()` (graceful end), `Drop()` (abrupt end) and `KeepOpen()` (headers sent, then silence).

### Source harness

`SseSourceHarness` builds an `SseSource` against the server with a `FakeTimeProvider` and records events, errors and connection lifecycle changes. Wait for what you expect instead of sleeping:

```csharp
await using SseSourceHarness harness = new SseSourceHarness(server, lastEventIdStore: store)
    .Listen("order")
    .Start();

await harness.Events.WaitForCountAsync(2);
gate.Open();
await harness.Events.WaitForCountAsync(3);
Assert.Equal("2", server.Requests[1].LastEventId);
```

- `SseGate` lets a test decide when the server continues a script.
- `Recorder<T>.WaitForCountAsync` completes as soon as enough items were recorded and fails with a clear message otherwise.
- Code that waits on time (connection retries, the file store flush interval) takes a `TimeProvider`; pass the harness `Time` or a `FakeTimeProvider` and call `Advance`. `AdvanceTimeUntilAsync` advances the clock until a condition holds.
- `TestWait.UntilAsync` polls a condition against real external systems (for example a Redis key expiring); use it only when fake time is not possible.

### Store contract tests

Every `ILastEventIdStore` implementation has a test class that inherits `LastEventIdStoreContract` (in-memory behaviour) or `DurableLastEventIdStoreContract` (persistence and failure isolation). To add a store, inherit the contract and implement the factory methods. Integration variants run against Testcontainers and carry the `IntegrationTests` trait.
