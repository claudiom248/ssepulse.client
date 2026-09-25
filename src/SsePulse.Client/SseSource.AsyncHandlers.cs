using System.Net.ServerSentEvents;
using SsePulse.Client.Internal;

namespace SsePulse.Client;

public partial class SseSource
{
    /// <summary>
    /// Registers an asynchronous handler for raw <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the full <see cref="System.Net.ServerSentEvents.SseItem{T}"/> including metadata. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem(string eventName, Func<SseItem<string>, CancellationToken, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncHandler(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for raw <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the full <see cref="System.Net.ServerSentEvents.SseItem{T}"/> including metadata.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem(string eventName, Func<SseItem<string>, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncHandler(eventName, HandlerAdapter.ToAsync(handler));
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events. The event name is derived from <typeparamref name="TEventData"/>'s type name using <see cref="SsePulse.Client.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(Func<SseItem<TEventData>, CancellationToken, ValueTask> handler)
    {
        return OnItem(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy),
            handler);
    }

    /// <summary>
    /// Registers an asynchronous handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events. The event name is derived from <typeparamref name="TEventData"/>'s type name using <see cref="SsePulse.Client.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(Func<SseItem<TEventData>, ValueTask> handler)
    {
        return OnItem(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy),
            handler);
    }

    /// <summary>
    /// Registers an asynchronous handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(string eventName, Func<SseItem<TEventData>, CancellationToken, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncStronglyTypedHandler<TEventData>(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(string eventName, Func<SseItem<TEventData>, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncStronglyTypedHandler<TEventData>(eventName, HandlerAdapter.ToAsync(handler));
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for the raw data string of events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the raw event data as a <see cref="string"/>. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On(string eventName, Func<string, CancellationToken, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncDataHandler(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for the raw data string of events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the raw event data as a <see cref="string"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On(string eventName, Func<string, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncDataHandler(eventName, HandlerAdapter.ToAsync(handler));
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for the deserialized data of events whose name is derived from <typeparamref name="TEventData"/>'s type name using <see cref="SsePulse.Client.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized event data. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(Func<TEventData, CancellationToken, ValueTask> handler)
    {
        return On(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy),
            handler);
    }

    /// <summary>
    /// Registers an asynchronous handler for the deserialized data of events whose name is derived from <typeparamref name="TEventData"/>'s type name using <see cref="SsePulse.Client.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized event data.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(Func<TEventData, ValueTask> handler)
    {
        return On(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy),
            handler);
    }

    /// <summary>
    /// Registers an asynchronous handler for the deserialized data of events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized event data. The token is cancelled when the source stops or is disposed, and when event processing faults.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(string eventName, Func<TEventData, CancellationToken, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncStronglyTypedDataHandler<TEventData>(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers an asynchronous handler for the deserialized data of events with the specified event name.
    /// The handler is awaited before the next event is dispatched when <see cref="SsePulse.Client.SseSourceOptions.MaxDegreeOfParallelism"/> is 1.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized event data.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(string eventName, Func<TEventData, ValueTask> handler)
    {
        AssertNotDisposed();
        AssertNotStarted();
        _handlers.AddAsyncStronglyTypedDataHandler<TEventData>(eventName, HandlerAdapter.ToAsync(handler));
        return this;
    }
}