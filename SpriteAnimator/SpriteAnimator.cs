using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flexible frame-based sprite animator that works with both SpriteRenderer and MeshRenderer.
/// Define animations inline via the Inspector — no ScriptableObjects required.
/// Optionally pair with SpriteAnimatorStateMachine for state-driven playback.
/// </summary>
[AddComponentMenu("Animation/Sprite Animator")]
public class SpriteAnimator : MonoBehaviour
{
    // ─── Inspector Data ────────────────────────────────────────────────────────

    [Serializable]
    public class SpriteAnimation
    {
        public string name = "New Animation";

        [Tooltip("Frames to display in order.")]
        public Sprite[] frames = Array.Empty<Sprite>();

        [Tooltip("Frames per second.")]
        [Min(1)] public float fps = 12f;

        [Tooltip("Loop this animation indefinitely.")]
        public bool loop = true;

        [Tooltip("Name of the animation to transition to when this one finishes (leave empty to stop).")]
        public string nextAnimation = "";
    }

    [Header("Animations")]
    [SerializeField] private List<SpriteAnimation> animations = new();

    [Header("Playback")]
    [SerializeField] private string defaultAnimation = "";
    [SerializeField] private bool playOnStart = true;

    [Header("Renderer (auto-detected if left empty)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private MeshRenderer meshRenderer;

    [Tooltip("Material property name that holds the main texture on the MeshRenderer material.")]
    [SerializeField] private string texturePropertyName = "_MainTex";

    // ─── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when a new animation starts playing. Passes the animation name.</summary>
    public event Action<string> OnAnimationStart;

    /// <summary>Fired when an animation finishes its last frame (non-looping). Passes the animation name.</summary>
    public event Action<string> OnAnimationComplete;

    /// <summary>Fired each time the frame index advances. Passes (animationName, frameIndex).</summary>
    public event Action<string, int> OnFrameChanged;

    // ─── Runtime State ─────────────────────────────────────────────────────────

    private SpriteAnimation _current;
    private int _frameIndex;
    private float _timer;
    private bool _playing;

    private enum RendererType { None, Sprite, Mesh }
    private RendererType _rendererType = RendererType.None;

    // Cache material instance for MeshRenderer to avoid leaking shared materials.
    private Material _materialInstance;

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Name of the currently playing animation (null if none).</summary>
    public string CurrentAnimation => _current?.name;

    /// <summary>Current frame index within the active animation.</summary>
    public int CurrentFrame => _frameIndex;

    /// <summary>Whether an animation is actively playing.</summary>
    public bool IsPlaying => _playing;

    /// <summary>Play an animation by name. Ignored if the same animation is already playing.</summary>
    public void Play(string animationName)
    {
        if (_current != null && _current.name == animationName && _playing) return;

        var anim = FindAnimation(animationName);
        if (anim == null)
        {
            Debug.LogWarning($"[SpriteAnimator] Animation '{animationName}' not found on {gameObject.name}.", this);
            return;
        }

        _current = anim;
        _frameIndex = 0;
        _timer = 0f;
        _playing = true;

        ApplyFrame();
        OnAnimationStart?.Invoke(_current.name);
    }

    /// <summary>Play an animation by name, restarting even if it's already active.</summary>
    public void Restart(string animationName)
    {
        _current = null; // force re-entry
        Play(animationName);
    }

    /// <summary>Pause playback without resetting the frame.</summary>
    public void Pause() => _playing = false;

    /// <summary>Resume paused playback.</summary>
    public void Resume() => _playing = true;

    /// <summary>Stop playback and reset to the first frame.</summary>
    public void Stop()
    {
        _playing = false;
        _frameIndex = 0;
        _timer = 0f;
        if (_current != null) ApplyFrame();
    }

    /// <summary>Jump to a specific frame index without changing play state.</summary>
    public void SetFrame(int index)
    {
        if (_current == null || _current.frames.Length == 0) return;
        _frameIndex = Mathf.Clamp(index, 0, _current.frames.Length - 1);
        ApplyFrame();
    }

    /// <summary>Returns true if an animation with the given name exists in the list.</summary>
    public bool HasAnimation(string animationName) => FindAnimation(animationName) != null;

    // ─── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        DetectRenderer();
    }

    private void Start()
    {
        if (playOnStart && !string.IsNullOrEmpty(defaultAnimation))
            Play(defaultAnimation);
    }

    private void Update()
    {
        if (!_playing || _current == null || _current.frames.Length == 0) return;

        _timer += Time.deltaTime;
        float frameDuration = 1f / _current.fps;

        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            AdvanceFrame();
        }
    }

    private void OnDestroy()
    {
        // Clean up the material instance we created for MeshRenderer.
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

    // ─── Internal ──────────────────────────────────────────────────────────────

    private void DetectRenderer()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (spriteRenderer != null)
        {
            _rendererType = RendererType.Sprite;
        }
        else if (meshRenderer != null)
        {
            _rendererType = RendererType.Mesh;
            // Create an instance so we don't mutate the shared project material.
            _materialInstance = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = _materialInstance;
        }
        else
        {
            Debug.LogWarning($"[SpriteAnimator] No SpriteRenderer or MeshRenderer found on {gameObject.name}.", this);
        }
    }

    private void AdvanceFrame()
    {
        _frameIndex++;

        if (_frameIndex >= _current.frames.Length)
        {
            if (_current.loop)
            {
                _frameIndex = 0;
            }
            else
            {
                _frameIndex = _current.frames.Length - 1;
                _playing = false;
                OnAnimationComplete?.Invoke(_current.name);

                // Auto-transition to next animation if specified.
                if (!string.IsNullOrEmpty(_current.nextAnimation))
                    Play(_current.nextAnimation);

                return;
            }
        }

        ApplyFrame();
        OnFrameChanged?.Invoke(_current.name, _frameIndex);
    }

    private void ApplyFrame()
    {
        if (_current == null || _current.frames.Length == 0) return;

        Sprite sprite = _current.frames[_frameIndex];
        if (sprite == null) return;

        switch (_rendererType)
        {
            case RendererType.Sprite:
                spriteRenderer.sprite = sprite;
                break;

            case RendererType.Mesh:
                if (_materialInstance != null)
                    _materialInstance.SetTexture(texturePropertyName, sprite.texture);
                break;
        }
    }

    private SpriteAnimation FindAnimation(string animName)
    {
        foreach (var anim in animations)
            if (anim.name == animName) return anim;
        return null;
    }

    // ─── Editor Helper ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Deduplicate animation names in the editor.
        var seen = new HashSet<string>();
        foreach (var anim in animations)
        {
            if (string.IsNullOrEmpty(anim.name)) anim.name = "Animation";
            while (!seen.Add(anim.name)) anim.name += "_copy";
        }
    }
#endif
}
