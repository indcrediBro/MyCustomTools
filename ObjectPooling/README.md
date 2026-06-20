# Object Pooling — Unity 6.5 Toolkit

Zero-setup, drop-in replacement for `Instantiate` / `Destroy` built on Unity's
own `UnityEngine.Pool.ObjectPool<T>`.

---

## Installation

1. Copy the `ObjectPooling/` folder anywhere inside your project's `Assets/`.
2. That's it. No scenes to configure, no Manager GameObjects to place.

---

## Quick Start

```csharp
using ObjectPooling;

// Spawn  — identical signature to Instantiate
var bullet = Pool.Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

// Despawn — identical signature to Destroy (including optional delay)
Pool.Destroy(bullet);
Pool.Destroy(bullet, 3f);   // returns to pool after 3 seconds

// Generic overload — returns a component directly (no GetComponent needed)
var ps = Pool.Instantiate(particlePrefab, pos, Quaternion.identity);
```

No pool configuration required. Pools are created automatically the first time
a prefab is passed to `Pool.Instantiate`.

---

## API Reference

| Method | Description |
|--------|-------------|
| `Pool.Instantiate(prefab)` | Spawn at world origin |
| `Pool.Instantiate(prefab, pos, rot)` | Spawn at position & rotation |
| `Pool.Instantiate(prefab, pos, rot, parent)` | Spawn under parent Transform |
| `Pool.Instantiate<T>(component, pos, rot, parent)` | Spawn & return component |
| `Pool.Destroy(go)` | Return to pool immediately |
| `Pool.Destroy(go, delay)` | Return to pool after N seconds |
| `Pool.Prewarm(prefab, count)` | Pre-allocate objects to avoid GC spikes |
| `Pool.ClearPool(prefab)` | Destroy all instances for one prefab |
| `Pool.ClearAll()` | Destroy every pool |
| `Pool.GetStats(prefab)` | Returns `(int active, int inactive)` |

---

## Optional: IPoolable Callbacks

Implement `IPoolable` on any MonoBehaviour to receive lifecycle callbacks:

```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()   { /* reset velocity, enable collider, etc. */ }
    public void OnDespawn() { /* stop coroutines, clear trails, etc.  */ }
}
```

`OnSpawn` fires when retrieved from the pool.  
`OnDespawn` fires before being returned to the pool.

---

## Optional: PoolSettings

Create a tuning asset via **Assets > Create > Object Pooling > Pool Settings**
and save it anywhere inside a `Resources/` folder named `PoolSettings`.

| Setting | Default | Description |
|---------|---------|-------------|
| Default Capacity | 10 | Initial per-pool allocation |
| Max Pool Size | 100 | Excess objects are destroyed |
| Collection Check | true | Throws if same instance released twice |
| Warn On Unknown Destroy | true | Logs when a non-pooled object is destroyed |
| Use Pool Container | true | Groups inactive objects under `[Pool Container]` |

---

## Pool Debugger (Editor)

Open **Window > Object Pooling > Pool Debugger** in Play Mode to see:

- Live active / inactive counts per prefab
- A visual fill bar showing utilisation
- Per-pool or global Clear buttons

---

## File Structure

```
ObjectPooling/
├── Runtime/
│   ├── Pool.cs                    ← main API
│   ├── PoolSettings.cs            ← ScriptableObject config
│   ├── PoolContainer.cs           ← hierarchy container
│   ├── PoolDelayedDestroy.cs      ← delayed return helper
│   ├── IPoolable.cs               ← optional lifecycle interface
│   └── ObjectPooling.Runtime.asmdef
├── Editor/
│   ├── PoolDebuggerWindow.cs      ← editor window
│   └── ObjectPooling.Editor.asmdef
└── Examples.cs                    ← usage examples (safe to delete)
```

---

## Notes

- Built on `UnityEngine.Pool.ObjectPool<T>` (Unity 2021+, fully supported in Unity 6.5).
- Inactive objects are parked under a `DontDestroyOnLoad` container so they
  survive scene transitions.
- `Pool.Destroy` falls back to `Object.Destroy` for objects not spawned by
  this toolkit, so mixed codebases work safely.
