using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Abstractions;

/// <summary>
/// Fluent builder for configuring a named SSE source in the dependency-injection container.
/// Obtained by calling <c>services.AddSseSource()</c> and used to wire up the HTTP client,
/// event handlers, and other components.
/// </summary>
public interface ISseSourceBuilder
{
    /// <summary>Gets the underlying service collection where registrations are applied.</summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the optional configuration section bound to this source, or <see langword="null"/>
    /// when the source was registered without a configuration object.
    /// </summary>
    IConfiguration? Configuration { get; }

    /// <summary>Gets the name that uniquely identifies this SSE source registration.</summary>
    string Name { get; }

    /// <summary>
    /// Registers a named <see cref="HttpClient"/> for this source using default settings.
    /// </summary>
    /// <returns>The same builder for chaining.</returns>
    SseSourceBuilder AddHttpClient();

    /// <summary>
    /// Registers a named <see cref="HttpClient"/> configured by <paramref name="configureClient"/>.
    /// </summary>
    /// <param name="configureClient">Delegate that configures the <see cref="HttpClient"/> (e.g. base address).</param>
    /// <returns>The same builder for chaining.</returns>
    SseSourceBuilder AddHttpClient(Action<HttpClient> configureClient);

    /// <summary>
    /// Registers a named <see cref="HttpClient"/> with optional client configuration and
    /// access to the underlying <see cref="IHttpClientBuilder"/> for advanced setup.
    /// </summary>
    /// <param name="configureClient">Delegate to configure the <see cref="HttpClient"/>; may be <see langword="null"/>.</param>
    /// <param name="clientBuilder">Delegate to further configure the <see cref="IHttpClientBuilder"/>; may be <see langword="null"/>.</param>
    /// <returns>The same builder for chaining.</returns>
    SseSourceBuilder AddHttpClient(Action<HttpClient>? configureClient, Action<IHttpClientBuilder>? clientBuilder);

    /// <summary>
    /// Instructs this SSE source to reuse an existing named <see cref="HttpClient"/> instead of
    /// registering a new one.
    /// </summary>
    /// <param name="clientName">The name of the already-registered HTTP client to use.</param>
    /// <returns>The same builder for chaining.</returns>
    SseSourceBuilder UseHttpClient(string clientName);

    /// <summary>
    /// Registers a callback that is invoked when the <see cref="SseSource"/> is created,
    /// allowing handler registration at resolution time.
    /// </summary>
    /// <param name="registerHandlers">
    /// Callback receiving the <see cref="IServiceProvider"/> and the <see cref="SseSource"/> being configured.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder RegisterHandlers(Action<IServiceProvider, SseSource> registerHandlers);

    /// <summary>
    /// Binds a pre-created <see cref="ISseEventsManager"/> instance to this SSE source.
    /// </summary>
    /// <param name="manager">The manager instance whose <c>On*</c> methods will be registered as handlers.</param>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder BindEventsManager(ISseEventsManager manager);
    
    /// <summary>
    /// Binds an <see cref="ISseEventsManager"/> implementation resolved from the DI container
    /// to this SSE source at creation time.
    /// </summary>
    /// <typeparam name="TManager">The events manager type registered in the DI container.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder BindEventsManager<TManager>() where TManager : ISseEventsManager;
    
    /// <summary>
    /// Binds an <see cref="ISseEventsManager"/> resolved via a factory delegate at source creation time.
    /// </summary>
    /// <param name="managerFactory">Factory that receives the <see cref="IServiceProvider"/> and returns the manager.</param>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder BindEventsManager(Func<IServiceProvider, ISseEventsManager> managerFactory);
    
    /// <summary>
    /// Registers an <see cref="IRequestMutator"/> implementation resolved from the DI container
    /// to be applied to every outgoing request made by this SSE source.
    /// </summary>
    /// <typeparam name="TRequestMutator">The mutator type registered in the DI container.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder AddRequestMutator<TRequestMutator>() where TRequestMutator : IRequestMutator;

    /// <summary>
    /// Registers a pre-created <see cref="IRequestMutator"/> instance to be applied to every
    /// outgoing request made by this SSE source.
    /// </summary>
    /// <param name="mutator">The mutator instance to register.</param>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder AddRequestMutator(IRequestMutator mutator);

    /// <summary>
    /// Registers an <see cref="IRequestMutator"/> resolved via a factory delegate at source
    /// creation time to be applied to every outgoing request made by this SSE source.
    /// </summary>
    /// <param name="mutatorFactory">Factory that receives the <see cref="IServiceProvider"/> and returns the mutator.</param>
    /// <returns>The same builder for chaining.</returns>
    ISseSourceBuilder AddRequestMutator(Func<IServiceProvider, IRequestMutator> mutatorFactory);
}