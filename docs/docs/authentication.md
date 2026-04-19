# Authentication

SsePulse offers several built-in authentication mechanisms. Each is implemented as an
`ISseAuthenticationProvider` and applied to every outgoing request before the connection
is established.

---

## Standalone usage (without DI)

Setup an `AuthenticationRequestMutator` instance, pass the provider and add it to the
request mutators pipeline when creating the `SseSource`. Use this approach when you don't
use DI.

### API key

```csharp
var provider = new ApiKeyAuthenticationProvider(new ApiKeyAuthenticationProviderConfiguration
{
    Key = "my-secret-key",
    Header = "X-Api-Key"          // default is "X-API-Key"
});

var source = new SseSource(httpClient, options, requestMutators: [new AuthenticationRequestMutator(provider)]);
```

### Bearer token

Choose the `ITokenProvider` implementation that matches your scenario.

**Static token** — a fixed, never-expiring token:

```csharp
var provider = new BearerTokenAuthenticationProvider(new StaticTokenProvider("my-jwt-token"));
```

**Environment variable** — reads the token from a named environment variable at runtime:

```csharp
var provider = new BearerTokenAuthenticationProvider(
    new EnvironmentVariableTokenProvider("MY_APP_TOKEN"));
```

**Client credentials (OAuth 2.0)** — automatically fetches and refreshes an access token
from a token endpoint:

```csharp
var credentials = new ClientCredentials("client-id", "client-secret");
var config = new ClientCredentialsTokenProviderConfiguration(
    new Uri("https://auth.example/token"),
    credentials);
var provider = new BearerTokenAuthenticationProvider(new ClientCredentialsTokenProvider(config));
```

**Delegating provider** — supply a custom async lambda when none of the above fit:

```csharp
var provider = new BearerTokenAuthenticationProvider(
    new DelegatingTokenProvider(async ct =>
    {
        string token = await myTokenService.GetTokenAsync(ct);
        return token;
    }));
```

### Basic auth

```csharp
var provider = new BasicAuthenticationProvider(new BasicCredentials("username", "password"));
```

### Custom provider

Implement `ISseAuthenticationProvider` to support any other scheme:

```csharp
public class HmacAuthenticationProvider : ISseAuthenticationProvider
{
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string signature = ComputeHmac(request);
        request.Headers.Add("X-Signature", signature);
        return ValueTask.CompletedTask;
    }
}
```

---

## Dependency injection

Install `SsePulse.Client.Authentication.DependencyInjection` and call one of the
`AddAuthentication` overloads on the `ISseSourceBuilder` returned by `AddSseSource()`.

### API key

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddApiKeyAuthentication(new ApiKeyAuthenticationProviderConfiguration
    {
        Key = "my-secret-key",
        Header = "X-Api-Key"
    });
```

Configure through a delegate (resolves from the options system):

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddApiKeyAuthentication(cfg =>
    {
        cfg.Key = "my-secret-key";
        cfg.Header = "X-Api-Key";
    });
```

### Bearer token

Pass a pre-built `ITokenProvider` instance:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddBearerTokenAuthentication(new StaticTokenProvider("my-jwt-token"));
```

Resolve the token provider from DI at runtime:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddBearerTokenAuthentication(sp => sp.GetRequiredService<IMyTokenService>());
```

### Basic auth

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddBasicAuthentication(new BasicCredentials("username", "password"));
```

Configure credentials through a delegate:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddBasicAuthentication(creds =>
    {
        creds.Username = "username";
        creds.Password = "password";
    });
```

### Custom provider

Register a pre-built instance:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddAuthentication(new HmacAuthenticationProvider());
```

Resolve from DI:

```csharp
services.AddSingleton<HmacAuthenticationProvider>();

services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddAuthentication<HmacAuthenticationProvider>();
```

Use a factory for full control:

```csharp
services
    .AddSseSource(options => options.Path = "/events")
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddAuthentication(sp =>
    {
        string secret = sp.GetRequiredService<ISecretsService>().GetSecret("hmac-key");
        return new HmacAuthenticationProvider(secret);
    });
```

---

## Configuration-driven authentication

When the `ISseSourceBuilder` was created with an `IConfiguration` section that contains an
`Authentication` sub-section, you can call the parameterless `AddAuthentication()` and the
provider is resolved entirely from configuration.

```json
{
  "SseSource": {
    "Path": "/events",
    "Authentication": {
      "Provider": "Bearer",
      "Args": {
        "TokenProvider": "ClientCredentials",
        "TokenEndpoint": "https://auth.example/token",
        "Credentials": {
          "ClientId": "my-client-id",
          "ClientSecret": "my-client-secret"
        }
      }
    }
  }
}
```

```csharp
services
    .AddSseSource(configuration.GetSection("SseSource"))
    .AddHttpClient(client => client.BaseAddress = new Uri("https://my-server.example"))
    .AddAuthentication();   // provider type is read from the "Authentication" section
```

Supported `Provider` values: `Bearer`, `Basic`, `ApiKey`.  
Supported `TokenProvider` values (for Bearer): `Static`, `ClientCredentials`, `EnvironmentVariable`.

