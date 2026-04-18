using System.Collections;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Triggers a particle system. Either plays an existing one in the scene,
    /// or instantiates a prefab at a specified position with optional auto-destroy.
    /// </summary>
    [System.Serializable]
    public class ParticlesFeedback : Feedback
    {
        public override string DefaultLabel => "✨ Particles";

        [Header("Existing System")] [Tooltip("Play this particle system (already in the scene).")]
        public ParticleSystem TargetParticles;

        public bool StopAndResetBeforePlaying = true;

        [Header("Instantiate Prefab")]
        [Tooltip("Instantiate this prefab instead (overrides Existing System if assigned).")]
        public GameObject ParticlePrefab;

        public Transform SpawnPosition;
        public bool AutoDestroy = true;
        [Min(0f)] public float AutoDestroyBuffer = 0.5f;
        [Min(0.01f)] public float ScaleMultiplier = 1f;

        protected override IEnumerator Play(GameObject owner)
        {
            if (ParticlePrefab != null)
            {
                yield return InstantiateParticles(owner);
            }
            else if (TargetParticles != null)
            {
                if (StopAndResetBeforePlaying)
                    TargetParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                TargetParticles.Play(true);
            }
            else
            {
                Debug.LogWarning("[JUICE] Particles: no ParticleSystem or Prefab assigned.");
            }

            yield return null;
        }

        private IEnumerator InstantiateParticles(GameObject owner)
        {
            Vector3 pos = SpawnPosition != null ? SpawnPosition.position : owner.transform.position;
            Quaternion rot = SpawnPosition != null ? SpawnPosition.rotation : owner.transform.rotation;

            GameObject instance = Object.Instantiate(ParticlePrefab, pos, rot);
            instance.transform.localScale *= ScaleMultiplier;

            ParticleSystem ps = instance.GetComponent<ParticleSystem>() ??
                                instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(true);
                if (AutoDestroy)
                {
                    float lifetime = ps.main.duration + ps.main.startLifetime.constantMax + AutoDestroyBuffer;
                    Object.Destroy(instance, lifetime);
                }
            }

            yield return null;
        }
    }
}
