using System.Collections;
using UnityEngine;

namespace JUICE
{
    public enum PositionMode
    {
        ShakePosition,
        MoveToDestination,
        MoveAToB
    }

    /// <summary>
    /// Animates a transform's position. Three modes:
    /// ShakePosition: randomly shakes in place (like camera shake, but for any object).
    /// MoveToDestination: moves from current position to a target transform's position.
    /// MoveAToB: moves between two explicit world positions.
    /// </summary>
    [System.Serializable]
    public class PositionFeedback : Feedback
    {
        public override string DefaultLabel => "🐢 Position";

        [Header("Target")] public Transform Target;

        [Header("Mode")] public PositionMode Mode = PositionMode.ShakePosition;

        [Header("Shake Settings")] [Min(0f)] public float ShakeDuration = 0.3f;
        public Vector3 ShakeAmplitude = new Vector3(0.1f, 0.1f, 0f);
        [Min(0f)] public float ShakeFrequency = 20f;
        public bool ShakeFalloff = true;

        [Header("Move Settings")] [Tooltip("Destination transform (used in MoveToDestination mode).")]
        public Transform Destination;

        [Tooltip("From position (used in MoveAToB mode, world space).")]
        public Vector3 PositionA = Vector3.zero;

        [Tooltip("To position (used in MoveAToB mode, world space).")]
        public Vector3 PositionB = Vector3.one;

        [Min(0.01f)] public float MoveDuration = 0.3f;
        public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool ReturnToOriginAfterMove = true;
        [Min(0.01f)] public float ReturnDuration = 0.3f;

        protected override IEnumerator Play(GameObject owner)
        {
            Transform t = Target != null ? Target : owner.transform;

            switch (Mode)
            {
                case PositionMode.ShakePosition: yield return Shake(t); break;
                case PositionMode.MoveToDestination: yield return MoveToTarget(t); break;
                case PositionMode.MoveAToB: yield return MoveAtoB(t); break;
            }
        }

        private IEnumerator Shake(Transform t)
        {
            Vector3 origin = t.position;
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);

            while (elapsed < ShakeDuration)
            {
                float strength = ShakeFalloff ? 1f - (elapsed / ShakeDuration) : 1f;
                float nx = (Mathf.PerlinNoise(seed + elapsed * ShakeFrequency, 0f) - 0.5f) * 2f;
                float ny = (Mathf.PerlinNoise(0f, seed + elapsed * ShakeFrequency) - 0.5f) * 2f;
                float nz = (Mathf.PerlinNoise(seed, seed + elapsed * ShakeFrequency) - 0.5f) * 2f;
                t.position = origin + new Vector3(nx * ShakeAmplitude.x, ny * ShakeAmplitude.y, nz * ShakeAmplitude.z) *
                    strength;
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.position = origin;
        }

        private IEnumerator MoveToTarget(Transform t)
        {
            if (Destination == null)
            {
                Debug.LogWarning("[JUICE] Position: Destination not assigned.");
                yield break;
            }

            Vector3 origin = t.position;
            Vector3 dest = Destination.position;
            yield return AnimateMove(t, origin, dest);
            if (ReturnToOriginAfterMove)
                yield return AnimateMove(t, dest, origin);
        }

        private IEnumerator MoveAtoB(Transform t)
        {
            yield return AnimateMove(t, PositionA, PositionB);
            if (ReturnToOriginAfterMove)
                yield return AnimateMove(t, PositionB, PositionA);
        }

        private IEnumerator AnimateMove(Transform t, Vector3 from, Vector3 to)
        {
            float elapsed = 0f;
            while (elapsed < MoveDuration)
            {
                t.position = Vector3.LerpUnclamped(from, to, MoveCurve.Evaluate(elapsed / MoveDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            t.position = to;
        }
    }
}