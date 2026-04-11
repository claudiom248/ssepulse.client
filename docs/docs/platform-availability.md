# Platform Availability

The `SseSource` API reference is generated from the `net10.0` target, which exposes the full surface area of the library. However, the library is designed to be highly compatible across various .NET versions.

A small number of members are conditionally available or exhibit slightly different behaviors depending on the target framework you are using.

## Support Matrix

| Member | `net10.0` | `net9.0` | `net8.0` | `netstandard2.0` |
| :--- | :---: | :---: | :---: | :---: |
| **`SseSource.DisposeAsync()`** | ✔ | ✔ | ✔ | ✔ |
| **`IAsyncDisposable` on `SseSource`** | ✔ | ✔ | ✔ | ✔ |
| **`SseSource.StopAsync()`** | ✔ (Async cancel) | ✔ (Async cancel) | ✔ (Async cancel) | ⚠️ **Sync fallback** |

---

## Target-Specific Notes

### `netstandard2.0` Synchronous Fallback

When targeting `netstandard2.0`, calling `StopAsync()` falls back to a **synchronous** cancellation.

* **Reasoning:** It performs a synchronous `CancellationTokenSource.Cancel()` call because the native `CancellationTokenSource.CancelAsync()` method is not available on that specific target framework.