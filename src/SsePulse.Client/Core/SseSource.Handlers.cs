using System.Net.ServerSentEvents;
using System.Reflection;
using System.Runtime.CompilerServices;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Attributes;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.Utils;
using SsePulse.Client.Common.Extensions;

namespace SsePulse.Client.Core;

public partial class SseSource
{
    private readonly SseHandlersDictionary _handlers = new();

    internal Action? OnDisposed { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked each time the SSE connection is successfully established.
    /// Exceptions thrown by the callback are silently swallowed to protect the consumption loop.
    /// </summary>
    public Action OnConnectionEstablished
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);
            _connectionHandlers.OnConnectionEstablished = field;
        }
    } = () => { };

    /// <summary>
    /// Gets or sets the callback invoked when the SSE connection is closed cleanly
    /// (i.e. the server ended the stream without an error).
    /// Exceptions thrown by the callback are silently swallowed to protect the consumption loop.
    /// </summary>
    public Action OnConnectionClosed
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);
            _connectionHandlers.OnConnectionClosed = field;
        }
    } = () => { };

    /// <summary>
    /// Gets or sets the callback invoked when the SSE connection drops unexpectedly due to an error.
    /// The <see cref="Exception"/> argument describes the failure.
    /// Exceptions thrown by the callback are silently swallowed to protect the consumption loop.
    /// </summary>
    public Action<Exception> OnConnectionLost
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);
            _connectionHandlers.OnConnectionLost = field;
        }
    } = _ => { };

    /// <summary>
    /// Gets or sets the callback invoked when an error occurs while processing an individual SSE event
    /// (e.g. deserialization failure or an exception thrown by a handler).
    /// Exceptions thrown by the callback are silently swallowed to protect the consumption loop.
    /// </summary>
    public Action<Exception> OnError
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);
        }
    } = ex => { Console.WriteLine("Error occurred: " + ex.Message + ""); };
    
    /// <summary>
    /// Registers a handler for raw <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the full <see cref="System.Net.ServerSentEvents.SseItem{T}"/> including metadata.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem(string eventName, Action<SseItem<string>> handler)
    {
        AssertNotDisposed();
        _handlers.AddHandler(eventName, handler);
        return this;
    }
    
    /// <summary>
    /// Registers a handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events.
    /// The event name is derived from <typeparamref name="TEventData"/>'s type name using
    /// <see cref="SsePulse.Client.Core.Configurations.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// The event data is deserialized from JSON into <typeparamref name="TEventData"/>.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(Action<SseItem<TEventData>> handler)
    {
        return OnItem(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy), 
            handler);
    }
    
    /// <summary>
    /// Registers a handler for typed <see cref="System.Net.ServerSentEvents.SseItem{T}"/> events with the specified event name.
    /// The event data is deserialized from JSON into <typeparamref name="TEventData"/>.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized <see cref="System.Net.ServerSentEvents.SseItem{T}"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource OnItem<TEventData>(string eventName, Action<SseItem<TEventData>> handler)
    {
        AssertNotDisposed();
        _handlers.AddStronglyTypedHandler(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers a handler for the raw data string of events with the specified event name.
    /// </summary>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the raw event data as a <see cref="string"/>.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On(string eventName, Action<string> handler)
    {
        AssertNotDisposed();
        _handlers.AddDataHandler(eventName, handler);
        return this;
    }

    /// <summary>
    /// Registers a handler for the deserialized data of events whose name is derived from
    /// <typeparamref name="TEventData"/>'s type name using
    /// <see cref="SsePulse.Client.Core.Configurations.SseSourceOptions.DefaultEventNameCasePolicy"/>.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="handler">Callback receiving the deserialized event data.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(Action<TEventData> handler)
    {
        return On(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy), 
            handler);
    }

    /// <summary>
    /// Registers a handler for the deserialized data of events with the specified event name.
    /// The event data is deserialized from JSON into <typeparamref name="TEventData"/>.
    /// </summary>
    /// <typeparam name="TEventData">The type to deserialize the event data into.</typeparam>
    /// <param name="eventName">The SSE event type string to match.</param>
    /// <param name="handler">Callback receiving the deserialized event data.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource On<TEventData>(string eventName, Action<TEventData> handler)
    {
        AssertNotDisposed();
        _handlers.AddStronglyTypedDataHandler(eventName, handler);
        return this;
    }
    
    /// <summary>
    /// Creates a new instance of <typeparamref name="TManager"/> using its parameterless constructor
    /// and binds all of its <c>On*</c> handler methods to their corresponding SSE event names.
    /// </summary>
    /// <typeparam name="TManager">
    /// An <see cref="ISseEventsManager"/> implementation with a parameterless constructor.
    /// </typeparam>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource Bind<TManager>() where TManager : ISseEventsManager, new()
    {
        TManager manager = Activator.CreateInstance<TManager>();
        return Bind(manager);
    }
    
    /// <summary>
    /// Invokes <paramref name="factory"/> to obtain an <typeparamref name="TManager"/> instance
    /// and binds all of its <c>On*</c> handler methods to their corresponding SSE event names.
    /// </summary>
    /// <typeparam name="TManager">An <see cref="ISseEventsManager"/> implementation.</typeparam>
    /// <param name="factory">Factory delegate that produces the manager instance.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource Bind<TManager>(Func<TManager> factory) where TManager : ISseEventsManager
    {
        return Bind(factory());
    }
    
    /// <summary>
    /// Binds all <c>On*</c> handler methods of the supplied <paramref name="manager"/> instance
    /// to their corresponding SSE event names. Use <see cref="MapEventNameAttribute"/> on a method
    /// to override the automatically derived event name.
    /// </summary>
    /// <typeparam name="TManager">An <see cref="ISseEventsManager"/> implementation.</typeparam>
    /// <param name="manager">The pre-created manager instance whose handlers will be registered.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource Bind<TManager>(TManager manager) where TManager : ISseEventsManager
    {
        AssertNotDisposed();
        
        MethodInfo addDataHandlerMethod = typeof(SseHandlersDictionary).GetMethod(nameof(SseHandlersDictionary.AddDataHandler), BindingFlags.Public | BindingFlags.Instance)!;
        MethodInfo addStronglyTypedDataHandlerMethod = typeof(SseHandlersDictionary).GetMethod(nameof(SseHandlersDictionary.AddStronglyTypedDataHandler), BindingFlags.Public | BindingFlags.Instance)!;

        IEnumerable<MethodInfo> methods = manager.GetType().GetMethods()
            .Where(m => m.Name.StartsWith("On") && m.GetParameters().Length == 1);

        foreach (MethodInfo method in methods)
        {
            string eventName = GetEventNameByMethod(method);
            Type eventDataType = method.GetParameters()[0].ParameterType;

            if (eventDataType == typeof(string))
            {
                Type actionType = typeof(Action<>).MakeGenericType(typeof(string));
                Delegate actionDelegate = method.CreateDelegate(actionType, manager);
                addDataHandlerMethod.Invoke(_handlers, [eventName, actionDelegate]);
            }
            else
            {
                Type actionType = typeof(Action<>).MakeGenericType(eventDataType);
                Delegate actionDelegate = method.CreateDelegate(actionType, manager);
                MethodInfo genericMethod = addStronglyTypedDataHandlerMethod.MakeGenericMethod(eventDataType);
                genericMethod.Invoke(_handlers, [eventName, actionDelegate]);
            }
        }

        return this;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Action WrapDefaultHandler(Action value)
    {
        return () => _ = Execute.WithIgnoreExceptionAsync(_ =>
        {
            value.Invoke();
            return Task.CompletedTask;
        });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Action<Exception> WrapDefaultHandler(Action<Exception> value)
    {
        return ex => _ = Execute.WithIgnoreExceptionAsync(_ =>
        {
            value.Invoke(ex);
            return Task.CompletedTask;
        });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetEvenName(string eventName)
    {
        return eventName.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy);
    }
    
    private string GetEventNameByMethod(MethodInfo method)
    {
        string methodNameWithoutPrefix = method.Name.Substring(2);
        MapEventNameAttribute? attribute = method.GetCustomAttribute<MapEventNameAttribute>();
        return attribute is not null 
            ? attribute.EventName 
            : GetEvenName(methodNameWithoutPrefix);
    }
}