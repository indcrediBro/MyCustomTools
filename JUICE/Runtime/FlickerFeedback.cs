using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Rapidly flickers a Renderer's material color between its original color and a target color.
    /// Great for damage flashes, invincibility blinks, and impact effects.
    /// </summary>
    [System.Serializable]
    public class FlickerFeedback : Feedback
    {
        public override string DefaultLabel => "💄 Flicker";

        [Header("Target")]
        [Tooltip("The Renderer whose material color will be flickered. Falls back to the owner's Renderer.")]
        public Renderer TargetRenderer;

        [Tooltip("The shader property name to modify. Default works for Sprites and Standard materials.")]
        public string ColorPropertyName = "_Color";

        [Header("Flicker Settings")] [Tooltip("The color to flicker to (e.g. white for a damage flash).")]
        public Color FlickerColor = Color.white;

        [Min(0f)] public float Duration = 0.5f;

        [Tooltip("How many color switches per second.")] [Min(1f)]
        public float Frequency = 20f;

        protected override IEnumerator Play(GameObject owner)
        {
            Renderer rend = TargetRenderer != null ? TargetRenderer : owner.GetComponent<Renderer>();
            if (rend == null)
            {
                Debug.LogWarning("[JUICE] Flicker: no Renderer found.");
                yield break;
            }

            // Use MaterialPropertyBlock to avoid creating new material instances
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            Color originalColor = rend.sharedMaterial.HasProperty(ColorPropertyName)
                ? rend.sharedMaterial.GetColor(ColorPropertyName)
                : Color.white;

            float elapsed = 0f;
            float interval = 1f / Frequency;
            bool flickerState = false;

            while (elapsed < Duration)
            {
                flickerState = !flickerState;
                block.SetColor(ColorPropertyName, flickerState ? FlickerColor : originalColor);
                rend.SetPropertyBlock(block);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            // Restore original
            block.SetColor(ColorPropertyName, originalColor);
            rend.SetPropertyBlock(block);
        }
    }
}