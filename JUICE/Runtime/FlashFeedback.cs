using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JUICE
{
    /// <summary>
    /// Flashes a color on screen by briefly showing a fullscreen UI Image overlay,
    /// then fading it back out. Great for hit impacts, pickups, and transitions.
    /// Assign a target Image, or leave empty to auto-create a Canvas overlay.
    /// </summary>
    [System.Serializable]
    public class FlashFeedback : Feedback
    {
        public override string DefaultLabel => "⚡ Flash";

        [Header("Target")]
        [Tooltip("The UI Image to flash. If empty, a temporary fullscreen overlay is created automatically.")]
        public Image TargetImage;

        [Header("Flash Settings")] public Color FlashColor = new Color(1f, 1f, 1f, 0.6f);
        [Min(0.01f)] public float FlashDuration = 0.05f;
        [Min(0.01f)] public float FadeOutDuration = 0.15f;

        protected override IEnumerator Play(GameObject owner)
        {
            Image img = TargetImage;
            GameObject tempGO = null;

            if (img == null)
            {
                tempGO = CreateOverlay();
                img = tempGO.GetComponent<Image>();
            }

            if (img == null) yield break;

            bool wasActive = img.gameObject.activeSelf;
            img.gameObject.SetActive(true);
            img.raycastTarget = false;

            // Flash in instantly
            img.color = FlashColor;
            yield return new WaitForSeconds(FlashDuration);

            // Fade out
            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                float t = 1f - (elapsed / FadeOutDuration);
                img.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, FlashColor.a * t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            img.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f);

            if (tempGO != null)
                Object.Destroy(tempGO);
            else
                img.gameObject.SetActive(wasActive);
        }

        private GameObject CreateOverlay()
        {
            // Find or create a root canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : null;

            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("JUICE_FlashCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                parent = canvasGO.transform;
            }

            GameObject go = new GameObject("JUICE_FlashOverlay");
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            go.transform.SetAsLastSibling();
            return go;
        }
    }
}
