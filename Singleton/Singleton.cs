using UnityEngine;

[DefaultExecutionOrder(-10)]
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Singleton Settings")]
    [SerializeField] private bool m_dontDestroyOnLoad;

    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogWarning($"[Singleton] {typeof(T)} instance is null.");
            return _instance;
        }
        private set => _instance = value;
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate {typeof(T)} on '{gameObject.name}'. Destroying.");
            Destroy(gameObject);
            return;
        }

        if (m_dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
