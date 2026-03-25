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
    
    public Action OnConnectionEstablished
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);
            _connectionHandlers.OnConnectionEstablished = field;
        }
    } = () => { Console.WriteLine("Connection established"); };

    public Action OnConnectionClosed
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);   
            _connectionHandlers.OnConnectionClosed = field;
        }
    } = () => { Console.WriteLine("Connection gracefully closed"); };

    public Action<Exception> OnConnectionLost
    {
        get;
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);        
            _connectionHandlers.OnConnectionLost = field;
        }
    } = ex => { Console.WriteLine("Connection lost due to: " + ex.Message + ""); };
    

    public Action<Exception> OnError 
    { 
        get; 
        set
        {
            AssertNotDisposed();
            field = WrapDefaultHandler(value);        
        } 
    } = ex => { Console.WriteLine("Error occurred: " + ex.Message + ""); };
    
    public SseSource OnItem(string eventName, Action<SseItem<string>> handler)
    {
        AssertNotDisposed();
        _handlers.AddHandler(eventName, handler);
        return this;
    }
    
    public SseSource OnItem<TEventData>(Action<SseItem<TEventData>> handler)
    {
        return On(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy), 
            handler);
    }
    
    public SseSource OnItem<TEventData>(string eventName, Action<SseItem<TEventData>> handler)
    {
        AssertNotDisposed();
        _handlers.AddStronglyTypedHandler(eventName, handler);
        return this;
    }

    public SseSource On(string eventName, Action<string> handler)
    {
        AssertNotDisposed();
        _handlers.AddDataHandler(eventName, handler);
        return this;
    }

    public SseSource On<TEventData>(Action<TEventData> handler)
    {
        return On(
            typeof(TEventData).Name.ApplyNamingCasePolicy(_options.DefaultEventNameCasePolicy), 
            handler);
    }

    public SseSource On<TEventData>(string eventName, Action<TEventData> handler)
    {
        AssertNotDisposed();
        _handlers.AddStronglyTypedDataHandler(eventName, handler);
        return this;
    }
    
    public SseSource Bind<TManager>() where TManager : ISseEventsManager
    {
        TManager manager = Activator.CreateInstance<TManager>();
        return Bind(manager);
    }
    
    public SseSource Bind<TManager>(TManager manager) where TManager : ISseEventsManager
    {
        AssertNotDisposed();
        
        MethodInfo addDataHandlerMethod = typeof(SseHandlersDictionary).GetMethod(nameof(SseHandlersDictionary.AddDataHandler), BindingFlags.Public | BindingFlags.Instance)!;
        MethodInfo addStronglyTypedDataHandlerMethod = typeof(SseHandlersDictionary).GetMethod(nameof(SseHandlersDictionary.AddStronglyTypedDataHandler), BindingFlags.Public | BindingFlags.Instance)!;

        IEnumerable<MethodInfo> methods = typeof(TManager).GetMethods()
            .Where(m => m.Name.StartsWith("On") && m.GetParameters().Length == 1);

        foreach (MethodInfo method in methods)
        {
            string eventName = GetEvenNameByMethod(method);
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
    
    private string GetEvenNameByMethod(MethodInfo method)
    {
        string methodNameWithoutPrefix = method.Name.Substring(2);
        MapEventNameAttribute? attribute = method.GetCustomAttribute<MapEventNameAttribute>();
        return attribute is not null 
            ? attribute.EventName 
            : GetEvenName(methodNameWithoutPrefix);
    }
}