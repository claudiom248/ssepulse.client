# Changelog

All notable changes to this project are documented below.
## [1.2.0](https://github.com/claudiom248/ssepulse.client/compare/v1.1.0..v1.2.0) - 2026-05-04

### 🚀 Features

- **`core/runtime`** — Add new properties to `SseSourceOptions` for customizing transient failures detection during connection and consumption [#31](https://github.com/claudiom248/ssepulse.client/issues/31)
- **`core/runtime`** — Add `FileLastEventIdStore` for persisting last event ID to a file [#28](https://github.com/claudiom248/ssepulse.client/issues/28)
- **`core/runtime`** — Enhance logging by including contextual information about the `SseSource`, and logged more operations (establishing connection, applying mutators, etc) [#30](https://github.com/claudiom248/ssepulse.client/issues/30)
- **`dependency-injection`** — Add `AddFileLastEventIdStore` method for registering the file based last event ID store
- **`mutators`** — Enhance logging in request mutators [#30](https://github.com/claudiom248/ssepulse.client/issues/30)

### 🐛 Bug Fixes

- **`core/runtime`** — Set `TaskCreationOptions.RunContinuationsAsynchronously` to avoid potential deadlocks when resetting the `TaskCompletionSource` handled by the source
- **`core/runtime`** — Fix a bug causing different instances of the same events source to share the same `ILastEventIdStore`

### 📚 Documentation

- **`core`** — Add documentation for `SseSource` architecture and implementation [#29](https://github.com/claudiom248/ssepulse.client/issues/29)
- **`core/runtime`** — Add documentation for the new options allowing the customization of transient failures detection during connection and consumption [#31](https://github.com/claudiom248/ssepulse.client/issues/31)
- **`core/runtime`** — Add documentation for Last-Event-ID resumption and file-based store [#28](https://github.com/claudiom248/ssepulse.client/issues/28)
- **`dependency-injection`** — Document how to register `FileLastEventIdStore` using the builder [#28](https://github.com/claudiom248/ssepulse.client/issues/28)
- Add a logo to the website and change website template

### 🔧 Maintenance

- **`dependencies`** — Bump .NET packages versions from 10.0.5 to 10.0.7
## [1.1.0](https://github.com/claudiom248/ssepulse.client/compare/v1.0.0..v1.1.0) - 2026-04-26

### 🚀 Features

- **`hosting`** — Add support to hosted services for automating the consumption from SSE sources [#15](https://github.com/claudiom248/ssepulse.client/issues/15)
- **`serialization`** — Add support to custom JSON serializer options [#17](https://github.com/claudiom248/ssepulse.client/issues/17)

### 🐛 Bug Fixes

- **`dependency-injection`** — Fixed a bug where the configured ILastEventIdStore was not shared between SseSource and LastEventIdRequestMutator when registering using the builder [#18](https://github.com/claudiom248/ssepulse.client/issues/18)

### 📚 Documentation

- **`hosting`** — Add description for package `SsePulse.Client.Hosting` in packages lists
- **`serialization`** — Add documentation guides and references for the new JSON Serializer [#15](https://github.com/claudiom248/ssepulse.client/issues/15)

### 🔧 Maintenance

- **`ci`** — Remove Windows from CI workflow matrix for speeding up builds
- **`ci`** — Update CI workflow to produce GitHub Packages
- **`release`** — Add release workflow to push packages to nuget and automate docs build and deployment
- **`release`** — Add automatic release notes generation
- **`release`** — Add prepare-release workflow for GitHub releases
- Add CHANGELOG.md
## [1.0.0](https://github.com/claudiom248/ssepulse.client/commit/f6f8fd19c6f15dcf8ac802b10bf495bd7f5c5ce6) - 2026-04-19

### <!-- 0 -->🚀 Features

- **`core/runtime`** — Introduce `SseSource`, the central component for connecting to and consuming SSE streams with automatic reconnection, configurable retry, and structured event dispatching
- **`core/handlers`** — Add strongly-typed event handler registration — bind handlers by event name, by C# type, or by reflecting over a dedicated event manager class
- **`mutators`** — Add `IRequestMutator` pipeline to customise outgoing HTTP requests before each connection attempt (headers, tokens, last-event-id, and more)
- **`authentication`** — Add authentication support for protected SSE endpoints. See the [authentication guide](docs/docs/authentication.md) for available providers and configuration options
- **`dependency-injection`** — Add Microsoft DI integration with a fluent builder for registering single or multiple named `SseSource` instances into the service container. See the [dependency-injection guide](docs/docs/dependency-injection.md) to learn how to use in your project.


### <!-- 4 -->📚 Documentation

- **`doc`** — Write full XML documentation on all public APIs
- **`doc`** — Add README with quickstart guide and highlights
- **`doc`** — Add DocFx for automatic documentation site creation. See (https://claudiom248.github.io/ssepulse.client/)


### <!-- 5 -->🔧 Maintenance
- **`ci`** — Add Multi-OS / multi-TFM CI pipeline with test reporting and NuGet packaging

### New Contributors
* @claudiom248 made their first contribution

<!-- generated by git-cliff -->
