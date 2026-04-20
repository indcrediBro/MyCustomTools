// ============================================================
//  AudioManager.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Self-bootstrapping — do NOT add to any scene manually.
//  A hidden DontDestroyOnLoad GameObject is created at startup.
//
//  USAGE:
//    // Fire-and-forget
//    AudioManager.Instance.Play(sfxExplosion);
//
//    // 3D positional
//    AudioManager.Instance.PlayAt(sfxFootstep, transform.position);
//
//    // Controlled playback
//    AudioHandle h = AudioManager.Instance.Play(sfxAmbient);
//    h.FadeOut(2f);
//
//    // UI sounds (convenience wrapper — auto-selects UI mixer group
//    //            if the SoundEffect has none assigned)
//    AudioManager.Instance.PlayUI(sfxClick);
// ============================================================

using System.Collections;
using System.Collections.Generic;
using Core.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Manages all non-music audio: SFX, UI sounds, ambient loops.<br/>
    /// Uses a fixed-size pool of <see cref="AudioSource"/>s so no allocation
    /// happens at play time.<br/><br/>
    /// Integrate with the settings system by assigning the same
    /// <see cref="AudioMixer"/> that <see cref="SettingsApplier"/> uses —
    /// the mixer handles global volume; AudioManager just drives individual sources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>
        /// The active AudioManager instance. Bootstrapped automatically at startup.<br/>
        /// Will be null before the first scene loads (use null-conditional access).
        /// </summary>
        public static AudioManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Pool")]
        [Tooltip("Maximum number of simultaneously active sound sources. " +
                 "Tune based on your game's worst-case audio density.")]
        [SerializeField]
        private int poolSize = 32;

        [Header("Fallback Mixer Groups")]
        [Tooltip("Fallback mixer group for SoundEffects that have no group assigned. " +
                 "Assign to your SFX sub-mix.")]
        [SerializeField]
        private AudioMixerGroup defaultSFXGroup;

        [Tooltip("Fallback mixer group for UI sounds. " +
                 "Assign to your UI sub-mix.")]
        [SerializeField]
        private AudioMixerGroup defaultUIGroup;

        [Header("Settings Fallback (no AudioMixer)")]
        [Tooltip("When true and no AudioMixer is used, AudioManager applies " +
                 "SettingsManager volume values directly to AudioListener.volume.")]
        [SerializeField]
        private bool applyMasterVolumeToListener = false;

        // ── Pool ───────────────────────────────────────────────────────────────

        private struct PoolSlot
        {
            public AudioSource Source;
            public SoundEffect Asset; // null = unused slot
            public int Generation; // increments on each reuse
            public bool IsPaused;
            public Coroutine FadeCoroutine;
        }

        private PoolSlot[] _pool;

        // Slots waiting for their AudioSource to finish naturally
        // (non-looping sounds we don't stop manually)
        private readonly HashSet<int> _activeSlots = new HashSet<int>();

        // Reverse lookup: oldest playing instance of each SoundEffect (for StealOldest)
        private readonly Dictionary<SoundEffect, Queue<int>> _assetSlots =
            new Dictionary<SoundEffect, Queue<int>>();

        // ── Bootstrap ──────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject("[AudioManager]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioManager>();
        }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildPool();
        }

        private void OnEnable() => SettingsManager.OnApplied += OnSettingsApplied;
        private void OnDisable() => SettingsManager.OnApplied -= OnSettingsApplied;

        private void Update() => RecycleFinishedSources();

        // ── Public Play API ────────────────────────────────────────────────────

        /// <summary>
        /// Play a 2D sound effect.<br/>
        /// Returns <see cref="AudioHandle.Invalid"/> if the sound was rejected
        /// (cooldown active, concurrent cap reached, no clips assigned).
        /// </summary>
        public AudioHandle Play(SoundEffect asset)
            => PlayInternal(asset, Vector3.zero, isPositional: false, uiMode: false);

        /// <summary>
        /// Play a 3D positional sound at <paramref name="worldPosition"/>.<br/>
        /// SpatialBlend on the asset controls how strongly position affects panning.
        /// </summary>
        public AudioHandle PlayAt(SoundEffect asset, Vector3 worldPosition)
            => PlayInternal(asset, worldPosition, isPositional: true, uiMode: false);

        /// <summary>
        /// Play a UI sound. Uses <see cref="defaultUIGroup"/> if the asset has no
        /// group assigned and forces 2D (SpatialBlend = 0).
        /// </summary>
        public AudioHandle PlayUI(SoundEffect asset)
            => PlayInternal(asset, Vector3.zero, isPositional: false, uiMode: true);

        /// <summary>Stop all currently active sounds immediately.</summary>
        public void StopAll()
        {
            foreach (int i in new List<int>(_activeSlots))
                ReleaseSlot(i);
            _activeSlots.Clear();
        }

        // ── Handle operations (called by AudioHandle) ──────────────────────────

        /// <summary>True if the slot is still active with the expected generation.</summary>
        public bool IsActive(int slotIndex, int generation)
        {
            if (slotIndex < 0 || slotIndex >= _pool.Length) return false;
            return _activeSlots.Contains(slotIndex) && _pool[slotIndex].Generation == generation;
        }

        public void Stop(int slotIndex, int generation)
        {
            if (!IsActive(slotIndex, generation)) return;
            ReleaseSlot(slotIndex);
            _activeSlots.Remove(slotIndex);
        }

        public void FadeOut(int slotIndex, int generation, float duration)
        {
            if (!IsActive(slotIndex, generation)) return;
            ref var slot = ref _pool[slotIndex];
            if (slot.FadeCoroutine != null) StopCoroutine(slot.FadeCoroutine);
            slot.FadeCoroutine = StartCoroutine(FadeOutCoroutine(slotIndex, generation, duration));
        }

        public void Pause(int slotIndex, int generation)
        {
            if (!IsActive(slotIndex, generation)) return;
            ref var slot = ref _pool[slotIndex];
            if (slot.IsPaused) return;
            slot.Source.Pause();
            slot.IsPaused = true;
        }

        public void Resume(int slotIndex, int generation)
        {
            if (!IsActive(slotIndex, generation)) return;
            ref var slot = ref _pool[slotIndex];
            if (!slot.IsPaused) return;
            slot.Source.UnPause();
            slot.IsPaused = false;
        }

        public void SetVolume(int slotIndex, int generation, float volume)
        {
            if (!IsActive(slotIndex, generation)) return;
            _pool[slotIndex].Source.volume = Mathf.Clamp01(volume);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void BuildPool()
        {
            _pool = new PoolSlot[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var childGo = new GameObject($"AudioSource_{i:D2}") { hideFlags = HideFlags.HideAndDontSave };
                childGo.transform.SetParent(transform);
                _pool[i].Source = childGo.AddComponent<AudioSource>();
                _pool[i].Source.playOnAwake = false;
                _pool[i].Generation = 0;
            }
        }

        private AudioHandle PlayInternal(SoundEffect asset, Vector3 position,
            bool isPositional, bool uiMode)
        {
            if (asset == null)
            {
                Debug.LogWarning("[AudioManager] Play called with null SoundEffect.");
                return AudioHandle.Invalid;
            }

            // ── Cooldown / concurrent checks ───────────────────────────────────
            if (!asset.CanPlay())
            {
                if (asset.StealOldestWhenCapped && asset.MaxConcurrent > 0)
                {
                    if (!TryStealOldest(asset))
                        return AudioHandle.Invalid;
                }
                else
                {
                    return AudioHandle.Invalid;
                }
            }

            // ── Acquire pool slot ──────────────────────────────────────────────
            int slotIndex = FindFreeSlot();
            if (slotIndex < 0)
            {
                Debug.LogWarning("[AudioManager] Pool exhausted — increase poolSize.");
                return AudioHandle.Invalid;
            }

            AudioClip clip = asset.PickClip();
            if (clip == null) return AudioHandle.Invalid;

            // ── Configure source ───────────────────────────────────────────────
            ref var slot = ref _pool[slotIndex];
            var source = slot.Source;

            asset.ConfigureSource(source, isPositional);

            if (uiMode)
            {
                source.spatialBlend = 0f;
                if (source.outputAudioMixerGroup == null)
                    source.outputAudioMixerGroup = defaultUIGroup;
            }
            else if (source.outputAudioMixerGroup == null)
            {
                source.outputAudioMixerGroup = defaultSFXGroup;
            }

            if (isPositional)
                source.transform.position = position;

            // ── Start playback ─────────────────────────────────────────────────
            source.clip = clip;
            source.Play();

            slot.Asset = asset;
            slot.IsPaused = false;
            slot.FadeCoroutine = null;

            _activeSlots.Add(slotIndex);
            TrackAssetSlot(asset, slotIndex);
            asset.RegisterPlay();

            return new AudioHandle(slotIndex, slot.Generation);
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _pool.Length; i++)
                if (!_activeSlots.Contains(i))
                    return i;
            return -1;
        }

        private bool TryStealOldest(SoundEffect asset)
        {
            if (!_assetSlots.TryGetValue(asset, out var queue)) return false;
            while (queue.Count > 0)
            {
                int oldest = queue.Dequeue();
                if (_activeSlots.Contains(oldest))
                {
                    ReleaseSlot(oldest);
                    _activeSlots.Remove(oldest);
                    return true;
                }
            }

            return false;
        }

        private void TrackAssetSlot(SoundEffect asset, int slotIndex)
        {
            if (!_assetSlots.TryGetValue(asset, out var queue))
            {
                queue = new Queue<int>();
                _assetSlots[asset] = queue;
            }

            queue.Enqueue(slotIndex);
        }

        private void RecycleFinishedSources()
        {
            // Iterate a snapshot to allow removal mid-loop
            var finished = new List<int>();
            foreach (int i in _activeSlots)
            {
                ref var slot = ref _pool[i];
                if (!slot.IsPaused && !slot.Source.isPlaying && slot.Source.clip != null)
                    finished.Add(i);
            }

            foreach (int i in finished)
            {
                _pool[i].Asset?.RegisterStop();
                _activeSlots.Remove(i);
                ResetSlot(i);
            }
        }

        private void ReleaseSlot(int i)
        {
            ref var slot = ref _pool[i];
            if (slot.FadeCoroutine != null)
            {
                StopCoroutine(slot.FadeCoroutine);
                slot.FadeCoroutine = null;
            }

            slot.Source.Stop();
            slot.Asset?.RegisterStop();
            ResetSlot(i);
        }

        private void ResetSlot(int i)
        {
            ref var slot = ref _pool[i];
            slot.Source.clip = null;
            slot.Source.outputAudioMixerGroup = null;
            slot.Source.volume = 1f;
            slot.Source.pitch = 1f;
            slot.Source.spatialBlend = 0f;
            slot.Source.loop = false;
            slot.Source.transform.localPosition = Vector3.zero;
            slot.Asset = null;
            slot.IsPaused = false;
            slot.Generation++; // invalidate all prior handles to this slot
        }

        private IEnumerator FadeOutCoroutine(int slotIndex, int generation, float duration)
        {
            var source = _pool[slotIndex].Source;
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsActive(slotIndex, generation)) yield break;
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            if (IsActive(slotIndex, generation))
            {
                ReleaseSlot(slotIndex);
                _activeSlots.Remove(slotIndex);
            }
        }

        private void OnSettingsApplied()
        {
            if (!applyMasterVolumeToListener) return;

            var a = SettingsManager.LiveAudio;
            AudioListener.volume = a.MuteAll ? 0f : a.MasterVolume;
        }
    }
}