using System.Collections;
using UnityEngine;

namespace ObjectPooling
{
    /// <summary>
    /// Internal MonoBehaviour used to schedule a delayed return-to-pool.
    /// Stays alive across scenes via DontDestroyOnLoad.
    /// </summary>
    internal class PoolDelayedDestroy : MonoBehaviour
    {
        private static PoolDelayedDestroy _instance;

        private static PoolDelayedDestroy Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[Pool Delayed Destroy]");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<PoolDelayedDestroy>();
                return _instance;
            }
        }

        internal static void Schedule(GameObject target, float delay)
        {
            Instance.StartCoroutine(Instance.ReturnAfterDelay(target, delay));
        }

        private IEnumerator ReturnAfterDelay(GameObject target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (target != null)
                Pool.Destroy(target);
        }
    }
}
