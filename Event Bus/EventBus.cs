using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<string, Action>         _events         = new();
    private static readonly Dictionary<string, Action<object>> _eventsWithData = new();

    public static void Subscribe(string key, Action listener)
    {
        if (!_events.ContainsKey(key)) _events[key] = null;
        _events[key] += listener;
    }

    public static void Subscribe(string key, Action<object> listener)
    {
        if (!_eventsWithData.ContainsKey(key)) _eventsWithData[key] = null;
        _eventsWithData[key] += listener;
    }

    public static void Unsubscribe(string key, Action listener)
    {
        if (_events.ContainsKey(key)) _events[key] -= listener;
    }

    public static void Unsubscribe(string key, Action<object> listener)
    {
        if (_eventsWithData.ContainsKey(key)) _eventsWithData[key] -= listener;
    }

    public static void Publish(string key)
    {
        if (_events.ContainsKey(key)) _events[key]?.Invoke();
    }

    public static void Publish(string key, object data)
    {
        if (_eventsWithData.ContainsKey(key)) _eventsWithData[key]?.Invoke(data);
    }
}