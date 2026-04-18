using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Shakes the main camera (or a specified camera) using Perlin noise.
    /// Supports positional and rotational shake with smooth falloff.
    /// </summary>
    [System.Serializable]
    public class CameraShakeFeedback : Feedback
    {
        public override string DefaultLabel => "📷 Camera Shake";

        [Header("Target")] [Tooltip("Camera to shake. Uses Camera.main if left empty.")]
        public GameObject TargetCamera;

        [Header("Shake")] [Min(0f)] public float Duration = 0.3f;
        [Min(0f)] public float PositionAmplitude = 0.2f;
        [Min(0f)] public float RotationAmplitude = 2f;
        [Min(0f)] public float Frequency = 25f;
        public bool Falloff = true;

        protected override IEnumerator Play(GameObject owner)
        {
            GameObject cam = TargetCamera != null ? TargetCamera : Camera.main.gameObject;
            if (cam == null)
            {
                Debug.LogWarning("[JUICE] CameraShake: no camera found.");
                yield break;
            }

            Transform t = cam.transform;
            Vector3 origPos = t.localPosition;
            Quaternion origRot = t.localRotation;
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);

            while (elapsed < Duration)
            {
                float strength = Falloff ? (1f - elapsed / Duration) : 1f;
                float nx = (Mathf.PerlinNoise(seed + elapsed * Frequency, 0f) - 0.5f) * 2f;
                float ny = (Mathf.PerlinNoise(0f, seed + elapsed * Frequency) - 0.5f) * 2f;
                float nz = (Mathf.PerlinNoise(seed + elapsed * Frequency, seed) - 0.5f) * 2f;

                if (PositionAmplitude > 0f)
                    t.localPosition = origPos + new Vector3(nx, ny, 0f) * PositionAmplitude * strength;
                if (RotationAmplitude > 0f)
                    t.localRotation = origRot * Quaternion.Euler(ny * RotationAmplitude * strength,
                        nx * RotationAmplitude * strength, nz * RotationAmplitude * strength);

                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localPosition = origPos;
            t.localRotation = origRot;
        }
    }
}
