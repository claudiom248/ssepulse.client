using System.Net.ServerSentEvents;
using System.Text.Json;
using SsePulse.Client.EventHandlers;

namespace SsePulse.Client.Core.Internal;

internal class SseHandlersDictionary : Dictionary<string, List<ISseEventHandler>>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public SseHandlersDictionary(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }
    
    public void AddHandler(string eventName, Action<SseItem<string>> handler)
    {
        AddHandlerCore(eventName, new SseEventHandler(handler));
    }
    
    public void AddStronglyTypedHandler<TEventData>(string eventName, Action<SseItem<TEventData>> handler)
    {
        AddHandlerCore(eventName, new SseEventHandler<TEventData>(handler, _jsonSerializerOptions));
    }

    public void AddDataHandler(string eventName, Action<string> handler)
    {
        AddHandlerCore(eventName, new SseEventDataHandler(handler));
    }

    public void AddStronglyTypedDataHandler<TEventData>(string eventName, Action<TEventData> handler)
    {
        AddHandlerCore(eventName, new SseEventDataHandler<TEventData>(handler, _jsonSerializerOptions));
    }

    private void AddHandlerCore(string eventName, ISseEventHandler handler)
    {
        if (!TryGetValue(eventName, out List<ISseEventHandler>? handlers))
        {
            handlers =
            [
                handler
            ];
            Add(eventName, handlers);
            return;
        }
        handlers.Add(handler);
    }
}