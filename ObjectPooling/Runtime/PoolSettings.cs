using UnityEngine;

namespace ObjectPooling
{
    /// <summary>
    /// Global settings for all pools.
    /// Create one via:  Assets > Create > Object Pooling > Pool Settings
    /// If none exists, sensible defaults are used automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "Object Pooling/Pool Settings", fileName = "PoolSettings")]
    public class PoolSettings : ScriptableObject
    {
        private const string ResourcePath = "PoolSettings";

        [Header("Pool Sizing")]
        [Tooltip("Initial capacity allocated per pool (no allocations up to this count).")]
        [Min(1)] public int DefaultCapacity = 10;

        [Tooltip("Maximum number of inactive objects kept alive per pool. Excess are destroyed.")]
        [Min(1)] public int MaxPoolSize = 100;

        [Header("Safety")]
        [Tooltip("Throw an error if the same instance is returned to the pool twice.")]
        public bool CollectionCheck = true;

        [Tooltip("Log a warning when Pool.Destroy is called on an object not spawned by Pool.Instantiate.")]
        public bool WarnOnUnknownDestroy = true;

        [Header("Container")]
        [Tooltip("Keep inactive pooled objects under a dedicated scene GameObject for a tidy hierarchy.")]
        public bool UsePoolContainer = true;

        // ── Internal factory ─────────────────────────────────────────────────

        private static PoolSettings _default;

        internal static PoolSettings GetOrCreateDefault()
        {
            if (_default != null) return _default;

            _default = Resources.Load<PoolSettings>(ResourcePath);
            if (_default != null) return _default;

            // Nothing found — create a transient in-memory default.
            _default = CreateInstance<PoolSettings>();
            _default.name = "PoolSettings (default)";
            return _default;
        }
    }
}
