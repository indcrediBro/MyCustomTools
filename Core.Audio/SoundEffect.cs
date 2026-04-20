// ============================================================
//  SoundEffect.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Create via: Right-click in Project → Create → Audio → Sound Effect
//
//  USAGE:
//    AudioManager.Instance.Play(mySoundEffect);
//    AudioManager.Instance.PlayAt(mySoundEffect, transform.position);
//    AudioHandle h = AudioManager.Instance.Play(mySoundEffect);
//    h.FadeOut(0.5f);
// ============================================================

using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Data asset describing a sound effect.<br/>
    /// Supports clip randomisation, pitch variation, per-asset cooldown,
    /// concurrent instance capping, and full 3D spatial configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Sound Effect", fileName = "SFX_New")]
    public class SoundEffect : ScriptableObject
    {
        // ── Clips ──────────────────────────────────────────────────────────────

        [Header("Clips")]
        [Tooltip("One clip is chosen at random each time this sound plays. " +
                 "Add multiple for natural variation (e.g. footsteps, hits).")]
        public AudioClip[] Clips;

        // ── Volume & Pitch ─────────────────────────────────────────────────────

        [Header("Volume & Pitch")]
        [Tooltip("Base linear volume of this sound (0–1). " +
                 "The AudioMixer group handles global SFX/Master scaling.")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("Minimum pitch multiplier. 1.0 = normal.")] [Range(-3f, 3f)]
        public float PitchMin = 1f;

        [Tooltip("Maximum pitch multiplier. Set equal to PitchMin for no randomisation.")] [Range(-3f, 3f)]
        public float PitchMax = 1f;

        // ── Mixer Routing ──────────────────────────────────────────────────────

        [Header("Mixer Routing")]
        [Tooltip("Assign to your SFX or UI AudioMixer group. " +
                 "Leave null to output through the Master group.")]
        public AudioMixerGroup MixerGroup;

        // ── Behaviour ──────────────────────────────────────────────────────────

        [Header("Behaviour")] [Tooltip("When true, the sound loops until explicitly stopped via AudioHandle.Stop().")]
        public bool Loop = false;

        [Tooltip("Minimum seconds that must elapse before this asset can play again. " +
                 "Useful for rapid-fire prevention (footsteps, gunshots). 0 = no limit.")]
        [Min(0f)]
        public float Cooldown = 0f;

        [Tooltip("Maximum number of simultaneous instances of this sound. " +
                 "When the cap is reached, new Play() calls are silently ignored. " +
                 "0 = unlimited.")]
        [Min(0)]
        public int MaxConcurrent = 0;

        [Tooltip("When the concurrent cap is reached, stop the oldest instance instead " +
                 "of silently dropping the new one.")]
        public bool StealOldestWhenCapped = false;

        // ── 3D Spatial Audio ───────────────────────────────────────────────────

        [Header("3D Spatial Audio")]
        [Tooltip("0 = fully 2D (ignores position). 1 = fully 3D (position-dependent volume/pan).")]
        [Range(0f, 1f)]
        public float SpatialBlend = 0f;

        [Tooltip("Within this distance (units) the sound is heard at full volume.")] [Min(0f)]
        public float MinDistance = 1f;

        [Tooltip("Beyond this distance the sound is inaudible.")] [Min(0f)]
        public float MaxDistance = 50f;

        [Tooltip("How volume falls off with distance.")]
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

        [Tooltip("Stereo pan spread for 3D sounds (0 = narrow, 1 = full spread).")] [Range(0f, 1f)]
        public float Spread = 0f;

        [Tooltip("How quickly the Doppler effect is applied to this sound.")] [Range(0f, 5f)]
        public float DopplerLevel = 1f;

        // ── Runtime State (not saved) ──────────────────────────────────────────

        [System.NonSerialized] private float _lastPlayTime = float.NegativeInfinity;
        [System.NonSerialized] private int _concurrentCount;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Pick a random clip from <see cref="Clips"/>. Returns null if the array is empty.</summary>
        public AudioClip PickClip()
        {
            if (Clips == null || Clips.Length == 0)
            {
                Debug.LogWarning($"[SoundEffect] \"{name}\" has no clips assigned.", this);
                return null;
            }

            return Clips.Length == 1 ? Clips[0] : Clips[Random.Range(0, Clips.Length)];
        }

        /// <summary>Random pitch within the [PitchMin, PitchMax] range.</summary>
        public float PickPitch() => Random.Range(PitchMin, PitchMax);

        /// <summary>
        /// Returns true if the sound is allowed to play right now.<br/>
        /// Checks cooldown and concurrent cap. Does NOT count the play — call
        /// <see cref="RegisterPlay"/> immediately after deciding to play.
        /// </summary>
        public bool CanPlay()
        {
            if (Cooldown > 0f && Time.time - _lastPlayTime < Cooldown) return false;
            if (MaxConcurrent > 0 && _concurrentCount >= MaxConcurrent) return false;
            return true;
        }

        /// <summary>How many instances are currently playing (live count maintained by AudioManager).</summary>
        public int ConcurrentCount => _concurrentCount;

        /// <summary>Called by AudioManager immediately before starting playback.</summary>
        public void RegisterPlay()
        {
            _lastPlayTime = Time.time;
            _concurrentCount++;
        }

        /// <summary>Called by AudioManager when a source finishes or is stopped.</summary>
        public void RegisterStop() => _concurrentCount = Mathf.Max(0, _concurrentCount - 1);

        /// <summary>Apply all settings from this asset to a pre-existing AudioSource.</summary>
        public void ConfigureSource(AudioSource source, bool is3D)
        {
            source.clip = PickClip();
            source.volume = Volume;
            source.pitch = PickPitch();
            source.loop = Loop;
            source.outputAudioMixerGroup = MixerGroup;
            source.spatialBlend = is3D ? SpatialBlend : 0f;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.rolloffMode = RolloffMode;
            source.spread = Spread;
            source.dopplerLevel = DopplerLevel;
        }
    }
}