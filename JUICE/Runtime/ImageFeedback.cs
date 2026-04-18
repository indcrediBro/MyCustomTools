using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JUICE
{
    public enum ImageFeedbackMode
    {
        Color,
        Alpha,
        Fill
    }

    /// <summary>
    /// Animates a UI Image's color, alpha, or fill amount over time.
    /// Great for health bars, cooldown indicators, hit flashes, and HUD effects.
    /// </summary>
    [System.Serializable]
    public class ImageFeedback : Feedback
    {
        public override string DefaultLabel => "🌶 Image";

        [Header("Target")] public Image TargetImage;

        [Header("Mode")] public ImageFeedbackMode Mode = ImageFeedbackMode.Alpha;

        [Header("Color")] public Color FromColor = Color.white;
        public Color ToColor = Color.red;
        [Min(0.01f)] public float ColorDuration = 0.2f;
        public bool RestoreColor = true;
        [Min(0.01f)] public float ColorRestoreDuration = 0.2f;

        [Header("Alpha")] [Range(0f, 1f)] public float FromAlpha = 1f;
        [Range(0f, 1f)] public float ToAlpha = 0f;
        [Min(0.01f)] public float AlphaDuration = 0.3f;
        public bool RestoreAlpha = true;
        [Min(0.01f)] public float AlphaRestoreDuration = 0.3f;

        [Header("Fill")] [Range(0f, 1f)] public float FromFill = 1f;
        [Range(0f, 1f)] public float ToFill = 0f;
        [Min(0.01f)] public float FillDuration = 0.5f;
        public AnimationCurve FillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool RestoreFill = false;
        [Min(0.01f)] public float FillRestoreDuration = 0.5f;

        protected override IEnumerator Play(GameObject owner)
        {
            if (TargetImage == null)
            {
                Debug.LogWarning("[JUICE] Image: no Image assigned.");
                yield break;
            }

            switch (Mode)
            {
                case ImageFeedbackMode.Color: yield return AnimateColor(); break;
                case ImageFeedbackMode.Alpha: yield return AnimateAlpha(); break;
                case ImageFeedbackMode.Fill: yield return AnimateFill(); break;
            }
        }

        private IEnumerator AnimateColor()
        {
            Color original = TargetImage.color;
            float elapsed = 0f;
            while (elapsed < ColorDuration)
            {
                TargetImage.color = Color.Lerp(FromColor, ToColor, EaseOutCubic(elapsed / ColorDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            TargetImage.color = ToColor;

            if (RestoreColor)
            {
                elapsed = 0f;
                while (elapsed < ColorRestoreDuration)
                {
                    TargetImage.color = Color.Lerp(ToColor, original, EaseOutCubic(elapsed / ColorRestoreDuration));
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                TargetImage.color = original;
            }
        }

        private IEnumerator AnimateAlpha()
        {
            Color c = TargetImage.color;
            float originalAlpha = c.a;
            float elapsed = 0f;
            while (elapsed < AlphaDuration)
            {
                c.a = Mathf.Lerp(FromAlpha, ToAlpha, EaseOutCubic(elapsed / AlphaDuration));
                TargetImage.color = c;
                elapsed += Time.deltaTime;
                yield return null;
            }

            c.a = ToAlpha;
            TargetImage.color = c;

            if (RestoreAlpha)
            {
                elapsed = 0f;
                while (elapsed < AlphaRestoreDuration)
                {
                    c.a = Mathf.Lerp(ToAlpha, originalAlpha, EaseOutCubic(elapsed / AlphaRestoreDuration));
                    TargetImage.color = c;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                c.a = originalAlpha;
                TargetImage.color = c;
            }
        }

        private IEnumerator AnimateFill()
        {
            float originalFill = TargetImage.fillAmount;
            float elapsed = 0f;
            while (elapsed < FillDuration)
            {
                TargetImage.fillAmount =
                    Mathf.LerpUnclamped(FromFill, ToFill, FillCurve.Evaluate(elapsed / FillDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            TargetImage.fillAmount = ToFill;

            if (RestoreFill)
            {
                elapsed = 0f;
                while (elapsed < FillRestoreDuration)
                {
                    TargetImage.fillAmount =
                        Mathf.Lerp(ToFill, originalFill, EaseOutCubic(elapsed / FillRestoreDuration));
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                TargetImage.fillAmount = originalFill;
            }
        }
    }
}
