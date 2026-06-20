using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ObjectPooling
{
    /// <summary>
    /// Drop-in replacement for Instantiate/Destroy using Unity's built-in ObjectPool.
    /// No setup required — pools are created automatically on first use.
    ///
    /// Usage:
    ///   // Instead of: var obj = Instantiate(prefab, pos, rot);
    ///   var obj = Pool.Instantiate(prefab, pos, rot);
    ///
    ///   // Instead of: Destroy(obj);
    ///   Pool.Destroy(obj);
    /// </summary>
    public static class Pool
    {
        // ── Internal registry ────────────────────────────────────────────────
        private static readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools
            = new Dictionary<GameObject, ObjectPool<GameObject>>();

        // Maps a live instance back to its source prefab so Destroy works correctly.
        private static readonly Dictionary<GameObject, GameObject> _instanceToPrefab
            = new Dictionary<GameObject, GameObject>();

        // ── Global defaults (tweak in code or via PoolSettings asset) ────────
        private static PoolSettings _settings;

        private static PoolSettings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = PoolSettings.GetOrCreateDefault();
                return _settings;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Retrieve a pooled instance. Mirrors Instantiate(prefab).</summary>
        public static GameObject Instantiate(GameObject prefab)
            => Instantiate(prefab, Vector3.zero, Quaternion.identity, null);

        /// <summary>Retrieve a pooled instance. Mirrors Instantiate(prefab, parent).</summary>
        public static GameObject Instantiate(GameObject prefab, Transform parent)
            => Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);

        /// <summary>Retrieve a pooled instance. Mirrors Instantiate(prefab, pos, rot).</summary>
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
            => Instantiate(prefab, position, rotation, null);

        /// <summary>Retrieve a pooled instance. Full signature mirrors Instantiate.</summary>
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogError("[Pool] Prefab is null.");
                return null;
            }

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            return instance;
        }

        /// <summary>Retrieve a pooled instance with a typed component returned.</summary>
        public static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("[Pool] Prefab component is null.");
                return null;
            }

            var go = Instantiate(prefab.gameObject, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>Return an instance to its pool. Mirrors Destroy(obj).</summary>
        public static void Destroy(GameObject instance)
        {
            if (instance == null) return;

            if (_instanceToPrefab.TryGetValue(instance, out var prefab) &&
                _pools.TryGetValue(prefab, out var pool))
            {
                pool.Release(instance);
            }
            else
            {
                // Fallback: not a pooled object — destroy normally.
                if (Settings.WarnOnUnknownDestroy)
                    Debug.LogWarning($"[Pool] '{instance.name}' was not spawned by Pool.Instantiate. Falling back to Object.Destroy.");
                Object.Destroy(instance);
            }
        }

        /// <summary>Return an instance to its pool via a component reference.</summary>
        public static void Destroy<T>(T component) where T : Component
        {
            if (component != null)
                Destroy(component.gameObject);
        }

        /// <summary>Return an instance to its pool after a delay (like Destroy with t).</summary>
        public static void Destroy(GameObject instance, float delay)
        {
            if (instance == null) return;
            PoolDelayedDestroy.Schedule(instance, delay);
        }

        /// <summary>Pre-warm a pool for a prefab so the first burst of spawns is free.</summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;

            var pool = GetOrCreatePool(prefab);
            var buffer = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
                buffer.Add(pool.Get());

            foreach (var go in buffer)
                pool.Release(go);
        }

        /// <summary>Destroy all pooled instances and remove the pool for a prefab.</summary>
        public static void ClearPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Clear();
                pool.Dispose();
                _pools.Remove(prefab);
            }
        }

        /// <summary>Destroy every pool. Call on scene unload if needed.</summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
                pool.Dispose();
            }
            _pools.Clear();
            _instanceToPrefab.Clear();
        }

        /// <summary>Returns stats for a prefab's pool (active / inactive counts).</summary>
        public static (int active, int inactive) GetStats(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
                return (pool.CountActive, pool.CountInactive);
            return (0, 0);
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private static ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var existing))
                return existing;

            var s = Settings;
            var capturedPrefab = prefab; // closure-safe

            var pool = new ObjectPool<GameObject>(
                createFunc:   () => CreateInstance(capturedPrefab),
                actionOnGet:  OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: s.CollectionCheck,
                defaultCapacity: s.DefaultCapacity,
                maxSize:         s.MaxPoolSize
            );

            _pools[prefab] = pool;
            return pool;
        }

        private static GameObject CreateInstance(GameObject prefab)
        {
            var go = Object.Instantiate(prefab);
            go.name = prefab.name; // strip "(Clone)" suffix
            _instanceToPrefab[go] = prefab;
            go.SetActive(false);
            return go;
        }

        private static void OnGet(GameObject go)
        {
            // SetActive(true) and position are applied by the caller.
            // Fire IPoolable callbacks on all root components.
            foreach (var p in go.GetComponents<IPoolable>())
                p.OnSpawn();
        }

        private static void OnRelease(GameObject go)
        {
            foreach (var p in go.GetComponents<IPoolable>())
                p.OnDespawn();

            go.SetActive(false);
            go.transform.SetParent(PoolContainer.Root);
        }

        private static void OnPoolDestroy(GameObject go)
        {
            _instanceToPrefab.Remove(go);
            Object.Destroy(go);
        }
    }
}
