# JSON Serializer Options

You can configure the options used by the internal json serializer in two equivalent ways:

- Set `SseSourceOptions.JsonSerializerOptions` directly.
- Use the method `WithSerializerOptions(JsonSerializerOptions options)` on the DI builder when configuring a source through.

`WithSerializerOptions(...)` is a convenience API that sets `SseSourceOptions.JsonSerializerOptions` for that named source.

## When to use it

Use custom serializer options when you need to customize the options used by the serializer, for example:

- Property-name casing differences (`snake_case`, `camelCase`, etc.)
- Enum serialization as strings
- Custom converters for domain-specific payloads

---

## Configure directly on `SseSourceOptions`

```csharp
JsonSerializerOptions serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

SseSourceOptions options = new SseSourceOptions
{
    Path = "/events/orders",
    JsonSerializerOptions = serializerOptions
};

HttpClient client = new HttpClient
{
    BaseAddress = new Uri("https://api.example")
};

await using SseSource source = new SseSource(client, options);
```

---

## Configure through DI builder

```csharp
JsonSerializerOptions serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

services
    .AddSseSource("orders", options => options.Path = "/events/orders")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://api.example"))
    .WithSerializerOptions(serializerOptions);
```

---

## Adding converters

```csharp
JsonSerializerOptions serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

serializerOptions.Converters.Add(new JsonStringEnumConverter());
serializerOptions.Converters.Add(new MoneyJsonConverter());

services
    .AddSseSource("billing", options => options.Path = "/events/billing")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://api.example"))
    .WithSerializerOptions(serializerOptions);
```

---

## Multiple named sources

Each source can use its own serializer settings.

```csharp
JsonSerializerOptions ordersOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

JsonSerializerOptions auditOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    PropertyNameCaseInsensitive = false
};

services
    .AddSseSource("orders", options => options.Path = "/events/orders")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://orders.example"))
    .WithSerializerOptions(ordersOptions);

services
    .AddSseSource("audit", options => options.Path = "/events/audit")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://audit.example"))
    .WithSerializerOptions(auditOptions);
```

---

## Notes

- Apply serializer options during source configuration.
- The configured options affect typed handlers (for example `.On<T>(...)` and `.OnItem<T>(...)`).
- Use one `JsonSerializerOptions` instance per source when serialization options must differ.
