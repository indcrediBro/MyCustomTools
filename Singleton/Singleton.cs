using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting;
    private static bool _initialized;

    public static bool HasInstance => _instance != null;

    public static T Instance
    {
        get
        {
            if (_isQuitting)
                return null;

            if (_instance == null && !_initialized)
            {
                Initialize();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Explicit initialization (optional but recommended for control)
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

        if (_instance == null)
        {
            var go = new GameObject($"[Singleton] {typeof(T).Name}");
            _instance = go.AddComponent<T>();
        }

        if (_instance is Singleton<T> singleton)
        {
            singleton.InternalInit();
        }
    }

    private void InternalInit()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (PersistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        OnInitialized();
    }

    /// <summary>
    /// Override this instead of Awake
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// Override if you DON'T want persistence
    /// </summary>
    protected virtual bool PersistAcrossScenes => true;

    protected virtual void Awake()
    {
        if (!_initialized)
        {
            _initialized = true;
            InternalInit();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _initialized = false;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}