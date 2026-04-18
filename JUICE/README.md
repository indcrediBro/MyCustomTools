# 🍊 JUICE — Game Feel Feedback System for Unity

A lightweight, modular game-feel system inspired by Feel/MMFeedbacks.
Stack any number of feedbacks on a **JuicePlayer** component and trigger them all
with a single `Play()` call — no code required for setup.

---

## Requirements

- **Unity 2020.3 LTS or newer** (uses `[SerializeReference]` for polymorphic lists)
- No third-party dependencies

---

## Installation

1. Copy the `JUICE/` folder into your project's `Assets/` directory.
2. Unity compiles automatically — no package manager steps needed.

---

## Quick Start

1. Select any GameObject and click **Add Component → JUICE → Juice Player**.
2. In the Inspector, click **＋ Add Feedback** and pick a type from the menu.
3. Configure the feedback's settings in the foldout.
4. Hit **Play Mode** and click **▶ Play** in the inspector — or call it from code:

```csharp
public JuicePlayer hitFeedback;

void OnHit()
{
    hitFeedback.Play();
}
```

---

## Included Feedbacks

### 📷 Camera
| Feedback | Description |
|---|---|
| **Camera Shake** | Perlin-noise shake with position & rotation amplitude, frequency, and smooth falloff |

### 🐢 Transform
| Feedback | Description |
|---|---|
| **Position** | Shake in place, Move to Destination, or Move A→B along an animation curve |
| **Rotation** | Additive (spring back), To Destination, Continuous spin, or Shake |
| **Scale** | Punch (pop & spring), Squash & Stretch (volume-conserving), or Custom curve |

### 💄 Renderer
| Feedback | Description |
|---|---|
| **Flicker** | Rapid color switch on any Renderer's material using MaterialPropertyBlock (no new material instances) |

### 🔊 Audio
| Feedback | Description |
|---|---|
| **Sound** | Plays an AudioClip with randomized pitch & volume. Fire-and-forget or via existing AudioSource |

### ✨ Particles
| Feedback | Description |
|---|---|
| **Particles** | Play an existing ParticleSystem, or instantiate a prefab with auto-destroy |

### 🌶 UI
| Feedback | Description |
|---|---|
| **Flash** | Fullscreen color flash with fade-out. Auto-creates an overlay Canvas if none is assigned |
| **Image** | Animate a UI Image's Color, Alpha, or Fill amount over time |
| **Canvas Group** | Fade a CanvasGroup's alpha in or out, with optional interaction disable |

### 💡 Light
| Feedback | Description |
|---|---|
| **Light** | Animate a Light's intensity and/or color over a duration using an animation curve |

### ⌚ Time
| Feedback | Description |
|---|---|
| **Time Scale** | Freeze Frame (instant pause), Slow Motion (smooth in/out), or Custom timescale curve |

### 💃 Animation
| Feedback | Description |
|---|---|
| **Animation Parameter** | Set Trigger, Bool, Int, or Float on an Animator. Supports auto-reset for Bool |

### 📅 Events
| Feedback | Description |
|---|---|
| **Unity Event** | Trigger any UnityEvent — wire up anything without writing a custom feedback |

### 📦 Control
| Feedback | Description |
|---|---|
| **Set Active** | Enable, disable, toggle, or pulse-activate one or more GameObjects |

---

## Inspector Features

| Feature | How |
|---|---|
| Enable/disable a feedback | Toggle on the left of each row |
| Reorder feedbacks | ▲ ▼ buttons |
| Delete a feedback | ✕ button |
| See delay at a glance | Orange `+0.1s` badge appears when Delay > 0 |
| Run simultaneously | Default — all enabled feedbacks fire at once |
| Run sequentially | Toggle **Sequential** in the options row |
| Auto-play on start | Toggle **Play On Awake** |
| Preview in editor | Enter Play Mode → click **▶ Play** |
| Stop mid-play | Click **■ Stop** |

---

## Creating a Custom Feedback

Subclass `Feedback`, implement two members, and register it in the editor:

```csharp
// 1. Create the feedback (in Runtime/)
[System.Serializable]
public class DebugLogFeedback : Feedback
{
    public override string DefaultLabel => "🐛 Debug Log";

    [Header("Message")]
    public string Message = "JUICE played!";
    public LogType LogType = LogType.Log;

    protected override IEnumerator Play(GameObject owner)
    {
        switch (LogType)
        {
            case LogType.Warning: Debug.LogWarning(Message); break;
            case LogType.Error:   Debug.LogError(Message);   break;
            default:              Debug.Log(Message);         break;
        }
        yield return null;
    }
}
```

```csharp
// 2. Register it in JuicePlayerEditor.cs — add one line to FeedbackRegistry:
("Debug", typeof(DebugLogFeedback), "🐛  Debug Log"),
```

That's it — it appears in the **＋ Add Feedback** menu under its category immediately.

---

## Calling from Code

```csharp
// Play all feedbacks
juicePlayer.Play();

// Stop all feedbacks
juicePlayer.Stop();

// Check if playing
if (juicePlayer.IsPlaying) { ... }
```

---

## File Structure

```
JUICE/
├── Runtime/
│   ├── JUICE.asmdef
│   ├── Feedback.cs                     ← Base class + shared easing
│   ├── JuicePlayer.cs                  ← Main MonoBehaviour
│   ├── CameraShakeFeedback.cs
│   ├── ScaleFeedback.cs
│   ├── PositionFeedback.cs
│   ├── RotationFeedback.cs
│   ├── FlickerFeedback.cs
│   ├── SoundFeedback.cs
│   ├── ParticlesFeedback.cs
│   ├── FlashFeedback.cs
│   ├── ImageFeedback.cs
│   ├── CanvasGroupFeedback.cs
│   ├── LightFeedback.cs
│   ├── TimeScaleFeedback.cs
│   ├── AnimationParameterFeedback.cs
│   ├── UnityEventFeedback.cs
│   └── SetActiveFeedback.cs
└── Editor/
    ├── JUICE.Editor.asmdef
    └── JuicePlayerEditor.cs            ← Custom inspector
```
