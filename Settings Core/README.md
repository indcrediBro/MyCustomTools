# Advanced Settings Manager — Setup Guide

A full-featured settings system for Unity built on top of **GamePrefs**.  
Supports Audio · Graphics · Gameplay · Accessibility with a Live/Pending UI pattern.

---

## File Overview

| File | Folder | Purpose |
|---|---|---|
| `SettingsData.cs` | `Assets/` | Serialisable data classes (AudioSettings etc.) |
| `SettingsManager.cs` | `Assets/` | Static core — load/save/apply/revert, events, get/set by path |
| `SettingsApplier.cs` | `Assets/` | MonoBehaviour — applies Live settings to Unity engine systems |
| `SettingsUIController.cs` | `Assets/` | MonoBehaviour — panel open/close, tabs, Apply/Cancel/Reset |
| `SettingsSliderBinder.cs` | `Assets/` | Component — binds a Slider to any float/int setting |
| `SettingsToggleBinder.cs` | `Assets/` | Component — binds a Toggle to any bool setting |
| `SettingsDropdownBinder.cs` | `Assets/` | Component — binds a Dropdown to any int setting |

All five GamePrefs files from your project stay completely untouched.

---

## Quick Start (5 minutes)

### Step 1 — Drop SettingsApplier into the scene

1. Create a persistent GameObject (e.g. `[GameSystems]`).  
2. Add **SettingsApplier** to it.  
3. In the Inspector, assign your **AudioMixer** asset and set the four exposed parameter names.  
   > In the AudioMixer window, right-click a volume parameter → **Expose to script**, then rename it in the *Exposed Parameters* list.

### Step 2 — Build your settings UI hierarchy

A recommended Canvas layout:

```
SettingsPanel (GameObject)
├─ TabBar
│   ├─ AudioTabButton    (Button)
│   ├─ GraphicsTabButton (Button)
│   ├─ GameplayTabButton (Button)
│   └─ AccessibilityTabButton (Button)
├─ AudioPanel
│   ├─ MasterVolumeRow
│   │   ├─ Label  ("Master Volume")
│   │   ├─ Slider  ← add SettingsSliderBinder
│   │   └─ ValueLabel (TMP_Text, wired into Slider binder)
│   ├─ MusicVolumeRow  …
│   ├─ SFXVolumeRow    …
│   └─ MuteAllToggle   ← add SettingsToggleBinder
├─ GraphicsPanel
│   ├─ QualityDropdown  ← add SettingsDropdownBinder (AutoPopulateQualityLevels)
│   ├─ ResolutionDropdown ← add SettingsDropdownBinder (AutoPopulateResolutions)
│   ├─ FullscreenToggle ← add SettingsToggleBinder
│   └─ VSyncToggle      ← add SettingsToggleBinder
├─ GameplayPanel
│   └─ …
├─ AccessibilityPanel
│   └─ …
├─ ActionBar
│   ├─ ApplyButton  (Button)
│   ├─ CancelButton (Button)
│   └─ ResetButton  (Button)
└─ PendingIndicator  ("● Unsaved changes" text/image, starts hidden)
```

### Step 3 — Add SettingsUIController

1. Add **SettingsUIController** to the `SettingsPanel` root.  
2. Wire all the references in the Inspector.

### Step 4 — Wire each UI element

Add one binder component per control:

**Slider example — Master Volume**
```
GameObject: MasterVolumeSlider (has Slider component)
Add: SettingsSliderBinder
  Path        = "Audio.MasterVolume"
  Type        = Float
  MinValue    = 0
  MaxValue    = 1
  ValueLabel  = (drag in your TMP_Text)
  LabelFormat = "{0:P0}"          ← shows "80%"
```

**Toggle example — Fullscreen**
```
GameObject: FullscreenToggle (has Toggle component)
Add: SettingsToggleBinder
  Path   = "Graphics.Fullscreen"
  Invert = false
```

**Toggle example — Sound Enabled (inverse of MuteAll)**
```
Add: SettingsToggleBinder
  Path   = "Audio.MuteAll"
  Invert = true    ← ON = sound enabled = MuteAll stored as false
```

**Dropdown example — Quality**
```
GameObject: QualityDropdown (has TMP_Dropdown component)
Add: SettingsDropdownBinder
  Path                     = "Graphics.QualityLevel"
  AutoPopulateQualityLevels = true   ← fills options from QualitySettings.names
```

**Dropdown example — Resolution**
```
Add: SettingsDropdownBinder
  Path                    = "Graphics.ResolutionIndex"
  AutoPopulateResolutions = true
```

