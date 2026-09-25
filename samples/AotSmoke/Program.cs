using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SsePulse.Client;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
await using WebApplication app = builder.Build();

app.MapGet("/events", async (HttpContext context) =>
{
    context.Response.ContentType = "text/event-stream";
    await context.Response.WriteAsync("id: 1\nevent: OrderCreated\ndata: {\"id\":1,\"customer\":\"first\"}\n\n");
    await context.Response.WriteAsync("id: 2\nevent: OrderCreated\ndata: {\"id\":2,\"customer\":\"second\"}\n\n");
});

await app.StartAsync();
string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

using HttpClient client = new() { BaseAddress = new Uri(address) };
await using SseSource source = new(client, new SseSourceOptions
{
    Path = "/events",
    JsonSerializerOptions = SmokeJsonContext.Default.Options
});

List<OrderCreated> received = [];
source.On<OrderCreated>(order => received.Add(order));
await source.StartConsumeAsync(CancellationToken.None);
await app.StopAsync();

if (received.Count == 2 && received[0] == new OrderCreated(1, "first") && received[1] == new OrderCreated(2, "second"))
{
    Console.WriteLine("OK: received 2 events");
    return 0;
}

Console.Error.WriteLine($"FAILED: received {received.Count} events");
return 1;

public sealed record OrderCreated(int Id, string Customer);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(OrderCreated))]
public sealed partial class SmokeJsonContext : JsonSerializerContext;
