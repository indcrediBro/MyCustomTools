# Unity Singleton Base

A minimal, scene-safe generic singleton base for Unity.

## What it does
- Ensures only one instance exists
- Destroys duplicates automatically
- Optional persistence across scenes (`DontDestroyOnLoad`)
- Early execution via `DefaultExecutionOrder(-10)`

## Usage

Inherit from the base class:

```csharp
public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
        // your init
    }
}
```

Access it anywhere:

```csharp
GameManager.Instance.DoSomething();
```

## Inspector

- **Dont Destroy On Load**  
  Enable to keep the instance across scene loads.

## Notes

- Does not auto-create instances. You must place it in the scene.
- Accessing `Instance` before initialization will return null and log a warning.
- Avoid overusing singletons; keep them for global systems only.
