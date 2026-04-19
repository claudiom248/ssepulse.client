# Request Mutators

Request mutators are the extension point for modifying every outgoing HTTP request before it
reaches the SSE endpoint. Common use-cases include adding custom headers, injecting query
parameters, or performing any kind of per-request enrichment.

---

## The `IRequestMutator` interface

A mutator is any type that implements `IRequestMutator`:

```csharp
public interface IRequestMutator
{
    ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}
```

`ApplyAsync` is called once per connection attempt, just before the request is sent.
The `HttpRequestMessage` is fully populated at that point (method, URI, default headers), so
you can read or write any part of it.

---

## Implementing a custom mutator

```csharp
public class CorrelationIdMutator : IRequestMutator
{
    public ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        message.Headers.TryAddWithoutValidation("X-Correlation-Id", Guid.NewGuid().ToString());
        return ValueTask.CompletedTask;
    }
}
```

This simple mutator will apply a random correlation ID to every outgoing request.

---

## Adding mutators

When constructing `SseSource` manually, mutators are passed directly to the constructor.
The full constructor accepts a collection of mutators that are applied on every connection attempt:

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://example.com") };
var options = new SseSourceOptions { Path = "/events" };

IRequestMutator[] mutators =
[
    new CorrelationIdMutator(),
    new TenantHeaderMutator("acme"),
];

var source = new SseSource(httpClient, options, mutators);

await source.StartConsumeAsync(cancellationToken);
```

Mutators are applied in the order they appear in the collection.
Pass an empty array (`[]`) or omit the parameter entirely by using the shorter overload
when no mutation is required:

```csharp
// Shorter overload — no mutators, no last-event-id store
var source = new SseSource(httpClient, options);
```

---

## Using mutators with dependency injection

`ISseSourceBuilder` exposes three `AddRequestMutator` overloads so every mutator style
integrates with the DI container.

### Instance overload

Use this when the mutator has no dependencies and can be created upfront:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddRequestMutator(new CorrelationIdMutator());
```

### Generic / DI-resolved overload

Use this when the mutator has constructor dependencies that should be satisfied by the
container. Register the type first, then reference it by its type parameter:

```csharp
services.AddScoped<CorrelationIdMutator>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddRequestMutator<CorrelationIdMutator>();
```

### Factory overload

Use this for fine-grained control over resolution — for example, when the mutator depends on a
named or keyed service:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddRequestMutator(sp =>
    {
        var config = sp.GetRequiredService<IOptions<MyConfig>>().Value;
        return new CustomHeaderMutator(config.ApiKey);
    });
```

### Chaining multiple mutators

All three overloads return `ISseSourceBuilder`, so you can chain as many mutators as you need:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddRequestMutator(new CorrelationIdMutator())
    .AddRequestMutator<TenantHeaderMutator>()
    .AddRequestMutator(sp => new CustomHeaderMutator(sp.GetRequiredService<IOptions<MyConfig>>().Value.ApiKey));
```

Mutators are applied in the order they are registered.

---

## Built-in mutators

SsePulse ships two mutators that are wired in automatically when you use the corresponding
builder extensions.

### Last-Event-ID mutator

Adds the `Last-Event-ID` header to every reconnection attempt so the server can resume from
where the stream was interrupted. Enabled via `AddLastEventId`:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddLastEventId();   // from SsePulse.Client.DependencyInjection
```

See the [Dependency Injection](dependency-injection.md) page for details on custom
`ILastEventIdStore` implementations.

### Authentication mutator

Wraps an `ISseAuthenticationProvider` and applies it as a request mutator. All
`AddAuthentication` / `AddBearerTokenAuthentication` / `AddBasicAuthentication` /
`AddApiKeyAuthentication` extension methods ultimately call `AddRequestMutator` under the hood:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://example.com"))
    .AddBearerTokenAuthentication(tokenProvider);   // from SsePulse.Client.Authentication.DependencyInjection
```

See the [Authentication](authentication.md) page for additional details.

