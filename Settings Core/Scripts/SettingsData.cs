// ============================================================
//  SettingsData.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Pure data classes — no Unity lifecycle, no MonoBehaviour.
//  All fields are [Serializable] so GamePrefs can store them
//  via JsonUtility.
// ============================================================

using System;

namespace Core.Settings
{
// ── Audio ──────────────────────────────────────────────────────────────────

/// <summary>All audio-related settings. Volumes are linear 0–1.</summary>
[Serializable]
public class AudioSettings
{
    public float MasterVolume = 1f;
    public float MusicVolume  = 0.8f;
    public float SFXVolume    = 1f;
    public float UIVolume     = 1f;
    public bool  MuteAll      = false;

    public AudioSettings Clone() => (AudioSettings)MemberwiseClone();

    public void CopyFrom(AudioSettings src)
    {
        MasterVolume = src.MasterVolume;
        MusicVolume  = src.MusicVolume;
        SFXVolume    = src.SFXVolume;
        UIVolume     = src.UIVolume;
        MuteAll      = src.MuteAll;
    }
}

// ── Graphics ───────────────────────────────────────────────────────────────

/// <summary>All graphics-related settings.</summary>
[Serializable]
public class GraphicsSettings
{
    /// <summary>Index into QualitySettings.names (0 = lowest).</summary>
    public int   QualityLevel      = 2;

    /// <summary>Index into Screen.resolutions. -1 = keep current resolution.</summary>
    public int   ResolutionIndex   = -1;

    public bool  Fullscreen        = true;

    /// <summary>Ignored when VSyncEnabled is true.</summary>
    public int   TargetFrameRate   = 60;

    public bool  VSyncEnabled      = true;

    /// <summary>Render scale multiplier. 1.0 = native. Values: 0.5, 0.75, 1.0, 1.25, 1.5.</summary>
    public float RenderScale       = 1f;

    /// <summary>MSAA sample count. Valid Unity values: 0, 2, 4, 8.</summary>
    public int   AntiAliasingLevel = 2;

    public bool  Bloom             = true;
    public bool  MotionBlur        = false;
    public bool  AmbientOcclusion  = true;

    public GraphicsSettings Clone() => (GraphicsSettings)MemberwiseClone();

    public void CopyFrom(GraphicsSettings src)
    {
        QualityLevel      = src.QualityLevel;
        ResolutionIndex   = src.ResolutionIndex;
        Fullscreen        = src.Fullscreen;
        TargetFrameRate   = src.TargetFrameRate;
        VSyncEnabled      = src.VSyncEnabled;
        RenderScale       = src.RenderScale;
        AntiAliasingLevel = src.AntiAliasingLevel;
        Bloom             = src.Bloom;
        MotionBlur        = src.MotionBlur;
        AmbientOcclusion  = src.AmbientOcclusion;
    }
}

// ── Gameplay ───────────────────────────────────────────────────────────────

/// <summary>Gameplay and control preferences.</summary>
[Serializable]
public class GameplaySettings
{
    public float  MouseSensitivity  = 1f;
    public bool   InvertY           = false;
    public bool   ShowTutorials     = true;
    public float  UIScale           = 1f;
    public bool   ShowDamageNumbers = true;
    public bool   ScreenShake       = true;

    /// <summary>0 = Easy, 1 = Normal, 2 = Hard.</summary>
    public int    Difficulty        = 1;

    /// <summary>BCP-47 language code, e.g. "en", "fr", "de".</summary>
    public string Language          = "en";

    public GameplaySettings Clone() => (GameplaySettings)MemberwiseClone();

    public void CopyFrom(GameplaySettings src)
    {
        MouseSensitivity  = src.MouseSensitivity;
        InvertY           = src.InvertY;
        ShowTutorials     = src.ShowTutorials;
        UIScale           = src.UIScale;
        ShowDamageNumbers = src.ShowDamageNumbers;
        ScreenShake       = src.ScreenShake;
        Difficulty        = src.Difficulty;
        Language          = src.Language;
    }
}

// ── Accessibility ──────────────────────────────────────────────────────────

/// <summary>Accessibility preferences.</summary>
[Serializable]
public class AccessibilitySettings
{
    public bool  ColorblindMode    = false;

    /// <summary>0 = Deuteranopia, 1 = Protanopia, 2 = Tritanopia.</summary>
    public int   ColorblindType    = 0;

    public bool  ReducedMotion     = false;
    public bool  LargeText         = false;

    /// <summary>UI text scale multiplier (1.0 = default).</summary>
    public float TextScale         = 1f;

    public bool  SubtitlesEnabled  = true;
    public float SubtitleSize      = 1f;

    /// <summary>Post-process contrast multiplier (1.0 = unchanged).</summary>
    public float Contrast          = 1f;

    public bool  HighContrastUI    = false;

    public AccessibilitySettings Clone() => (AccessibilitySettings)MemberwiseClone();

    public void CopyFrom(AccessibilitySettings src)
    {
        ColorblindMode   = src.ColorblindMode;
        ColorblindType   = src.ColorblindType;
        ReducedMotion    = src.ReducedMotion;
        LargeText        = src.LargeText;
        TextScale        = src.TextScale;
        SubtitlesEnabled = src.SubtitlesEnabled;
        SubtitleSize     = src.SubtitleSize;
        Contrast         = src.Contrast;
        HighContrastUI   = src.HighContrastUI;
    }
}
}