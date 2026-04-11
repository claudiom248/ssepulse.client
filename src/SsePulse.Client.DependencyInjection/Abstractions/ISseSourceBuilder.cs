using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Abstractions;

public interface ISseSourceBuilder
{
    public IServiceCollection Services { get; }
    IConfiguration? Configuration { get; }
    string Name { get; }

    internal ISseSourceBuilder AddRequestMutator<TRequestMutator>() where TRequestMutator : IRequestMutator;
    internal ISseSourceBuilder AddRequestMutator(IRequestMutator mutator);
    internal ISseSourceBuilder AddRequestMutator(Func<IServiceProvider, IRequestMutator> mutatorFactory);
    SseSourceBuilder AddHttpClient();
    SseSourceBuilder AddHttpClient(Action<HttpClient> configureClient);
    SseSourceBuilder AddHttpClient(Action<HttpClient>? configureClient, Action<IHttpClientBuilder>? clientBuilder);
    ISseSourceBuilder BindEventsManager<TManager>() where TManager : ISseEventsManager;
    ISseSourceBuilder BindEventsManager(ISseEventsManager manager);
}