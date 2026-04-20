// ============================================================
//  SettingsApplier.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Attach to a persistent GameObject in your first/main scene
//  (tick "Don't Destroy On Load" on that object, or call
//  DontDestroyOnLoad yourself).
//
//  Wire the AudioMixer reference in the Inspector for full
//  volume routing. Everything else works without Inspector setup.
//
//  Exposed mixer param names must match the "Exposed Parameters"
//  list in your AudioMixer asset (right-click a parameter → Expose).
// ============================================================

using UnityEngine;
using UnityEngine.Audio;

namespace Core.Settings
{
    /// <summary>
    /// Applies <see cref="SettingsManager.LiveAudio"/>, <see cref="SettingsManager.LiveGraphics"/>
    /// etc. to Unity's built-in systems whenever <see cref="SettingsManager.OnApplied"/> fires.<br/>
    /// Also applies immediately in <c>Awake</c> so the game starts with correct settings.
    /// </summary>
    public class SettingsApplier : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Audio Mixer (optional but recommended)")]
        [Tooltip("Assign your project's Master AudioMixer asset here.")]
        [SerializeField]
        private AudioMixer audioMixer;

        [Tooltip("Name of the exposed Master Volume parameter in the AudioMixer.")] [SerializeField]
        private string masterVolumeParam = "MasterVolume";

        [Tooltip("Name of the exposed Music Volume parameter.")] [SerializeField]
        private string musicVolumeParam = "MusicVolume";

        [Tooltip("Name of the exposed SFX Volume parameter.")] [SerializeField]
        private string sfxVolumeParam = "SFXVolume";

        [Tooltip("Name of the exposed UI Volume parameter.")] [SerializeField]
        private string uiVolumeParam = "UIVolume";

        [Header("URP / HDRP Render Scale (optional)")]
        [Tooltip("When assigned, RenderScale is forwarded to the URP asset via Camera. " +
                 "Leave null if not using a scriptable render pipeline.")]
        [SerializeField]
        private Camera mainCamera;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure settings are loaded before we try to apply them.
            // SettingsManager is lazy-loading so this is safe even if Load()
            // has not been called yet elsewhere.
            ApplyAll();
        }

        private void OnEnable() => SettingsManager.OnApplied += ApplyAll;
        private void OnDisable() => SettingsManager.OnApplied -= ApplyAll;

        // ── Public surface ─────────────────────────────────────────────────────

        /// <summary>Apply every category of live settings to Unity's systems.</summary>
        public void ApplyAll()
        {
            ApplyAudio();
            ApplyGraphics();
            // Gameplay & Accessibility have no engine-level equivalents here;
            // individual gameplay systems should subscribe to OnApplied directly
            // and read from SettingsManager.LiveGameplay / LiveAccessibility.
        }

        // ── Audio ──────────────────────────────────────────────────────────────

        private void ApplyAudio()
        {
            if (audioMixer == null) return;

            var a = SettingsManager.LiveAudio;

            // When MuteAll is on, drive master to -80 dB regardless of the fader
            float effectiveMaster = a.MuteAll ? 0f : a.MasterVolume;

            SetMixerDb(masterVolumeParam, effectiveMaster);
            SetMixerDb(musicVolumeParam, a.MusicVolume);
            SetMixerDb(sfxVolumeParam, a.SFXVolume);
            SetMixerDb(uiVolumeParam, a.UIVolume);
        }

        /// <summary>
        /// Converts a linear 0–1 volume to decibels and writes it to the mixer.
        /// Any value ≤ 0.0001 maps to −80 dB (effectively silent).
        /// </summary>
        private void SetMixerDb(string paramName, float linear)
        {
            float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
            if (!audioMixer.SetFloat(paramName, db))
                Debug.LogWarning($"[SettingsApplier] AudioMixer has no exposed parameter \"{paramName}\". " +
                                 "Right-click the parameter in the mixer and choose 'Expose to script'.");
        }

        // ── Graphics ───────────────────────────────────────────────────────────

        private void ApplyGraphics()
        {
            var g = SettingsManager.LiveGraphics;

            // Quality preset — applyExpensiveChanges=true forces texture reload etc.
            if (g.QualityLevel >= 0 && g.QualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(g.QualityLevel, applyExpensiveChanges: true);

            // VSync (overrides targetFrameRate when enabled)
            QualitySettings.vSyncCount = g.VSyncEnabled ? 1 : 0;

            // Target frame rate (only active when VSync is off)
            Application.targetFrameRate = g.TargetFrameRate;

            // MSAA (0 = off, 2, 4, 8 samples)
            QualitySettings.antiAliasing = g.AntiAliasingLevel;

            // Resolution + fullscreen mode
            ApplyResolution(g);

            // URP render scale — requires UnityEngine.Rendering.Universal
            // Uncomment the block below if you use URP and have the package installed:
            //
            // var urpAsset = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)
            //                GraphicsSettings.renderPipelineAsset;
            // if (urpAsset != null) urpAsset.renderScale = g.RenderScale;
        }

        private static void ApplyResolution(GraphicsSettings g)
        {
            var mode = g.Fullscreen
                ? FullScreenMode.ExclusiveFullScreen
                : FullScreenMode.Windowed;

            if (g.ResolutionIndex >= 0)
            {
                var resolutions = Screen.resolutions;
                if (g.ResolutionIndex < resolutions.Length)
                {
                    var res = resolutions[g.ResolutionIndex];
                    Screen.SetResolution(res.width, res.height, mode);
                    return;
                }

                Debug.LogWarning($"[SettingsApplier] ResolutionIndex {g.ResolutionIndex} is out of range " +
                                 $"(Screen has {Screen.resolutions.Length} entries). Skipping resolution change.");
            }
            else
            {
                // -1 = no resolution preference, just toggle fullscreen
                Screen.fullScreenMode = mode;
            }
        }
    }
}