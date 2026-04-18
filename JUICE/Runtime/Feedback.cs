using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Base class for all JUICE feedbacks. Inherit from this to create custom feedbacks.
    /// </summary>
    [System.Serializable]
    public abstract class Feedback
    {
        [Tooltip("Optional label shown in the inspector header. Leave empty to use the default.")]
        public string Label = "";

        [Tooltip("Toggle this feedback on or off without removing it.")]
        public bool Enabled = true;

        [Tooltip("Delay in seconds before this feedback triggers after Play() is called.")] [Min(0f)]
        public float Delay = 0f;

        /// <summary>Override to provide a default label shown in the inspector header.</summary>
        public abstract string DefaultLabel { get; }

        /// <summary>The label shown in the inspector.</summary>
        public string DisplayLabel => string.IsNullOrEmpty(Label) ? DefaultLabel : Label;

        /// <summary>Called by JuicePlayer. Handles delay then runs Play().</summary>
        public IEnumerator Execute(MonoBehaviour owner)
        {
            if (!Enabled) yield break;
            if (Delay > 0f) yield return new WaitForSeconds(Delay);
            yield return owner.StartCoroutine(Play(owner.gameObject));
        }

        /// <summary>Override this to implement the feedback logic.</summary>
        protected abstract IEnumerator Play(GameObject owner);

        // ─── Shared easing helpers ────────────────────────────────────
        protected static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        protected static float EaseInCubic(float t) => t * t * t;
        protected static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        protected static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }

        protected static float EaseOutBounce(float t)
        {
            if (t < 1 / 2.75f) return 7.5625f * t * t;
            if (t < 2 / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }

            if (t < 2.5 / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }

            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }
}
