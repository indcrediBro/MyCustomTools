// ============================================================
//  SettingPaths.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  One enum per value type — enforces correct usage at compile
//  time. You cannot accidentally pass a BoolSetting to SetFloat.
// ============================================================

namespace Core.Settings
{
    /// <summary>All float settings accessible through <see cref="SettingsManager"/>.</summary>
    public enum FloatSetting
    {
        // ── Audio ───────────────────────────────────────────────────────────────
        Audio_MasterVolume,
        Audio_MusicVolume,
        Audio_SFXVolume,
        Audio_UIVolume,

        // ── Graphics ────────────────────────────────────────────────────────────
        Graphics_RenderScale,

        // ── Gameplay ────────────────────────────────────────────────────────────
        Gameplay_MouseSensitivity,
        Gameplay_UIScale,

        // ── Accessibility ────────────────────────────────────────────────────────
        Accessibility_TextScale,
        Accessibility_SubtitleSize,
        Accessibility_Contrast,
    }

    /// <summary>All integer settings accessible through <see cref="SettingsManager"/>.</summary>
    public enum IntSetting
    {
        // ── Graphics ─────────────────────────────────────────────────────────────
        Graphics_QualityLevel,
        Graphics_ResolutionIndex,
        Graphics_TargetFrameRate,
        Graphics_AntiAliasingLevel,

        // ── Gameplay ─────────────────────────────────────────────────────────────
        Gameplay_Difficulty,

        // ── Accessibility ─────────────────────────────────────────────────────────
        Accessibility_ColorblindType,
    }

    /// <summary>All boolean settings accessible through <see cref="SettingsManager"/>.</summary>
    public enum BoolSetting
    {
        // ── Audio ─────────────────────────────────────────────────────────────────
        Audio_MuteAll,

        // ── Graphics ─────────────────────────────────────────────────────────────
        Graphics_Fullscreen,
        Graphics_VSyncEnabled,
        Graphics_Bloom,
        Graphics_MotionBlur,
        Graphics_AmbientOcclusion,

        // ── Gameplay ─────────────────────────────────────────────────────────────
        Gameplay_InvertY,
        Gameplay_ShowTutorials,
        Gameplay_ShowDamageNumbers,
        Gameplay_ScreenShake,

        // ── Accessibility ─────────────────────────────────────────────────────────
        Accessibility_ColorblindMode,
        Accessibility_ReducedMotion,
        Accessibility_LargeText,
        Accessibility_SubtitlesEnabled,
        Accessibility_HighContrastUI,
    }

    /// <summary>All string settings accessible through <see cref="SettingsManager"/>.</summary>
    public enum StringSetting
    {
        // ── Gameplay ─────────────────────────────────────────────────────────────
        Gameplay_Language,
    }
}