using System.Collections;
using UnityEngine;

namespace JUICE
{
    public enum RotationMode
    {
        Additive,
        ToDestination,
        Continuous,
        Shake
    }

    /// <summary>
    /// Animates a transform's rotation.
    /// Additive: adds to current rotation then returns. ToDestination: rotates to a target.
    /// Continuous: spins at a set speed for a duration. Shake: rapidly oscillates the rotation.
    /// </summary>
    [System.Serializable]
    public class RotationFeedback : Feedback
    {
        public override string DefaultLabel => "🔄 Rotation";

        [Header("Target")] public Transform Target;

        [Header("Mode")] public RotationMode Mode = RotationMode.Additive;

        [Header("Additive")] public Vector3 AddRotation = new Vector3(0f, 0f, 15f);
        [Min(0.01f)] public float AddDuration = 0.1f;
        public bool ReturnAfterAdd = true;
        [Min(0.01f)] public float AddReturnDuration = 0.2f;

        [Header("To Destination")] public Transform DestinationTransform;
        [Min(0.01f)] public float DestDuration = 0.3f;
        public AnimationCurve DestCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Continuous")] public Vector3 SpinAxis = Vector3.forward;
        [Min(0f)] public float SpinSpeed = 360f;
        [Min(0.01f)] public float SpinDuration = 0.5f;

        [Header("Shake")] public Vector3 ShakeAmplitude = new Vector3(0f, 0f, 10f);
        [Min(0f)] public float ShakeFrequency = 20f;
        [Min(0.01f)] public float ShakeDuration = 0.3f;
        public bool ShakeFalloff = true;

        protected override IEnumerator Play(GameObject owner)
        {
            Transform t = Target != null ? Target : owner.transform;
            switch (Mode)
            {
                case RotationMode.Additive: yield return PlayAdditive(t); break;
                case RotationMode.ToDestination: yield return PlayToDestination(t); break;
                case RotationMode.Continuous: yield return PlayContinuous(t); break;
                case RotationMode.Shake: yield return PlayShake(t); break;
            }
        }

        private IEnumerator PlayAdditive(Transform t)
        {
            Quaternion original = t.localRotation;
            Quaternion target = original * Quaternion.Euler(AddRotation);
            float elapsed = 0f;
            while (elapsed < AddDuration)
            {
                t.localRotation = Quaternion.SlerpUnclamped(original, target, EaseOutCubic(elapsed / AddDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localRotation = target;
            if (ReturnAfterAdd)
            {
                elapsed = 0f;
                while (elapsed < AddReturnDuration)
                {
                    t.localRotation =
                        Quaternion.SlerpUnclamped(target, original, EaseOutElastic(elapsed / AddReturnDuration));
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                t.localRotation = original;
            }
        }

        private IEnumerator PlayToDestination(Transform t)
        {
            if (DestinationTransform == null)
            {
                Debug.LogWarning("[JUICE] Rotation: Destination not set.");
                yield break;
            }

            Quaternion from = t.rotation;
            Quaternion to = DestinationTransform.rotation;
            float elapsed = 0f;
            while (elapsed < DestDuration)
            {
                t.rotation = Quaternion.SlerpUnclamped(from, to, DestCurve.Evaluate(elapsed / DestDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.rotation = to;
        }

        private IEnumerator PlayContinuous(Transform t)
        {
            float elapsed = 0f;
            while (elapsed < SpinDuration)
            {
                t.Rotate(SpinAxis.normalized * SpinSpeed * Time.deltaTime, Space.Self);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PlayShake(Transform t)
        {
            Quaternion original = t.localRotation;
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);
            while (elapsed < ShakeDuration)
            {
                float strength = ShakeFalloff ? 1f - (elapsed / ShakeDuration) : 1f;
                float nx = (Mathf.PerlinNoise(seed + elapsed * ShakeFrequency, 0f) - 0.5f) * 2f;
                float ny = (Mathf.PerlinNoise(0f, seed + elapsed * ShakeFrequency) - 0.5f) * 2f;
                float nz = (Mathf.PerlinNoise(seed, seed + elapsed * ShakeFrequency) - 0.5f) * 2f;
                t.localRotation = original *
                                  Quaternion.Euler(new Vector3(nx * ShakeAmplitude.x, ny * ShakeAmplitude.y,
                                      nz * ShakeAmplitude.z) * strength);
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localRotation = original;
        }
    }
}