// ============================================================
//  AudioHandle.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Returned by AudioManager.Play() and AudioManager.PlayAt().
//  Store it to control a specific sound after it starts.
//
//  USAGE:
//    AudioHandle footstep = AudioManager.Instance.Play(sfxFootstep);
//    ...
//    if (footstep.IsValid) footstep.FadeOut(0.3f);
//
//  Safe to ignore: AudioHandle.Invalid is the default, all
//  methods are no-ops when the sound has already ended.
// ============================================================

namespace Core.Audio
{
    /// <summary>
    /// A lightweight, safe token referencing a single active sound in
    /// <see cref="AudioManager"/>.<br/><br/>
    /// AudioHandle is a <c>readonly struct</c> — copy freely with no allocation.<br/>
    /// Stale handles (sound already finished) are safe to call — all methods
    /// check <see cref="IsValid"/> first and silently do nothing if the sound
    /// is no longer active.
    /// </summary>
    public readonly struct AudioHandle
    {
        // ── Sentinel ───────────────────────────────────────────────────────────

        /// <summary>A handle that is never valid. Returned on failed Play() calls.</summary>
        public static readonly AudioHandle Invalid = new AudioHandle(-1, 0);

        // ── Identity ───────────────────────────────────────────────────────────

        // The pool slot index and a generation counter.
        // Generation ensures handles from a previous use of the same slot are stale.
        internal readonly int SlotIndex;
        internal readonly int Generation;

        internal AudioHandle(int slotIndex, int generation)
        {
            SlotIndex = slotIndex;
            Generation = generation;
        }

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>
        /// True while the referenced sound is still active in the AudioManager pool.<br/>
        /// Becomes false automatically when the sound finishes, is stopped, or fades out.
        /// </summary>
        public bool IsValid =>
            SlotIndex >= 0
            && AudioManager.Instance != null
            && AudioManager.Instance.IsActive(SlotIndex, Generation);

        // ── Control ────────────────────────────────────────────────────────────

        /// <summary>Stop this sound immediately and return its source to the pool.</summary>
        public void Stop()
        {
            if (IsValid) AudioManager.Instance.Stop(SlotIndex, Generation);
        }

        /// <summary>
        /// Fade this sound out over <paramref name="duration"/> seconds, then return to pool.<br/>
        /// Default duration is 1 second.
        /// </summary>
        public void FadeOut(float duration = 1f)
        {
            if (IsValid) AudioManager.Instance.FadeOut(SlotIndex, Generation, duration);
        }

        /// <summary>
        /// Pause this sound. The pool slot is held — call <see cref="Resume"/> to continue.
        /// </summary>
        public void Pause()
        {
            if (IsValid) AudioManager.Instance.Pause(SlotIndex, Generation);
        }

        /// <summary>Resume a paused sound.</summary>
        public void Resume()
        {
            if (IsValid) AudioManager.Instance.Resume(SlotIndex, Generation);
        }

        /// <summary>
        /// Change the volume of this specific instance at runtime (0–1 linear).<br/>
        /// Stacks multiplicatively with the SoundEffect asset's base volume.
        /// </summary>
        public void SetVolume(float volume)
        {
            if (IsValid) AudioManager.Instance.SetVolume(SlotIndex, Generation, volume);
        }

        // ── Equality ───────────────────────────────────────────────────────────

        public bool Equals(AudioHandle other) =>
            SlotIndex == other.SlotIndex && Generation == other.Generation;

        public override bool Equals(object obj) =>
            obj is AudioHandle h && Equals(h);

        public override int GetHashCode() =>
            SlotIndex * 397 ^ Generation;

        public static bool operator ==(AudioHandle a, AudioHandle b) => a.Equals(b);
        public static bool operator !=(AudioHandle a, AudioHandle b) => !a.Equals(b);

        public override string ToString() =>
            IsValid ? $"AudioHandle(slot={SlotIndex}, gen={Generation})" : "AudioHandle(Invalid)";
    }
}