using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObjectPooling
{
    /// <summary>
    /// Provides a persistent scene container Transform for inactive pooled objects,
    /// keeping the Hierarchy window tidy.
    /// </summary>
    internal static class PoolContainer
    {
        private static Transform _root;

        internal static Transform Root
        {
            get
            {
                // Return null if the settings say we don't want a container.
                var settings = PoolSettings.GetOrCreateDefault();
                if (!settings.UsePoolContainer) return null;

                if (_root != null) return _root;

                var go = new GameObject("[Pool Container]");
                Object.DontDestroyOnLoad(go);
                _root = go.transform;
                return _root;
            }
        }
    }
}
