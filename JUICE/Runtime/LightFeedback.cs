using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Animates a Light's intensity and/or color over time.
    /// Perfect for muzzle flashes, explosions, pickups, and environmental effects.
    /// </summary>
    [System.Serializable]
    public class LightFeedback : Feedback
    {
        public override string DefaultLabel => "💡 Light";

        [Header("Target")] [Tooltip("The Light to control. Falls back to the owner's Light component.")]
        public Light TargetLight;

        [Header("Intensity")] public bool AnimateIntensity = true;
        public float FromIntensity = 3f;
        public float ToIntensity = 0f;
        public AnimationCurve IntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [Min(0.01f)] public float Duration = 0.3f;
        public bool RestoreOriginalIntensity = true;

        [Header("Color")] public bool AnimateColor = false;
        public Color FromColor = Color.yellow;
        public Color ToColor = Color.red;
        public bool RestoreOriginalColor = true;

        protected override IEnumerator Play(GameObject owner)
        {
            Light light = TargetLight != null ? TargetLight : owner.GetComponent<Light>();
            if (light == null)
            {
                Debug.LogWarning("[JUICE] Light: no Light component found.");
                yield break;
            }

            float originalIntensity = light.intensity;
            Color originalColor = light.color;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                float t = elapsed / Duration;

                if (AnimateIntensity)
                    light.intensity = Mathf.LerpUnclamped(FromIntensity, ToIntensity, IntensityCurve.Evaluate(t));

                if (AnimateColor)
                    light.color = Color.Lerp(FromColor, ToColor, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (RestoreOriginalIntensity) light.intensity = originalIntensity;
            else light.intensity = ToIntensity;

            if (AnimateColor)
                light.color = RestoreOriginalColor ? originalColor : ToColor;
        }
    }
}
