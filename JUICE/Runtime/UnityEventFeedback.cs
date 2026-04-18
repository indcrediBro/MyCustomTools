using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace JUICE
{
    /// <summary>
    /// Triggers a UnityEvent when played. Useful for hooking up any arbitrary logic
    /// without writing a custom feedback — just wire it up in the inspector.
    /// </summary>
    [System.Serializable]
    public class UnityEventFeedback : Feedback
    {
        public override string DefaultLabel => "📅 Unity Event";

        [Header("Event")] public UnityEvent OnPlay = new UnityEvent();

        protected override IEnumerator Play(GameObject owner)
        {
            OnPlay?.Invoke();
            yield return null;
        }
    }
}