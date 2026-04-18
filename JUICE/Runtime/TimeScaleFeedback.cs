using System.Collections;
using UnityEngine;

namespace JUICE
{
    public enum TimeScaleMode
    {
        FreezeFrame,
        SlowMotion,
        Custom
    }

    /// <summary>
    /// Controls Time.timeScale.
    /// FreezeFrame: instantly snaps to 0 for a short duration then restores.
    /// SlowMotion: smoothly reduces timescale and restores.
    /// Custom: animate between two arbitrary timescale values.
    /// Note: Uses WaitForSecondsRealtime internally so the feedback itself isn't affected.
    /// </summary>
    [System.Serializable]
    public class TimeScaleFeedback : Feedback
    {
        public override string DefaultLabel => "⌚ Time Scale";

        [Header("Mode")] public TimeScaleMode Mode = TimeScaleMode.FreezeFrame;

        [Header("Freeze Frame")] [Tooltip("Duration of the freeze in real seconds.")] [Min(0f)]
        public float FreezeDuration = 0.05f;

        [Header("Slow Motion")] [Range(0f, 1f)]
        public float SlowMotionScale = 0.1f;

        [Min(0f)] public float SlowDuration = 0.5f;
        [Min(0.01f)] public float SlowReturnDuration = 0.3f;

        [Header("Custom")] public float FromTimeScale = 1f;
        public float ToTimeScale = 0.5f;
        [Min(0.01f)] public float CustomDuration = 0.5f;
        public bool RestoreTimeScale = true;
        public AnimationCurve CustomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        protected override IEnumerator Play(GameObject owner)
        {
            float originalTimeScale = Time.timeScale;

            switch (Mode)
            {
                case TimeScaleMode.FreezeFrame:
                    Time.timeScale = 0f;
                    yield return new WaitForSecondsRealtime(FreezeDuration);
                    Time.timeScale = originalTimeScale;
                    break;

                case TimeScaleMode.SlowMotion:
                    Time.timeScale = SlowMotionScale;
                    yield return new WaitForSecondsRealtime(SlowDuration);
                    float elapsed = 0f;
                    while (elapsed < SlowReturnDuration)
                    {
                        Time.timeScale = Mathf.Lerp(SlowMotionScale, originalTimeScale, elapsed / SlowReturnDuration);
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    Time.timeScale = originalTimeScale;
                    break;

                case TimeScaleMode.Custom:
                    elapsed = 0f;
                    while (elapsed < CustomDuration)
                    {
                        Time.timeScale = Mathf.LerpUnclamped(FromTimeScale, ToTimeScale,
                            CustomCurve.Evaluate(elapsed / CustomDuration));
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    if (RestoreTimeScale)
                        Time.timeScale = originalTimeScale;
                    else
                        Time.timeScale = ToTimeScale;
                    break;
            }
        }
    }
}