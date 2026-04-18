// ============================================================
//  GamePrefsAutoSave.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Self-bootstrapping MonoBehaviour. Do NOT add to any scene.
//  Activates automatically when GamePrefsSettings.UseAutoSave = true.
// ============================================================

using Unity.Burst.CompilerServices;
using UnityEngine;
#pragma warning disable CS0162
/// <summary>
/// Hooks into application lifecycle events to auto-save <see cref="GamePrefs"/>.<br/>
/// Bootstraps itself at game start — no scene setup required.
/// Enable/disable via <see cref="GamePrefsSettings.UseAutoSave"/>.
/// </summary>
[AddComponentMenu("")] // hidden from Add Component menu
internal sealed class GamePrefsAutoSave : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (!GamePrefsSettings.UseAutoSave) return;

        var go = new GameObject("[GamePrefsAutoSave]")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(go);
        go.AddComponent<GamePrefsAutoSave>();
        if(GamePrefsSettings.DEBUG)Debug.Log("[GamePrefs] Auto-save active.");
    }

    private void OnApplicationQuit()
    {
        if (!GamePrefs.IsDirty) return;
        if(GamePrefsSettings.DEBUG)Debug.Log("[GamePrefs] Auto-saving on application quit…");
        GamePrefs.Save();
    }

    /// <summary>Also triggers on mobile when the app is sent to the background.</summary>
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && GamePrefs.IsDirty)
        {
            if(GamePrefsSettings.DEBUG)Debug.Log("[GamePrefs] Auto-saving on application pause…");
            GamePrefs.Save();
        }
    }
}
#pragma warning restore CS0162