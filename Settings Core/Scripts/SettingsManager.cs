// ============================================================
//  SettingsManager.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Static class — no MonoBehaviour needed, no scene setup.
//  First access auto-loads from GamePrefs (slot 0 by default).
//
//  USAGE:
//    float vol = SettingsManager.GetFloat(FloatSetting.Audio_MasterVolume);
//    SettingsManager.SetFloat(FloatSetting.Audio_MasterVolume, 0.8f);
//    SettingsManager.Apply();   // commit + save
//    SettingsManager.Revert();  // discard pending changes
// ============================================================

using System;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Central, static settings manager backed by <see cref="GamePrefs"/>.<br/>
    /// Implements a <b>Pending / Live</b> pattern:<br/>
    /// • <b>Pending</b> — what the user is currently editing in the UI.<br/>
    /// • <b>Live</b>    — the last applied &amp; saved snapshot.<br/><br/>
    /// All values are addressed with typed enums (<see cref="FloatSetting"/>,
    /// <see cref="IntSetting"/>, <see cref="BoolSetting"/>, <see cref="StringSetting"/>)
    /// so incorrect usage is caught at compile time.
    /// </summary>
    public static class SettingsManager
    {
        // ── GamePrefs keys ─────────────────────────────────────────────────────
        private const string KeyAudio = "Settings.Audio";
        private const string KeyGraphics = "Settings.Graphics";
        private const string KeyGameplay = "Settings.Gameplay";
        private const string KeyAccessibility = "Settings.Accessibility";

        // ── State ──────────────────────────────────────────────────────────────
        private static AudioSettings _liveAudio;
        private static GraphicsSettings _liveGraphics;
        private static GameplaySettings _liveGameplay;
        private static AccessibilitySettings _liveAccessibility;

        private static AudioSettings _pendingAudio;
        private static GraphicsSettings _pendingGraphics;
        private static GameplaySettings _pendingGameplay;
        private static AccessibilitySettings _pendingAccessibility;

        private static bool _initialised;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired after <see cref="Apply"/> completes.</summary>
        public static event Action OnApplied;

        /// <summary>
        /// Fired after <see cref="Revert"/> or <see cref="ResetToDefaults"/>.
        /// UI binders refresh their values in response to this event.
        /// </summary>
        public static event Action OnReverted;

        /// <summary>
        /// Fired whenever a pending value changes via <c>Set*</c>.<br/>
        /// The argument is the enum value that changed, boxed as <c>object</c>.
        /// Binders cast it to their own enum type before comparing:
        /// <code>
        /// SettingsManager.OnPendingChanged += e => {
        ///     if (e is FloatSetting f &amp;&amp; f == FloatSetting.Audio_MasterVolume)
        ///         PullFromManager();
        /// };
        /// </code>
        /// </summary>
        public static event Action<object> OnPendingChanged;

        // ── Live snapshots ─────────────────────────────────────────────────────

        /// <summary>The last applied and saved audio settings.</summary>
        public static AudioSettings LiveAudio
        {
            get
            {
                EnsureInit();
                return _liveAudio;
            }
        }

        /// <summary>The last applied and saved graphics settings.</summary>
        public static GraphicsSettings LiveGraphics
        {
            get
            {
                EnsureInit();
                return _liveGraphics;
            }
        }

        /// <summary>The last applied and saved gameplay settings.</summary>
        public static GameplaySettings LiveGameplay
        {
            get
            {
                EnsureInit();
                return _liveGameplay;
            }
        }

        /// <summary>The last applied and saved accessibility settings.</summary>
        public static AccessibilitySettings LiveAccessibility
        {
            get
            {
                EnsureInit();
                return _liveAccessibility;
            }
        }

        // ── Pending snapshots ──────────────────────────────────────────────────

        /// <summary>Pending audio settings (may differ from Live until Apply is called).</summary>
        public static AudioSettings PendingAudio
        {
            get
            {
                EnsureInit();
                return _pendingAudio;
            }
        }

        /// <summary>Pending graphics settings.</summary>
        public static GraphicsSettings PendingGraphics
        {
            get
            {
                EnsureInit();
                return _pendingGraphics;
            }
        }

        /// <summary>Pending gameplay settings.</summary>
        public static GameplaySettings PendingGameplay
        {
            get
            {
                EnsureInit();
                return _pendingGameplay;
            }
        }

        /// <summary>Pending accessibility settings.</summary>
        public static AccessibilitySettings PendingAccessibility
        {
            get
            {
                EnsureInit();
                return _pendingAccessibility;
            }
        }

        /// <summary>True if there are pending changes that have not yet been applied.</summary>
        public static bool HasPendingChanges { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Load settings from disk. Called automatically on first access.</summary>
        public static void Load()
        {
            _liveAudio = GamePrefs.GetObject(KeyAudio, new AudioSettings());
            _liveGraphics = GamePrefs.GetObject(KeyGraphics, new GraphicsSettings());
            _liveGameplay = GamePrefs.GetObject(KeyGameplay, new GameplaySettings());
            _liveAccessibility = GamePrefs.GetObject(KeyAccessibility, new AccessibilitySettings());

            _pendingAudio = _liveAudio.Clone();
            _pendingGraphics = _liveGraphics.Clone();
            _pendingGameplay = _liveGameplay.Clone();
            _pendingAccessibility = _liveAccessibility.Clone();

            HasPendingChanges = false;
            _initialised = true;
        }

        /// <summary>Commit pending settings → Live → disk. Fires <see cref="OnApplied"/>.</summary>
        public static void Apply()
        {
            EnsureInit();

            _liveAudio.CopyFrom(_pendingAudio);
            _liveGraphics.CopyFrom(_pendingGraphics);
            _liveGameplay.CopyFrom(_pendingGameplay);
            _liveAccessibility.CopyFrom(_pendingAccessibility);

            GamePrefs.SetObject(KeyAudio, _liveAudio);
            GamePrefs.SetObject(KeyGraphics, _liveGraphics);
            GamePrefs.SetObject(KeyGameplay, _liveGameplay);
            GamePrefs.SetObject(KeyAccessibility, _liveAccessibility);
            GamePrefs.Save();

            HasPendingChanges = false;
            OnApplied?.Invoke();
        }

        /// <summary>Discard pending changes. Fires <see cref="OnReverted"/>.</summary>
        public static void Revert()
        {
            EnsureInit();

            _pendingAudio.CopyFrom(_liveAudio);
            _pendingGraphics.CopyFrom(_liveGraphics);
            _pendingGameplay.CopyFrom(_liveGameplay);
            _pendingAccessibility.CopyFrom(_liveAccessibility);

            HasPendingChanges = false;
            OnReverted?.Invoke();
        }

        /// <summary>
        /// Reset all pending settings to factory defaults.
        /// Does NOT save — call <see cref="Apply"/> to persist.
        /// Fires <see cref="OnReverted"/> so UI binders refresh.
        /// </summary>
        public static void ResetToDefaults()
        {
            EnsureInit();

            _pendingAudio = new AudioSettings();
            _pendingGraphics = new GraphicsSettings();
            _pendingGameplay = new GameplaySettings();
            _pendingAccessibility = new AccessibilitySettings();

            HasPendingChanges = true;
            OnReverted?.Invoke();
        }

        // ── float ──────────────────────────────────────────────────────────────

        public static float GetFloat(FloatSetting setting, float defaultValue = 0f)
        {
            EnsureInit();
            return setting switch
            {
                FloatSetting.Audio_MasterVolume => _pendingAudio.MasterVolume,
                FloatSetting.Audio_MusicVolume => _pendingAudio.MusicVolume,
                FloatSetting.Audio_SFXVolume => _pendingAudio.SFXVolume,
                FloatSetting.Audio_UIVolume => _pendingAudio.UIVolume,
                FloatSetting.Graphics_RenderScale => _pendingGraphics.RenderScale,
                FloatSetting.Gameplay_MouseSensitivity => _pendingGameplay.MouseSensitivity,
                FloatSetting.Gameplay_UIScale => _pendingGameplay.UIScale,
                FloatSetting.Accessibility_TextScale => _pendingAccessibility.TextScale,
                FloatSetting.Accessibility_SubtitleSize => _pendingAccessibility.SubtitleSize,
                FloatSetting.Accessibility_Contrast => _pendingAccessibility.Contrast,
                _ => defaultValue,
            };
        }

        public static void SetFloat(FloatSetting setting, float value)
        {
            EnsureInit();
            switch (setting)
            {
                case FloatSetting.Audio_MasterVolume: _pendingAudio.MasterVolume = value; break;
                case FloatSetting.Audio_MusicVolume: _pendingAudio.MusicVolume = value; break;
                case FloatSetting.Audio_SFXVolume: _pendingAudio.SFXVolume = value; break;
                case FloatSetting.Audio_UIVolume: _pendingAudio.UIVolume = value; break;
                case FloatSetting.Graphics_RenderScale: _pendingGraphics.RenderScale = value; break;
                case FloatSetting.Gameplay_MouseSensitivity: _pendingGameplay.MouseSensitivity = value; break;
                case FloatSetting.Gameplay_UIScale: _pendingGameplay.UIScale = value; break;
                case FloatSetting.Accessibility_TextScale: _pendingAccessibility.TextScale = value; break;
                case FloatSetting.Accessibility_SubtitleSize: _pendingAccessibility.SubtitleSize = value; break;
                case FloatSetting.Accessibility_Contrast: _pendingAccessibility.Contrast = value; break;
                default:
                    Debug.LogWarning($"[SettingsManager] Unhandled FloatSetting: {setting}");
                    return;
            }

            HasPendingChanges = true;
            OnPendingChanged?.Invoke(setting);
        }

        // ── int ────────────────────────────────────────────────────────────────

        public static int GetInt(IntSetting setting, int defaultValue = 0)
        {
            EnsureInit();
            return setting switch
            {
                IntSetting.Graphics_QualityLevel => _pendingGraphics.QualityLevel,
                IntSetting.Graphics_ResolutionIndex => _pendingGraphics.ResolutionIndex,
                IntSetting.Graphics_TargetFrameRate => _pendingGraphics.TargetFrameRate,
                IntSetting.Graphics_AntiAliasingLevel => _pendingGraphics.AntiAliasingLevel,
                IntSetting.Gameplay_Difficulty => _pendingGameplay.Difficulty,
                IntSetting.Accessibility_ColorblindType => _pendingAccessibility.ColorblindType,
                _ => defaultValue,
            };
        }

        public static void SetInt(IntSetting setting, int value)
        {
            EnsureInit();
            switch (setting)
            {
                case IntSetting.Graphics_QualityLevel: _pendingGraphics.QualityLevel = value; break;
                case IntSetting.Graphics_ResolutionIndex: _pendingGraphics.ResolutionIndex = value; break;
                case IntSetting.Graphics_TargetFrameRate: _pendingGraphics.TargetFrameRate = value; break;
                case IntSetting.Graphics_AntiAliasingLevel: _pendingGraphics.AntiAliasingLevel = value; break;
                case IntSetting.Gameplay_Difficulty: _pendingGameplay.Difficulty = value; break;
                case IntSetting.Accessibility_ColorblindType: _pendingAccessibility.ColorblindType = value; break;
                default:
                    Debug.LogWarning($"[SettingsManager] Unhandled IntSetting: {setting}");
                    return;
            }

            HasPendingChanges = true;
            OnPendingChanged?.Invoke(setting);
        }

        // ── bool ───────────────────────────────────────────────────────────────

        public static bool GetBool(BoolSetting setting, bool defaultValue = false)
        {
            EnsureInit();
            return setting switch
            {
                BoolSetting.Audio_MuteAll => _pendingAudio.MuteAll,
                BoolSetting.Graphics_Fullscreen => _pendingGraphics.Fullscreen,
                BoolSetting.Graphics_VSyncEnabled => _pendingGraphics.VSyncEnabled,
                BoolSetting.Graphics_Bloom => _pendingGraphics.Bloom,
                BoolSetting.Graphics_MotionBlur => _pendingGraphics.MotionBlur,
                BoolSetting.Graphics_AmbientOcclusion => _pendingGraphics.AmbientOcclusion,
                BoolSetting.Gameplay_InvertY => _pendingGameplay.InvertY,
                BoolSetting.Gameplay_ShowTutorials => _pendingGameplay.ShowTutorials,
                BoolSetting.Gameplay_ShowDamageNumbers => _pendingGameplay.ShowDamageNumbers,
                BoolSetting.Gameplay_ScreenShake => _pendingGameplay.ScreenShake,
                BoolSetting.Accessibility_ColorblindMode => _pendingAccessibility.ColorblindMode,
                BoolSetting.Accessibility_ReducedMotion => _pendingAccessibility.ReducedMotion,
                BoolSetting.Accessibility_LargeText => _pendingAccessibility.LargeText,
                BoolSetting.Accessibility_SubtitlesEnabled => _pendingAccessibility.SubtitlesEnabled,
                BoolSetting.Accessibility_HighContrastUI => _pendingAccessibility.HighContrastUI,
                _ => defaultValue,
            };
        }

        public static void SetBool(BoolSetting setting, bool value)
        {
            EnsureInit();
            switch (setting)
            {
                case BoolSetting.Audio_MuteAll: _pendingAudio.MuteAll = value; break;
                case BoolSetting.Graphics_Fullscreen: _pendingGraphics.Fullscreen = value; break;
                case BoolSetting.Graphics_VSyncEnabled: _pendingGraphics.VSyncEnabled = value; break;
                case BoolSetting.Graphics_Bloom: _pendingGraphics.Bloom = value; break;
                case BoolSetting.Graphics_MotionBlur: _pendingGraphics.MotionBlur = value; break;
                case BoolSetting.Graphics_AmbientOcclusion: _pendingGraphics.AmbientOcclusion = value; break;
                case BoolSetting.Gameplay_InvertY: _pendingGameplay.InvertY = value; break;
                case BoolSetting.Gameplay_ShowTutorials: _pendingGameplay.ShowTutorials = value; break;
                case BoolSetting.Gameplay_ShowDamageNumbers: _pendingGameplay.ShowDamageNumbers = value; break;
                case BoolSetting.Gameplay_ScreenShake: _pendingGameplay.ScreenShake = value; break;
                case BoolSetting.Accessibility_ColorblindMode: _pendingAccessibility.ColorblindMode = value; break;
                case BoolSetting.Accessibility_ReducedMotion: _pendingAccessibility.ReducedMotion = value; break;
                case BoolSetting.Accessibility_LargeText: _pendingAccessibility.LargeText = value; break;
                case BoolSetting.Accessibility_SubtitlesEnabled: _pendingAccessibility.SubtitlesEnabled = value; break;
                case BoolSetting.Accessibility_HighContrastUI: _pendingAccessibility.HighContrastUI = value; break;
                default:
                    Debug.LogWarning($"[SettingsManager] Unhandled BoolSetting: {setting}");
                    return;
            }

            HasPendingChanges = true;
            OnPendingChanged?.Invoke(setting);
        }

        // ── string ─────────────────────────────────────────────────────────────

        public static string GetString(StringSetting setting, string defaultValue = "")
        {
            EnsureInit();
            return setting switch
            {
                StringSetting.Gameplay_Language => _pendingGameplay.Language,
                _ => defaultValue,
            };
        }

        public static void SetString(StringSetting setting, string value)
        {
            EnsureInit();
            switch (setting)
            {
                case StringSetting.Gameplay_Language: _pendingGameplay.Language = value; break;
                default:
                    Debug.LogWarning($"[SettingsManager] Unhandled StringSetting: {setting}");
                    return;
            }

            HasPendingChanges = true;
            OnPendingChanged?.Invoke(setting);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private static void EnsureInit()
        {
            if (!_initialised) Load();
        }
    }
}