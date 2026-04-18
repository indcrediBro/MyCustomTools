using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUICE
{
    public enum SetActiveMode
    {
        SetActive,
        SetInactive,
        Toggle,
        PulseActive
    }

    /// <summary>
    /// Sets one or more GameObjects active or inactive.
    /// PulseActive: briefly activates the object then deactivates it again (useful for one-shot effects).
    /// </summary>
    [System.Serializable]
    public class SetActiveFeedback : Feedback
    {
        public override string DefaultLabel => "📦 Set Active";

        [Header("Targets")] public List<GameObject> Targets = new List<GameObject>();

        [Header("Mode")] public SetActiveMode Mode = SetActiveMode.SetActive;

        [Header("Pulse Settings")]
        [Tooltip("How long to keep the object active before deactivating it again (PulseActive only).")]
        [Min(0f)]
        public float PulseDuration = 0.1f;

        protected override IEnumerator Play(GameObject owner)
        {
            if (Targets == null || Targets.Count == 0)
            {
                Debug.LogWarning("[JUICE] SetActive: no targets assigned.");
                yield break;
            }

            foreach (var target in Targets)
            {
                if (target == null) continue;
                switch (Mode)
                {
                    case SetActiveMode.SetActive: target.SetActive(true); break;
                    case SetActiveMode.SetInactive: target.SetActive(false); break;
                    case SetActiveMode.Toggle: target.SetActive(!target.activeSelf); break;
                    case SetActiveMode.PulseActive:
                        target.SetActive(true);
                        break;
                }
            }

            if (Mode == SetActiveMode.PulseActive)
            {
                yield return new WaitForSeconds(PulseDuration);
                foreach (var target in Targets)
                {
                    if (target != null)
                        target.SetActive(false);
                }
            }

            yield return null;
        }
    }
}
