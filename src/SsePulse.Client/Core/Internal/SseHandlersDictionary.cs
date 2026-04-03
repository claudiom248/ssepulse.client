using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core.Internal;

internal class SseHandlersDictionary : Dictionary<string, ISseEventHandler>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHandler(string eventName, Action<SseItem<string>> handler)
    {
        AddHandlerCore(eventName, new SseEventHandler(handler));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddStronglyTypedHandler<TEventData>(string eventName, Action<SseItem<TEventData>> handler)
    {
        AddHandlerCore(eventName, new SseEventHandler<TEventData>(handler));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDataHandler(string eventName, Action<string> handler)
    {
        AddHandlerCore(eventName, new SseEventDataHandler(handler));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddStronglyTypedDataHandler<TEventData>(string eventName, Action<TEventData> handler)
    {
        AddHandlerCore(eventName, new SseEventDataHandler<TEventData>(handler));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddHandlerCore(string eventName, ISseEventHandler handler)
    {
#if NET8_0_OR_GREATER
        TryAdd(eventName, handler);
#else
        Add(eventName, handler);
#endif
    }
}