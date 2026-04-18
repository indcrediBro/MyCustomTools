# Unity Generic Singleton

A reusable, production-ready Singleton base class for Unity projects. Designed to be safe, efficient, and flexible while avoiding common pitfalls like duplicate instances, initialization order issues, and editor-related bugs.

---

## ✨ Features

- Lazy initialization (auto-creates if missing)
- Optional manual initialization for better control
- Prevents duplicate instances
- Supports inactive objects in scene
- Safe with scene reloads and domain reloads
- Optional persistence across scenes
- Clean override pattern (no messy `Awake()` usage)
- Minimal overhead (no unnecessary locking or repeated searches)

---

## 📦 Installation

1. Copy the `Singleton<T>` script into your project.
2. Inherit from it for any global system you need.

---

## 🚀 Usage

### Create a Singleton

```csharp
public class GameManager : Singleton<GameManager>
{
    public int Score { get; private set; }

    protected override void OnInitialized()
    {
        Debug.Log("GameManager Initialized");
    }

    public void AddScore(int value)
    {
        Score += value;
    }
}
```

---

### Access Anywhere

```csharp
GameManager.Instance.AddScore(10);
```

---

### Optional: Manual Initialization (Recommended)

```csharp
GameManager.Initialize();
AudioManager.Initialize();
```

This ensures controlled initialization order and avoids hidden dependencies.

---

## ⚙️ Configuration

### Disable Persistence Across Scenes

```csharp
protected override bool PersistAcrossScenes => false;
```

---

### Check If Instance Exists

```csharp
if (GameManager.HasInstance)
{
    // Safe to use
}
```

---

## ⚠️ Best Practices

Use singletons **only for global systems**, such as:
- Game Manager
- Audio Manager
- Save System
- Configuration Services

Avoid using them for:
- Gameplay logic
- Temporary systems
- Anything that should be modular or testable

---

## ❗ Common Pitfalls Avoided

- Duplicate instances on scene load
- Hidden initialization order bugs
- Accessing destroyed instances on quit
- Missing references causing null crashes
- Inactive objects not being detected

---

## 🧠 Design Notes

This implementation prioritizes:
- Predictability over magic
- Explicit control over lifecycle
- Compatibility with scalable architectures

---
