# SpriteAnimator

A lightweight, frame-based sprite animator for Unity that works with both **SpriteRenderer** (2D) and **MeshRenderer** (3D). No ScriptableObjects, no Animator Controller — just plain C# and Inspector-configured clip lists.

---

## Files

| File | Purpose |
|---|---|
| `SpriteAnimator.cs` | The animator component. Drop this on your GameObject. |

---

## Setup

### 1. Add the component

Attach `SpriteAnimator` to any GameObject that already has a `SpriteRenderer` **or** `MeshRenderer`. The component auto-detects which renderer is present at runtime.

> For **MeshRenderer**, a private material instance is created automatically so your shared project material is never mutated.

### 2. Configure animations in the Inspector

Each entry in the **Animations** list is one clip:

| Field | Description |
|---|---|
| `Name` | Identifier used in code (`Play("Run")`) |
| `Frames` | Array of `Sprite` assets, played in order |
| `FPS` | Playback speed in frames per second |
| `Loop` | Whether the clip repeats indefinitely |
| `Next Animation` | Name of a clip to auto-transition to when this one ends (leave empty to stop) |

### 3. Set a default animation

Set **Default Animation** to the name of the clip you want to play on `Start`. Enable **Play On Start** to begin immediately.

---

## Renderer Behaviour

### SpriteRenderer
Each frame swaps `SpriteRenderer.sprite` directly.

### MeshRenderer
Each frame calls `material.SetTexture("_MainTex", sprite.texture)` on a private material instance. If your shader uses a different property name, change **Texture Property Name** in the Inspector.

---

## API Reference

```csharp
// Play a clip by name. No-ops if the same clip is already playing.
animator.Play("Run");

// Force restart even if the clip is already active.
animator.Restart("Run");

// Pause / resume without resetting the frame.
animator.Pause();
animator.Resume();

// Stop and reset to frame 0.
animator.Stop();

// Jump to a specific frame index without changing play state.
animator.SetFrame(3);

// Query state.
bool active  = animator.IsPlaying;
string name  = animator.CurrentAnimation;   // null if nothing playing
int frame    = animator.CurrentFrame;
bool exists  = animator.HasAnimation("Jump");
```

---

## Events

Subscribe to these from any script to react to animation milestones:

```csharp
var anim = GetComponent<SpriteAnimator>();

// Fired when a new clip starts.
anim.OnAnimationStart += clipName => Debug.Log($"Started: {clipName}");

// Fired when a non-looping clip plays its last frame.
anim.OnAnimationComplete += clipName => Debug.Log($"Finished: {clipName}");

// Fired every time the frame index advances.
// Useful for footstep sounds, hit detection on specific frames, VFX, etc.
anim.OnFrameChanged += (clipName, frameIndex) =>
{
    if (clipName == "Attack" && frameIndex == 3)
        SpawnHitEffect();
};
```

---

## Common Patterns

### One-shot clip that returns to Idle

Set **Loop** to `false` and **Next Animation** to `"Idle"` on the clip.  
The animator transitions automatically when the last frame plays — no code needed.

### Manual transition on complete

```csharp
anim.OnAnimationComplete += name =>
{
    if (name == "Death") LoadGameOverScreen();
};
```

### Triggering a one-shot from code

```csharp
void OnAttack()
{
    // "Attack" is non-looping with nextAnimation = "Idle"
    animator.Play("Attack");
}
```

### Reacting to movement

```csharp
void Update()
{
    bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    string target = moving ? "Walk" : "Idle";

    if (animator.CurrentAnimation != target)
        animator.Play(target);
}
```

### Flipping the sprite

`SpriteAnimator` handles playback only — flipping is done directly on the renderer:

```csharp
// 2D
GetComponent<SpriteRenderer>().flipX = movingLeft;

// 3D: flip the local scale X
transform.localScale = new Vector3(movingLeft ? -1 : 1, 1, 1);
```

---

## Tips

- **Frame order matters.** Sprites in the `Frames` array are played index 0 → last. Reorder them in the Inspector to adjust timing.
- **Mixed FPS per clip.** Each clip has its own `FPS` value, so an idle can run at 8 fps while an attack runs at 24 fps.
- **MeshRenderer + atlas.** If you use a texture atlas, the `SetTexture` call replaces the whole texture each frame. For better GPU performance with atlases, swap materials per frame instead (one material per frame baked with the cropped texture).
- **No Animator Controller conflicts.** If the GameObject also has a Unity `Animator` component, make sure it isn't assigned a Controller that also drives the SpriteRenderer sprite, or they will fight each other.
