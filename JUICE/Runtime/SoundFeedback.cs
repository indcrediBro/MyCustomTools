using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace JUICE
{
    /// <summary>
    /// Plays an AudioClip with randomized pitch and volume for natural variation.
    /// OneShot mode creates a temporary AudioSource at the owner's position (fire-and-forget).
    /// </summary>
    [System.Serializable]
    public class SoundFeedback : Feedback
    {
        public override string DefaultLabel => "🔊 Sound";

        [Header("Clip")] public AudioClip Clip;

        [Header("Existing Source (optional)")]
        [Tooltip("If assigned, plays through this source. Otherwise a temporary one-shot source is created.")]
        public AudioSource TargetSource;

        [Header("Volume")] [Range(0f, 1f)] public float MinVolume = 0.9f;
        [Range(0f, 1f)] public float MaxVolume = 1f;

        [Header("Pitch")] [Range(-3f, 3f)] public float MinPitch = 0.95f;
        [Range(-3f, 3f)] public float MaxPitch = 1.05f;

        [Header("Mixer")] public AudioMixerGroup MixerGroup;

        protected override IEnumerator Play(GameObject owner)
        {
            if (Clip == null)
            {
                Debug.LogWarning("[JUICE] Sound: no AudioClip assigned.");
                yield break;
            }

            float volume = Random.Range(MinVolume, MaxVolume);
            float pitch = Random.Range(MinPitch, MaxPitch);

            if (TargetSource != null)
            {
                TargetSource.pitch = pitch;
                TargetSource.PlayOneShot(Clip, volume);
            }
            else
            {
                GameObject go = new GameObject("JUICE_OneShot");
                go.transform.position = owner.transform.position;
                AudioSource src = go.AddComponent<AudioSource>();
                src.clip = Clip;
                src.volume = volume;
                src.pitch = pitch;
                src.spatialBlend = 0f;
                if (MixerGroup != null) src.outputAudioMixerGroup = MixerGroup;
                src.Play();
                Object.Destroy(go, Clip.length / Mathf.Abs(pitch) + 0.1f);
            }

            yield return null;
        }
    }
}