**Dropdown example — Anti-Aliasing**
```
Add: SettingsDropdownBinder
  Path                    = "Graphics.AntiAliasingLevel"
  AutoPopulateAntiAliasing = true   ← shows Off / 2× / 4× / 8×
```

---

## All Valid Dot-Paths

### float paths (use SettingsSliderBinder with Type = Float)
| Path | Default | Range |
|---|---|---|
| `Audio.MasterVolume` | 1.0 | 0 – 1 |
| `Audio.MusicVolume` | 0.8 | 0 – 1 |
| `Audio.SFXVolume` | 1.0 | 0 – 1 |
| `Audio.UIVolume` | 1.0 | 0 – 1 |
| `Graphics.RenderScale` | 1.0 | 0.5 – 2.0 |
| `Gameplay.MouseSensitivity` | 1.0 | 0.1 – 5.0 |
| `Gameplay.UIScale` | 1.0 | 0.75 – 1.5 |
| `Accessibility.TextScale` | 1.0 | 0.75 – 2.0 |
| `Accessibility.SubtitleSize` | 1.0 | 0.75 – 2.0 |
| `Accessibility.Contrast` | 1.0 | 0.5 – 2.0 |

### int paths (use SettingsSliderBinder Type=Int or SettingsDropdownBinder)
| Path | Default | Notes |
|---|---|---|
| `Graphics.QualityLevel` | 2 | Index into QualitySettings.names |
| `Graphics.ResolutionIndex` | -1 | -1 = native; else index into Screen.resolutions |
| `Graphics.TargetFrameRate` | 60 | Ignored when VSync is on |
| `Graphics.AntiAliasingLevel` | 2 | Unity values: 0, 2, 4, 8 |
| `Gameplay.Difficulty` | 1 | 0 = Easy · 1 = Normal · 2 = Hard |
| `Accessibility.ColorblindType` | 0 | 0 = Deuteranopia · 1 = Protanopia · 2 = Tritanopia |

### bool paths (use SettingsToggleBinder)
| Path | Default |
|---|---|
| `Audio.MuteAll` | false |
| `Graphics.Fullscreen` | true |
| `Graphics.VSyncEnabled` | true |
| `Graphics.Bloom` | true |
| `Graphics.MotionBlur` | false |
| `Graphics.AmbientOcclusion` | true |
| `Gameplay.InvertY` | false |
| `Gameplay.ShowTutorials` | true |
| `Gameplay.ShowDamageNumbers` | true |
| `Gameplay.ScreenShake` | true |
| `Accessibility.ColorblindMode` | false |
| `Accessibility.ReducedMotion` | false |
| `Accessibility.LargeText` | false |
| `Accessibility.SubtitlesEnabled` | true |
| `Accessibility.HighContrastUI` | false |

### string paths
| Path | Default |
|---|---|
| `Gameplay.Language` | "en" |

---

## Using Settings in Gameplay Code

Read **Live** settings (what was last applied and saved):

```csharp
// In any MonoBehaviour, read directly from the Live snapshot
void Start()
{
    float sens = SettingsManager.LiveGameplay.MouseSensitivity;
    bool  shake = SettingsManager.LiveGameplay.ScreenShake;
}
```

React to settings being applied (e.g. to update post-processing):

```csharp
void OnEnable()
{
    SettingsManager.OnApplied += ApplyPostProcess;
    ApplyPostProcess(); // apply immediately on scene load
}

void OnDisable() => SettingsManager.OnApplied -= ApplyPostProcess;

void ApplyPostProcess()
{
    float contrast = SettingsManager.LiveAccessibility.Contrast;
    // ... drive your post-process volume here
}
```

Trigger settings programmatically (e.g. from a difficulty selector):

```csharp
SettingsManager.SetInt("Gameplay.Difficulty", 2); // sets Pending
SettingsManager.Apply();                           // commits to Live + saves
```

---

## Extending with New Settings

1. Add a new field to the appropriate data class in `SettingsData.cs`  
   and update its `Clone()` / `CopyFrom()` methods.

2. Add a `case` for it in the matching `Get*` and `Set*` switch in `SettingsManager.cs`.

3. Add a UI element and the appropriate binder component — done.

---

## Save Slot Support

Settings always save to **slot 0** by default (matching GamePrefs default).  
To save settings on a per-profile slot, call:

```csharp
GamePrefs.SetActiveSlot(1);   // switch to slot 1
SettingsManager.Load();       // reload settings for that slot
```

---

## LabelFormat Cheatsheet

| Format string | Output for 0.8 |
|---|---|
| `{0:P0}` | 80% |
| `{0:F1}` | 0.8 |
| `{0:F0}` | 1   |
| `{0:F2}` | 0.80 |
| `{0} FPS` | 60 FPS (with Int type) |
