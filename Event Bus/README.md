# EventBus

A lightweight, static publish/subscribe event bus for Unity. Decouples senders and receivers with no direct references required.

---

## Overview

`EventBus` is a static class — no instance, no setup, available anywhere. Events are identified by a plain string key. Two variants are supported:

| Variant | Use when |
|---|---|
| `Action` | The event carries no data |
| `Action<object>` | The event carries a payload |

---

## Quick Start

```csharp
// Publisher (no data)
EventBus.Publish("PlayerDied");

// Publisher (with data)
EventBus.Publish("ScoreChanged", newScore);

// Subscriber (no data)
EventBus.Subscribe("PlayerDied", OnPlayerDied);
void OnPlayerDied() { ... }

// Subscriber (with data)
EventBus.Subscribe("ScoreChanged", OnScoreChanged);
void OnScoreChanged(object data)
{
    int score = (int)data;
}

// Unsubscribe (always unsubscribe when the listener is destroyed)
EventBus.Unsubscribe("PlayerDied", OnPlayerDied);
EventBus.Unsubscribe("ScoreChanged", OnScoreChanged);
```

---

## API Reference

### Subscribe

```csharp
EventBus.Subscribe(string key, Action listener);
EventBus.Subscribe(string key, Action<object> listener);
```

Registers `listener` to be called when `key` is published. Multiple listeners can be registered to the same key.

### Unsubscribe

```csharp
EventBus.Unsubscribe(string key, Action listener);
EventBus.Unsubscribe(string key, Action<object> listener);
```

Removes `listener` from the key's invocation list. Safe to call even if the listener was never subscribed.

### Publish

```csharp
EventBus.Publish(string key);               // No data
EventBus.Publish(string key, object data);  // With data
```

Invokes all listeners registered to `key`. No-op if no one is subscribed.

---

## Recommended Patterns

### Define keys as constants

Avoid magic strings scattered across files:

```csharp
public static class GameEvents
{
    public const string PlayerDied    = "PlayerDied";
    public const string ScoreChanged  = "ScoreChanged";
    public const string LevelComplete = "LevelComplete";
}
```

```csharp
EventBus.Publish(GameEvents.PlayerDied);
EventBus.Subscribe(GameEvents.ScoreChanged, OnScoreChanged);
```

### Always unsubscribe on destroy

Failing to unsubscribe causes callbacks to fire on destroyed objects and leaks references:

```csharp
private void OnEnable()  => EventBus.Subscribe(GameEvents.PlayerDied, OnPlayerDied);
private void OnDisable() => EventBus.Unsubscribe(GameEvents.PlayerDied, OnPlayerDied);
```

Using `OnEnable`/`OnDisable` (rather than `Awake`/`OnDestroy`) also handles object deactivation correctly.

### Casting payloads safely

The `Action<object>` variant requires a cast. Use a safe cast pattern to guard against mismatched publishers:

```csharp
void OnScoreChanged(object data)
{
    if (data is int score)
        UpdateUI(score);
    else
        Debug.LogWarning($"[EventBus] Unexpected payload type: {data?.GetType()}");
}
```

---

## Limitations

- **No typed payloads** — the `object` parameter requires manual casting. For type safety, wrap `EventBus` in a typed helper or switch to a generic variant.
- **No event ordering** — listeners are invoked in subscription order, which may vary between runs.
- **No memory of past events** — late subscribers miss events published before they subscribed.
- **Static lifetime** — the bus persists for the full application session. Events subscribed during play mode in the Editor may carry over if the bus isn't cleared between runs. Consider adding a `Clear()` method or reinitialising dictionaries on scene load.

---

## File Reference

| File | Purpose |
|---|---|
| `EventBus.cs` | The entire implementation — drop into any Unity project |
