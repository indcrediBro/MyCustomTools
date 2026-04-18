using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Fades a CanvasGroup's alpha in or out over time.
    /// Useful for showing/hiding UI panels, popups, tooltips, and damage overlays.
    /// </summary>
    [System.Serializable]
    public class CanvasGroupFeedback : Feedback
    {
        public override string DefaultLabel => "🌶 Canvas Group";

        [Header("Target")] public CanvasGroup TargetCanvasGroup;

        [Header("Alpha")] [Range(0f, 1f)] public float FromAlpha = 1f;
        [Range(0f, 1f)] public float ToAlpha = 0f;
        [Min(0.01f)] public float Duration = 0.3f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool RestoreAlpha = true;
        [Min(0.01f)] public float RestoreDuration = 0.3f;

        [Header("Interaction")] [Tooltip("Sets interactable and blocksRaycasts to false while faded out.")]
        public bool DisableInteractionWhenFaded = true;

        protected override IEnumerator Play(GameObject owner)
        {
            CanvasGroup cg = TargetCanvasGroup != null ? TargetCanvasGroup : owner.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                Debug.LogWarning("[JUICE] CanvasGroup: no CanvasGroup found.");
                yield break;
            }

            float originalAlpha = cg.alpha;

            yield return AnimateAlpha(cg, FromAlpha, ToAlpha, Duration);

            if (DisableInteractionWhenFaded && ToAlpha < 0.01f)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            if (RestoreAlpha)
            {
                if (DisableInteractionWhenFaded)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                yield return AnimateAlpha(cg, ToAlpha, originalAlpha, RestoreDuration);
            }
        }

        private IEnumerator AnimateAlpha(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                cg.alpha = Mathf.LerpUnclamped(from, to, Curve.Evaluate(elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            cg.alpha = to;
        }
    }
}
