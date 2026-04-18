using System.Collections;
using UnityEngine;

namespace JUICE
{
    public enum ScaleMode
    {
        Punch,
        SquashAndStretch,
        Custom
    }

    /// <summary>
    /// Animates the scale of a transform.
    /// Punch: pops out then springs back. Squash & Stretch: conserves volume like a bouncing ball.
    /// Custom: animate between two arbitrary scales along an animation curve.
    /// </summary>
    [System.Serializable]
    public class ScaleFeedback : Feedback
    {
        public override string DefaultLabel => "📐 Scale";

        [Header("Target")] public Transform Target;

        [Header("Mode")] public ScaleMode Mode = ScaleMode.Punch;

        [Header("Punch")] public Vector3 PunchAmount = new Vector3(0.3f, 0.3f, 0.3f);
        [Min(0.01f)] public float PunchDuration = 0.1f;
        [Min(0.01f)] public float PunchReturnDuration = 0.15f;

        [Header("Squash & Stretch")] public Vector3 SquashAxis = Vector3.up;
        [Range(0f, 1f)] public float SquashAmount = 0.3f;
        [Min(0.01f)] public float SquashDuration = 0.1f;
        [Min(0.01f)] public float StretchDuration = 0.1f;
        [Min(0.01f)] public float SquashReturnDuration = 0.15f;

        [Header("Custom")] public Vector3 FromScale = Vector3.one;
        public Vector3 ToScale = new Vector3(1.3f, 1.3f, 1.3f);
        [Min(0.01f)] public float CustomDuration = 0.2f;
        public bool ReturnToOriginal = true;
        [Min(0.01f)] public float CustomReturnDuration = 0.2f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        protected override IEnumerator Play(GameObject owner)
        {
            Transform t = Target != null ? Target : owner.transform;
            Vector3 original = t.localScale;

            switch (Mode)
            {
                case ScaleMode.Punch: yield return Punch(t, original); break;
                case ScaleMode.SquashAndStretch: yield return SquashStretch(t, original); break;
                case ScaleMode.Custom: yield return Custom(t, original); break;
            }
        }

        private IEnumerator Punch(Transform t, Vector3 original)
        {
            Vector3 peak = original + PunchAmount;
            yield return LerpScale(t, original, peak, PunchDuration, EaseOutCubic);
            yield return LerpScale(t, peak, original, PunchReturnDuration, EaseOutElastic);
        }

        private IEnumerator SquashStretch(Transform t, Vector3 original)
        {
            Vector3 squash = original - Vector3.Scale(SquashAxis, original) * SquashAmount;
            Vector3 stretch = original + Vector3.Scale(SquashAxis, original) * SquashAmount * 0.5f;
            yield return LerpScale(t, original, squash, SquashDuration, EaseOutCubic);
            yield return LerpScale(t, squash, stretch, StretchDuration, EaseOutCubic);
            yield return LerpScale(t, stretch, original, SquashReturnDuration, EaseOutElastic);
        }

        private IEnumerator Custom(Transform t, Vector3 original)
        {
            float elapsed = 0f;
            while (elapsed < CustomDuration)
            {
                t.localScale = Vector3.LerpUnclamped(FromScale, ToScale, Curve.Evaluate(elapsed / CustomDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localScale = ToScale;
            if (ReturnToOriginal)
                yield return LerpScale(t, ToScale, original, CustomReturnDuration, EaseOutCubic);
        }

        private static IEnumerator LerpScale(Transform t, Vector3 from, Vector3 to, float duration,
            System.Func<float, float> ease)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                t.localScale = Vector3.LerpUnclamped(from, to, ease(elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localScale = to;
        }
    }
}
