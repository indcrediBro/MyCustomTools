// ============================================================
//  MusicTrack.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Create via: Right-click in Project → Create → Audio → Music Track
//
//  USAGE:
//    MusicManager.Instance.Play(myTrack);
//    MusicManager.Instance.CrossfadeTo(myTrack, 2f);
//    MusicManager.Instance.Stop(1.5f);
// ============================================================

using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Data asset describing a piece of music.<br/>
    /// Supports a one-shot intro clip followed by a seamlessly looping clip,
    /// per-track volume offsets, crossfade defaults, and BPM metadata for
    /// future beat-synchronised transitions.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Music Track", fileName = "Music_New")]
    public class MusicTrack : ScriptableObject
    {
        // ── Clips ──────────────────────────────────────────────────────────────

        [Header("Clips")]
        [Tooltip("Played once at the start. Leave null to go straight to the loop.\n\n" +
                 "Useful for pick-up intros (e.g. a 4-bar lead-in before a theme loops).")]
        public AudioClip IntroClip;

        [Tooltip("Loops indefinitely (or until crossfaded/stopped). " +
                 "Can be null if you only want a one-shot piece.")]
        public AudioClip LoopClip;

        // ── Mixer Routing ──────────────────────────────────────────────────────

        [Header("Mixer Routing")]
        [Tooltip("Assign to your Music AudioMixer group. " +
                 "Leave null to output through the Master group.")]
        public AudioMixerGroup MixerGroup;

        // ── Volume ─────────────────────────────────────────────────────────────

        [Header("Volume")]
        [Tooltip("Base linear volume for this track (0–1). " +
                 "The AudioMixer Music group handles global music volume scaling.")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("Per-track loudness trim in dB (applied on top of the mixer group). " +
                 "Use to normalise tracks that are inherently louder or quieter than others.\n" +
                 "The dB value is converted to a linear multiplier and applied to the " +
                 "AudioSource volume together with the Volume field above.")]
        [Range(-20f, 20f)]
        public float VolumeOffsetDb = 0f;

        // ── Transitions ────────────────────────────────────────────────────────

        [Header("Transitions")]
        [Tooltip("Default fade-in duration when this track starts playing (seconds).")]
        [Min(0f)]
        public float DefaultFadeIn = 1f;

        [Tooltip("Default fade-out duration when this track stops or is replaced (seconds).")] [Min(0f)]
        public float DefaultFadeOut = 1f;

        // ── Metadata ───────────────────────────────────────────────────────────

        [Header("Metadata")]
        [Tooltip("Beats per minute of this track. Reserved for future beat-synced crossfades " +
                 "where the transition aligns to the nearest bar boundary.")]
        [Min(1f)]
        public float BPM = 120f;

        [Tooltip("Optional tags for filtering and playlist management. " +
                 "Examples: \"combat\", \"ambient\", \"boss\", \"menu\".")]
        public string[] Tags;

        // ── Derived helpers ────────────────────────────────────────────────────

        /// <summary>True if an intro clip is assigned.</summary>
        public bool HasIntro => IntroClip != null;

        /// <summary>True if a loop clip is assigned.</summary>
        public bool HasLoop => LoopClip != null;

        /// <summary>
        /// The effective linear volume for the AudioSource: base Volume multiplied
        /// by the linear conversion of <see cref="VolumeOffsetDb"/>.
        /// </summary>
        public float EffectiveVolume =>
            Volume * Mathf.Pow(10f, VolumeOffsetDb / 20f);

        /// <summary>Seconds per beat at the track's BPM.</summary>
        public float SecondsPerBeat => 60f / BPM;

        /// <summary>Seconds per bar (4/4 assumed).</summary>
        public float SecondsPerBar => SecondsPerBeat * 4f;

        /// <summary>Returns true if the track has at least one of intro or loop clip.</summary>
        public bool IsValid => HasIntro || HasLoop;

        /// <summary>Check whether this track has a given tag (case-insensitive).</summary>
        public bool HasTag(string tag)
        {
            if (Tags == null) return false;
            foreach (var t in Tags)
                if (string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}