# Audio System

A self-bootstrapping, mixer-integrated audio system for Unity built on top of the **Settings Manager** and **GamePrefs** stack. No scene setup required — both managers create themselves at runtime.

---

## Table of Contents

- [File Overview](#file-overview)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
- [AudioMixer Setup](#audiomixer-setup)
- [SoundEffect Asset](#soundeffect-asset)
- [MusicTrack Asset](#musictrack-asset)
- [AudioManager](#audiomanager)
- [MusicManager](#musicmanager)
- [AudioHandle](#audiohandle)
- [Settings Integration](#settings-integration)
- [Volume Authority Model](#volume-authority-model)
- [Common Patterns](#common-patterns)
- [Extending the System](#extending-the-system)

---

## File Overview

| File | Folder | Purpose |
|---|---|---|
| `SoundEffect.cs` | `Assets/` | ScriptableObject — clip variations, pitch, cooldown, 3D spatial settings |
| `MusicTrack.cs` | `Assets/` | ScriptableObject — intro + loop clips, per-track volume, fade defaults, BPM |
| `AudioHandle.cs` | `Assets/` | Struct token returned by `AudioManager.Play()` — stop/pause/fade a specific instance |
| `AudioManager.cs` | `Assets/` | Self-bootstrapping singleton — pooled AudioSources, SFX/UI/3D playback |
| `MusicManager.cs` | `Assets/` | Self-bootstrapping singleton — A/B crossfade, intro→loop, playlist queue |

### Dependencies (from the same project)
| File | Role |
|---|---|
| `SettingsManager.cs` | Provides `LiveAudio` volume values and `OnApplied` event |
| `SettingsApplier.cs` | Routes `SettingsManager` values to the AudioMixer in dB |
| `SettingPaths.cs` | Typed enums for settings access (`FloatSetting.Audio_MasterVolume`, etc.) |

---

## Architecture

```
AudioMixer  ←──────────────────────────────  SettingsApplier
  │   MasterVolume (dB)                          │ subscribes to SettingsManager.OnApplied
  │   MusicVolume  (dB)                          │
  │   SFXVolume    (dB)                          │
  │   UIVolume     (dB)                          │
  │                                              │
  ├── Music Sub-mix ◄── MusicManager             │ reads LiveAudio for optional direct scale
  │       Source A  (fading out)                 │
  │       Source B  (fading in)                  │
  │                                              │
  └── SFX Sub-mix  ◄── AudioManager             │
  │       Pool[0..31]                            │
  │                                              │
  └── UI Sub-mix   ◄── AudioManager.PlayUI()
```

Both managers bootstrap via `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` — identical to the `GamePrefsAutoSave` pattern already in the project. They are hidden `DontDestroyOnLoad` GameObjects you never see in the Hierarchy unless you unhide hidden objects.

---

## Quick Start

### 1 — Create a SoundEffect asset

Right-click in the Project window → **Create → Audio → Sound Effect**

Fill in:
- `Clips` — drag in one or more `AudioClip`s
- `MixerGroup` — assign your **SFX** mixer sub-group
- Leave everything else at defaults to start

### 2 — Create a MusicTrack asset

Right-click in the Project window → **Create → Audio → Music Track**

Fill in:
- `IntroClip` — optional one-shot intro (leave null to skip)
- `LoopClip` — the clip that loops after the intro
- `MixerGroup` — assign your **Music** mixer sub-group

### 3 — Play sounds in code

```csharp
// Fire-and-forget SFX
AudioManager.Instance.Play(sfxExplosion);

// 3D positional SFX
AudioManager.Instance.PlayAt(sfxFootstep, transform.position);

// UI sound (forces 2D, uses UI mixer group fallback)
AudioManager.Instance.PlayUI(sfxButtonClick);

// Start music
MusicManager.Instance.Play(trackMainTheme);

// Crossfade to a new track over 2 seconds
MusicManager.Instance.CrossfadeTo(trackCombat, 2f);
```

### 4 — Wire the AudioManager Inspector (optional but recommended)

The `AudioManager` and `MusicManager` self-create, so they have no Inspector by default. To expose the `defaultSFXGroup`, `defaultUIGroup`, and `defaultMusicGroup` fallback references, add a scene-level **configurator** object:

```csharp
// AudioSystemConfig.cs — attach to your persistent [GameSystems] GameObject
public class AudioSystemConfig : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    private void Awake()
    {
        // AudioManager and MusicManager are already alive (bootstrapped before scene load)
        // but their Inspector references need to be set here if you want fallback groups.
        // Alternatively, always assign MixerGroup directly on each SoundEffect/MusicTrack asset.
    }
}
```

> **Simplest approach:** assign `MixerGroup` on every `SoundEffect` and `MusicTrack` asset directly. This makes each asset fully self-contained and eliminates the need for a configurator entirely.

---

## AudioMixer Setup

The AudioMixer is created in **Window → Audio → Audio Mixer**. Recommended structure:

```
Master
├── Music       ← MusicManager sources route here
├── SFX         ← AudioManager SFX sources route here
└── UI          ← AudioManager.PlayUI() sources route here
```

**Exposing parameters for SettingsApplier:**

1. Select a group (e.g. **Master**)
2. Right-click the **Volume** parameter → **Expose 'Volume' to script**
3. In the **Exposed Parameters** list (top-left of the mixer window), rename it to match your `SettingsApplier` Inspector field (e.g. `MasterVolume`)
4. Repeat for `MusicVolume`, `SFXVolume`, `UIVolume`

`SettingsApplier` converts linear 0–1 settings values to dB and writes them to these parameters whenever the user applies settings. The audio managers never touch the mixer parameters directly — they only set `AudioSource.volume` (0–1) and `outputAudioMixerGroup`.

---

## SoundEffect Asset

**Create:** Right-click → Create → Audio → Sound Effect

### Fields

| Field | Default | Description |
|---|---|---|
| `Clips` | — | Array of clips. One is chosen at random per play. Add multiples for natural variation. |
| `Volume` | `1.0` | Base linear volume (0–1). Stacks with the mixer group's level. |
| `PitchMin` | `1.0` | Minimum pitch multiplier. |
| `PitchMax` | `1.0` | Maximum pitch multiplier. Set equal to `PitchMin` for no randomisation. |
| `MixerGroup` | null | Output AudioMixerGroup. Falls back to `AudioManager.defaultSFXGroup` if null. |
| `Loop` | false | Loops until stopped via `AudioHandle.Stop()`. |
| `Cooldown` | `0` | Minimum seconds between plays of this specific asset. Prevents rapid-fire spam. |
| `MaxConcurrent` | `0` | Max simultaneous instances. `0` = unlimited. |
| `StealOldestWhenCapped` | false | When the cap is hit, stop the oldest instance instead of dropping the new play. |
| `SpatialBlend` | `0` | `0` = 2D, `1` = full 3D. Only active when using `PlayAt()`. |
| `MinDistance` | `1` | Full volume within this radius (units). |
| `MaxDistance` | `50` | Inaudible beyond this radius. |
| `RolloffMode` | Logarithmic | Volume falloff curve with distance. |
| `Spread` | `0` | Stereo spread for 3D sounds. |
| `DopplerLevel` | `1` | Doppler effect strength. |

### Tips

- **Footsteps:** Add 4–6 clip variations with `PitchMin = 0.95`, `PitchMax = 1.05`, `Cooldown = 0.1`, `MaxConcurrent = 3`
- **Gunshot:** `PitchMin = 0.98`, `PitchMax = 1.02`, `StealOldestWhenCapped = true`, `MaxConcurrent = 5`
- **Ambient loop:** `Loop = true`, `SpatialBlend = 1`, configure distance settings for the space size
- **UI click:** `Volume = 0.7`, `SpatialBlend = 0`, `MixerGroup` = UI group

---

## MusicTrack Asset

**Create:** Right-click → Create → Audio → Music Track

### Fields

| Field | Default | Description |
|---|---|---|
| `IntroClip` | null | Plays once before the loop. Leave null to skip straight to looping. |
| `LoopClip` | null | Loops indefinitely after the intro finishes. |
| `MixerGroup` | null | Output group. Falls back to `MusicManager.defaultMusicGroup` if null. |
| `Volume` | `1.0` | Base linear volume for this track. |
| `VolumeOffsetDb` | `0` | Per-track dB trim for loudness normalisation across your music library. |
| `DefaultFadeIn` | `1.0` | Seconds to fade this track in when it starts. |
| `DefaultFadeOut` | `1.0` | Seconds to fade this track out when replaced or stopped. |
| `BPM` | `120` | Beats per minute — stored for future beat-synced crossfades. |
| `Tags` | — | String tags for filtering. Examples: `"combat"`, `"menu"`, `"boss"` |

### Intro → Loop Sequencing

When both `IntroClip` and `LoopClip` are assigned, the track plays like this:

```
[IntroClip — plays once] → [LoopClip — loops forever] → crossfade out on CrossfadeTo()
```

This is the standard pattern for game music (pick-up intros, 4-bar lead-ins before a theme loops). If only `LoopClip` is assigned, it loops from the start immediately.

### Computed Properties

```csharp
float effectiveVolume = track.EffectiveVolume;  // Volume * pow(10, VolumeOffsetDb / 20)
float secsPerBeat     = track.SecondsPerBeat;   // 60 / BPM
float secsPerBar      = track.SecondsPerBar;    // SecondsPerBeat * 4
bool  hasTag          = track.HasTag("combat"); // case-insensitive tag check
```

---

## AudioManager

### Key Properties

| Property | Description |
|---|---|
| `AudioManager.Instance` | The singleton. Bootstrapped before the first scene loads. |

### Methods

```csharp
// 2D fire-and-forget
AudioHandle Play(SoundEffect asset)

// 3D positional (uses asset's SpatialBlend, min/max distance)
AudioHandle PlayAt(SoundEffect asset, Vector3 worldPosition)

// UI sound — forces SpatialBlend = 0, uses defaultUIGroup fallback
AudioHandle PlayUI(SoundEffect asset)

// Stop everything immediately
void StopAll()
```

### Pool Configuration

The pool size defaults to **32 sources**. To change it, the `AudioManager` is created via `RuntimeInitializeOnLoadMethod` so it has no Inspector at startup. The cleanest way to change pool size is to modify the `poolSize` field default in `AudioManager.cs`, or to add an `AudioSystemConfig` MonoBehaviour (see Quick Start) that sets it before Bootstrap fires — though Bootstrap fires before `Awake`, so a pre-scene configurator approach works best.

If the pool is exhausted, `Play()` logs a warning and returns `AudioHandle.Invalid`. Tune `poolSize` based on your game's worst-case audio density.

---

## MusicManager

### Key Properties

| Property | Type | Description |
|---|---|---|
| `CurrentTrack` | `MusicTrack` | The track currently fading in or playing. Null when stopped. |
| `IsPlaying` | `bool` | True if any source is active or paused. |
| `IsPaused` | `bool` | True while paused. |
| `QueuedCount` | `int` | Number of tracks waiting in the playlist queue. |

### Events

```csharp
// Fired when a track finishes fading in and its loop begins
MusicManager.OnTrackStarted += (MusicTrack track) => { ... };

// Fired when music fully stops (fade complete, or Stop() with no queue)
MusicManager.OnMusicStopped += () => { ... };
```

### Methods

```csharp
// Play immediately, clears queue, crossfades from current track
void Play(MusicTrack track, float fadeOutDuration = -1f)

// Crossfade without clearing the queue
void CrossfadeTo(MusicTrack track, float fadeOutDuration = -1f)

// Stop with fade
void Stop(float fadeDuration = 1f)

// Pause / Resume both sources
void Pause()
void Resume()

// Playlist
void Enqueue(MusicTrack track)   // adds to end of queue
void ClearPlaylist()             // clears queue, keeps current track

// Runtime volume scale (0–1, stacks with mixer and settings)
void SetVolumeScale(float scale)
void TweenVolumeScale(float target, float duration)  // smooth tween
```

### Crossfade Behaviour

`fadeOutDuration = -1f` (the default) resolves as follows:
- If music is currently playing → uses `CurrentTrack.DefaultFadeOut`
- If nothing is playing → uses the incoming track's `DefaultFadeIn`

Pass an explicit float to override per-call:
```csharp
MusicManager.Instance.CrossfadeTo(combatTrack, 0.5f);  // fast cut
MusicManager.Instance.CrossfadeTo(menuTrack,   3.0f);  // slow cinematic fade
```

### Playlist

```csharp
MusicManager.Instance.Play(trackAct1);
MusicManager.Instance.Enqueue(trackAct2);
MusicManager.Instance.Enqueue(trackAct3);

// Tracks play in order: Act1 → Act2 → Act3 → (stop, or loop if loopPlaylist = true)
```

Set `loopPlaylist = true` in the Inspector (requires an `AudioSystemConfig` to reach the self-created instance) for seamless looping playlists.

---

## AudioHandle

`AudioHandle` is a `readonly struct` — zero allocation, copy freely.

```csharp
AudioHandle handle = AudioManager.Instance.Play(sfxAmbientLoop);

// Check if still active
if (handle.IsValid) { ... }

// Stop immediately
handle.Stop();

// Fade out over 0.5 seconds
handle.FadeOut(0.5f);

// Pause / resume
handle.Pause();
handle.Resume();

// Change volume of just this instance
handle.SetVolume(0.3f);
```

### Stale Handle Safety

Handles use a **slot + generation** pattern. When a pool slot is reused for a new sound, its generation counter increments, making all prior handles to that slot report `IsValid = false`. All operations silently no-op on invalid handles — safe to store, safe to call after the sound ends.

```csharp
private AudioHandle _engineLoop;

void StartEngine()   => _engineLoop = AudioManager.Instance.Play(sfxEngineLoop);
void StopEngine()    => _engineLoop.Stop();   // safe even if already stopped
void OnDestroy()     => _engineLoop.Stop();   // safe even if scene changed
```

---

## Settings Integration

The audio system integrates with `SettingsManager` at two layers:

### Layer 1 — AudioMixer (recommended)

`SettingsApplier` converts `SettingsManager.LiveAudio` values to dB and writes them to the mixer on every `SettingsManager.OnApplied`. The audio managers route their sources to the correct mixer group and the mixer handles everything else. No code needed.

### Layer 2 — Direct volume (no AudioMixer)

If your project doesn't use an AudioMixer, enable the fallback options:

| Manager | Option | Effect |
|---|---|---|
| `AudioManager` | `applyMasterVolumeToListener = true` | Sets `AudioListener.volume` on settings change |
| `MusicManager` | `applySettingsVolumeDirectly = true` | Multiplies music source volumes by `LiveAudio.MusicVolume` |

Both options respond to `MuteAll` correctly (drives to 0 when muted).

---

## Volume Authority Model

To avoid double-scaling, each layer owns one responsibility:

```
SettingsManager.LiveAudio.MusicVolume (0–1)
    │
    ▼  SettingsApplier converts to dB → writes to AudioMixer "MusicVolume" param
AudioMixer.MusicVolume (dB)
    │
    ▼  MusicManager sets AudioSource.volume = track.EffectiveVolume × _volumeScale
AudioSource.volume (0–1, per-source)
    │
    ▼  Final output = Mixer dB scale × Source linear volume
Speaker
```

`MusicManager.SetVolumeScale()` and `MusicManager.TweenVolumeScale()` sit at the per-source level — they affect music independently of user preferences. Use them for gameplay ducking (dialogue, cutscenes) without ever touching the user's saved settings.

---

## Common Patterns

### Combat music transition

```csharp
public class CombatZone : MonoBehaviour
{
    [SerializeField] private MusicTrack combatTrack;
    [SerializeField] private MusicTrack ambienceTrack;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            MusicManager.Instance.CrossfadeTo(combatTrack, 0.5f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            MusicManager.Instance.CrossfadeTo(ambienceTrack, 2f);
    }
}
```

### Dialogue music duck

```csharp
public class DialogueSystem : MonoBehaviour
{
    private void StartDialogue()
    {
        MusicManager.Instance.TweenVolumeScale(0.2f, 0.5f); // duck to 20% over 0.5s
    }

    private void EndDialogue()
    {
        MusicManager.Instance.TweenVolumeScale(1f, 1f); // restore over 1s
    }
}
```

### Managed looping ambient

```csharp
public class AmbientZone : MonoBehaviour
{
    [SerializeField] private SoundEffect sfxAmbient;
    private AudioHandle _ambientHandle;

    private void OnEnable()
    {
        _ambientHandle = AudioManager.Instance.PlayAt(sfxAmbient, transform.position);
    }

    private void OnDisable()
    {
        _ambientHandle.FadeOut(1f);
    }
}
```

### Footstep system with cooldown and variation

```csharp
public class FootstepController : MonoBehaviour
{
    [SerializeField] private SoundEffect sfxFootstepGrass;
    [SerializeField] private SoundEffect sfxFootstepStone;

    // Called from animation events
    private void OnFootstep()
    {
        var sfx = IsOnStone() ? sfxFootstepStone : sfxFootstepGrass;
        AudioManager.Instance.PlayAt(sfx, transform.position);
        // Cooldown and MaxConcurrent on the asset prevent spam automatically
    }
}
```

### React to settings changes in a custom system

```csharp
public class CustomAudioSystem : MonoBehaviour
{
    private void OnEnable()  => SettingsManager.OnApplied += OnSettingsApplied;
    private void OnDisable() => SettingsManager.OnApplied -= OnSettingsApplied;

    private void OnSettingsApplied()
    {
        bool subtitles = SettingsManager.LiveAccessibility.SubtitlesEnabled;
        float contrast = SettingsManager.LiveAccessibility.Contrast;
        // respond to accessibility changes that affect audio presentation
    }
}
```

---

## Extending the System

### Add a new sound category (e.g. Voice)

1. **Add to AudioSettings** in `SettingsData.cs`:
   ```csharp
   public float VoiceVolume = 1f;
   ```
2. **Add to SettingPaths** in `SettingPaths.cs`:
   ```csharp
   Audio_VoiceVolume,  // in FloatSetting enum
   ```
3. **Add to SettingsManager** get/set switches
4. **Expose a new mixer parameter** (`VoiceVolume`) in your AudioMixer
5. **Add to SettingsApplier** `ApplyAudio()`:
   ```csharp
   SetMixerDb(voiceVolumeParam, a.VoiceVolume);
   ```
6. **Add a `defaultVoiceGroup`** field to `AudioManager` and a `PlayVoice()` wrapper

### Beat-synced crossfades

`MusicTrack.BPM`, `SecondsPerBeat`, and `SecondsPerBar` are already stored. To crossfade on the next bar boundary:

```csharp
float timeToNextBar = track.SecondsPerBar
    - (AudioSettings.dspTime % track.SecondsPerBar);
StartCoroutine(CrossfadeAfter(nextTrack, timeToNextBar));
```

### Audio snapshots / states

For complex games with many distinct audio states (menu, gameplay, paused, underwater), extend `MusicManager` with a state machine or use Unity's **AudioMixer Snapshots** with `mixer.TransitionToSnapshot(snapshot, transitionTime)` alongside the managers.
