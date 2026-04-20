// ============================================================
//  MusicManager.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Self-bootstrapping — do NOT add to any scene manually.
//  A hidden DontDestroyOnLoad GameObject is created at startup.
//
//  USAGE:
//    // Play immediately (fades in using track's DefaultFadeIn)
//    MusicManager.Instance.Play(myTrack);
//
//    // Crossfade with a custom duration
//    MusicManager.Instance.CrossfadeTo(myTrack, 2.5f);
//
//    // Queue for after the current track finishes
//    MusicManager.Instance.Enqueue(nextTrack);
//
//    // Stop with a fade
//    MusicManager.Instance.Stop(1.5f);
//
//    // Pause / Resume
//    MusicManager.Instance.Pause();
//    MusicManager.Instance.Resume();
//
//    // Runtime volume override (stacks with mixer)
//    MusicManager.Instance.SetVolumeScale(0.5f);
// ============================================================

using System.Collections;
using System.Collections.Generic;
using Core.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Manages background music using a dual-source A/B crossfade pattern.<br/>
    /// Supports intro → loop sequencing, playlist queuing, beat-sync metadata,
    /// and smooth volume transitions.<br/><br/>
    /// Volume is driven by the AudioMixer Music group (set up by <see cref="SettingsApplier"/>).
    /// An optional runtime scale (<see cref="SetVolumeScale"/>) lets game code
    /// duck music independently of user preferences.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>The active MusicManager instance.</summary>
        public static MusicManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Fallback Mixer Group")]
        [Tooltip("Applied to both music sources when a MusicTrack has no group assigned.")]
        [SerializeField]
        private AudioMixerGroup defaultMusicGroup;

        [Header("Settings Fallback (no AudioMixer)")]
        [Tooltip("When true and no AudioMixer is used, MusicManager applies " +
                 "SettingsManager.LiveAudio.MusicVolume directly to both sources.")]
        [SerializeField]
        private bool applySettingsVolumeDirectly = false;

        [Header("Behaviour")]
        [Tooltip("When true, the playlist loops back to the first track after the last one plays.")]
        [SerializeField]
        private bool loopPlaylist = false;

        [Tooltip("Crossfade duration used when a queued track auto-advances. " +
                 "0 = cut immediately.")]
        [SerializeField]
        private float autoAdvanceFade = 1.5f;

        // ── State ──────────────────────────────────────────────────────────────

        // A/B dual sources
        private AudioSource _sourceA;
        private AudioSource _sourceB;

        // Which source is currently the "active" (louder) one
        private AudioSource _active;
        private AudioSource _inactive;

        private MusicTrack _currentTrack;
        private float _volumeScale = 1f; // runtime multiplier (duck music etc.)
        private bool _isPaused;

        // Coroutine handles for overlapping transitions
        private Coroutine _introCoroutine;
        private Coroutine _crossfadeCoroutine;
        private Coroutine _stopCoroutine;

        // Playlist queue
        private readonly Queue<MusicTrack> _playlist = new Queue<MusicTrack>();

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when a new track begins playing (after intro, just before loop starts).</summary>
        public static event System.Action<MusicTrack> OnTrackStarted;

        /// <summary>Fired when music stops (fade complete or Stop() called).</summary>
        public static event System.Action OnMusicStopped;

        // ── Public readonly state ──────────────────────────────────────────────

        /// <summary>The track currently fading in or playing.</summary>
        public MusicTrack CurrentTrack => _currentTrack;

        /// <summary>True while music is paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>True if any music source is actively playing.</summary>
        public bool IsPlaying => (_active != null && _active.isPlaying) || _isPaused;

        /// <summary>Number of tracks waiting in the playlist queue.</summary>
        public int QueuedCount => _playlist.Count;

        // ── Bootstrap ──────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[MusicManager]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MusicManager>();
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

            _sourceA = CreateMusicSource("MusicSource_A");
            _sourceB = CreateMusicSource("MusicSource_B");
            _active = _sourceA;
            _inactive = _sourceB;
        }

        private void OnEnable() => SettingsManager.OnApplied += OnSettingsApplied;
        private void OnDisable() => SettingsManager.OnApplied -= OnSettingsApplied;

        private void Update()
        {
            // Auto-advance to the next queued track when the loop clip ends
            // (for non-looping tracks or when loop is false on the source)
            if (!_isPaused && _currentTrack != null
                           && _active != null && !_active.isPlaying
                           && _active.clip != null)
            {
                AdvancePlaylist();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Start playing a <see cref="MusicTrack"/> immediately.<br/>
        /// If music is already playing, crossfades from the current track using
        /// <paramref name="fadeOutDuration"/> for the outgoing and the track's
        /// <c>DefaultFadeIn</c> for the incoming track.<br/>
        /// Clears any queued playlist.
        /// </summary>
        /// <param name="track">The track to play.</param>
        /// <param name="fadeOutDuration">
        /// Seconds to fade the current track out. −1 uses the current track's
        /// <c>DefaultFadeOut</c>.
        /// </param>
        public void Play(MusicTrack track, float fadeOutDuration = -1f)
        {
            if (track == null || !track.IsValid)
            {
                Debug.LogWarning("[MusicManager] Play called with null/invalid track.");
                return;
            }

            _playlist.Clear();
            CrossfadeTo(track, fadeOutDuration);
        }

        /// <summary>
        /// Crossfade to a new <see cref="MusicTrack"/>.<br/>
        /// Unlike <see cref="Play"/>, this does NOT clear the playlist.
        /// </summary>
        /// <param name="track">The track to crossfade to.</param>
        /// <param name="fadeOutDuration">
        /// Seconds for the outgoing fade. −1 uses the current track's DefaultFadeOut
        /// (or the incoming track's DefaultFadeIn if no current track).
        /// </param>
        public void CrossfadeTo(MusicTrack track, float fadeOutDuration = -1f)
        {
            if (track == null || !track.IsValid) return;

            float outDuration = fadeOutDuration >= 0f
                ? fadeOutDuration
                : (_currentTrack != null ? _currentTrack.DefaultFadeOut : track.DefaultFadeIn);

            StopAllMusicCoroutines();
            _crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(track, outDuration, track.DefaultFadeIn));
        }

        /// <summary>
        /// Stop music, fading out over <paramref name="fadeDuration"/> seconds.<br/>
        /// Fires <see cref="OnMusicStopped"/> when complete.
        /// </summary>
        public void Stop(float fadeDuration = 1f)
        {
            _playlist.Clear();
            StopAllMusicCoroutines();
            _stopCoroutine = StartCoroutine(StopCoroutine(fadeDuration));
        }

        /// <summary>Pause all music sources.</summary>
        public void Pause()
        {
            if (_isPaused) return;
            _sourceA.Pause();
            _sourceB.Pause();
            _isPaused = true;
        }

        /// <summary>Resume paused music sources.</summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _sourceA.UnPause();
            _sourceB.UnPause();
            _isPaused = false;
        }

        /// <summary>
        /// Add a track to the end of the playlist queue.<br/>
        /// It will play automatically after the current track finishes.
        /// </summary>
        public void Enqueue(MusicTrack track)
        {
            if (track != null && track.IsValid) _playlist.Enqueue(track);
        }

        /// <summary>Clear the playlist without stopping the current track.</summary>
        public void ClearPlaylist() => _playlist.Clear();

        /// <summary>
        /// Set a runtime volume scale (0–1) applied on top of the mixer and settings.<br/>
        /// Use to duck music during cutscenes, dialogues, etc.
        /// </summary>
        public void SetVolumeScale(float scale)
        {
            _volumeScale = Mathf.Clamp01(scale);
            RefreshSourceVolumes();
        }

        /// <summary>
        /// Smoothly tween the volume scale from its current value to <paramref name="target"/>
        /// over <paramref name="duration"/> seconds.
        /// </summary>
        public void TweenVolumeScale(float target, float duration)
        {
            StartCoroutine(TweenVolumeScaleCoroutine(target, duration));
        }

        // ── Private: Crossfade ─────────────────────────────────────────────────

        private IEnumerator CrossfadeCoroutine(MusicTrack track, float outDuration, float inDuration)
        {
            _currentTrack = track;

            // Swap A/B roles
            (_active, _inactive) = (_inactive, _active);

            float trackVolume = track.EffectiveVolume * _volumeScale;

            if (applySettingsVolumeDirectly)
            {
                float settingsVol = SettingsManager.LiveAudio.MuteAll
                    ? 0f
                    : SettingsManager.LiveAudio.MusicVolume;
                trackVolume *= settingsVol;
            }

            // Configure the incoming source (new active)
            _active.outputAudioMixerGroup = track.MixerGroup != null ? track.MixerGroup : defaultMusicGroup;
            _active.volume = 0f;
            _active.loop = false;

            // Simultaneously fade out the old source and fade in the new one
            float elapsed = 0f;
            float maxDuration = Mathf.Max(outDuration, inDuration, 0.01f);

            // Start the intro/loop sequence on the new active source
            _introCoroutine = StartCoroutine(PlayTrackSequence(track, _active));

            while (elapsed < maxDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float outT = outDuration > 0f ? Mathf.Clamp01(elapsed / outDuration) : 1f;
                float inT = inDuration > 0f ? Mathf.Clamp01(elapsed / inDuration) : 1f;

                _inactive.volume = Mathf.Lerp(_inactive.volume > 0 ? _inactive.volume : 0f, 0f, outT);
                _active.volume = Mathf.Lerp(0f, trackVolume, inT);

                yield return null;
            }

            _inactive.volume = 0f;
            _inactive.Stop();
            _inactive.clip = null;

            _active.volume = trackVolume;

            OnTrackStarted?.Invoke(track);
            _crossfadeCoroutine = null;
        }

        // ── Private: Intro → Loop sequencing ──────────────────────────────────

        private IEnumerator PlayTrackSequence(MusicTrack track, AudioSource source)
        {
            if (track.HasIntro)
            {
                source.clip = track.IntroClip;
                source.loop = false;
                source.Play();

                // Wait for intro to finish
                while (source.isPlaying && source.clip == track.IntroClip)
                    yield return null;
            }

            if (track.HasLoop && source == _active && _currentTrack == track)
            {
                source.clip = track.LoopClip;
                source.loop = true;
                source.Play();
            }
            else if (!track.HasLoop)
            {
                // One-shot track — after it ends, try playlist
                while (source.isPlaying) yield return null;
                if (_currentTrack == track) AdvancePlaylist();
            }
        }

        // ── Private: Stop ──────────────────────────────────────────────────────

        private IEnumerator StopCoroutine(float duration)
        {
            float startA = _sourceA.volume;
            float startB = _sourceB.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                _sourceA.volume = Mathf.Lerp(startA, 0f, t);
                _sourceB.volume = Mathf.Lerp(startB, 0f, t);
                yield return null;
            }

            _sourceA.Stop();
            _sourceA.clip = null;
            _sourceA.volume = 0f;
            _sourceB.Stop();
            _sourceB.clip = null;
            _sourceB.volume = 0f;
            _currentTrack = null;
            _isPaused = false;
            _stopCoroutine = null;

            OnMusicStopped?.Invoke();
        }

        // ── Private: Volume scale tween ────────────────────────────────────────

        private IEnumerator TweenVolumeScaleCoroutine(float target, float duration)
        {
            float start = _volumeScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _volumeScale = Mathf.Lerp(start, target, elapsed / duration);
                RefreshSourceVolumes();
                yield return null;
            }

            _volumeScale = target;
            RefreshSourceVolumes();
        }

        private void RefreshSourceVolumes()
        {
            if (_currentTrack == null) return;
            float vol = _currentTrack.EffectiveVolume * _volumeScale;

            if (applySettingsVolumeDirectly)
            {
                float sv = SettingsManager.LiveAudio.MuteAll
                    ? 0f
                    : SettingsManager.LiveAudio.MusicVolume;
                vol *= sv;
            }

            _active.volume = vol;
        }

        // ── Private: Playlist ──────────────────────────────────────────────────

        private void AdvancePlaylist()
        {
            if (_playlist.Count > 0)
            {
                var next = _playlist.Dequeue();
                if (loopPlaylist) _playlist.Enqueue(next); // re-add to end for looping
                CrossfadeTo(next, autoAdvanceFade);
            }
            else
            {
                _currentTrack = null;
                OnMusicStopped?.Invoke();
            }
        }

        // ── Private: Helpers ───────────────────────────────────────────────────

        private AudioSource CreateMusicSource(string sourceName)
        {
            var go = new GameObject(sourceName) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // music is always 2D
            source.volume = 0f;
            source.loop = true;
            return source;
        }

        private void StopAllMusicCoroutines()
        {
            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
                _introCoroutine = null;
            }

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            if (_stopCoroutine != null)
            {
                StopCoroutine(_stopCoroutine);
                _stopCoroutine = null;
            }
        }

        private void OnSettingsApplied()
        {
            if (applySettingsVolumeDirectly) RefreshSourceVolumes();
        }
    }
}