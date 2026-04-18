using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUICE
{
    /// <summary>
    /// Add this component to any GameObject to manage and trigger a stack of feedbacks.
    /// Call Play() from code, or use the ▶ button in the inspector during Play Mode.
    /// </summary>
    [AddComponentMenu("JUICE/Juice Player")]
    public class JuicePlayer : MonoBehaviour
    {
        [Tooltip("All feedbacks in this list are triggered when Play() is called.")] [SerializeReference]
        public List<Feedback> Feedbacks = new List<Feedback>();

        [Tooltip("Automatically play all feedbacks when the scene starts.")]
        public bool PlayOnAwake = false;

        [Tooltip("Run feedbacks one after another instead of all simultaneously.")]
        public bool Sequential = false;

        private bool _isPlaying = false;
        public bool IsPlaying => _isPlaying;

        private void Start()
        {
            if (PlayOnAwake) Play();
        }

        /// <summary>Triggers all enabled feedbacks.</summary>
        public void Play()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(PlayRoutine());
        }

        /// <summary>Stops all running feedbacks immediately.</summary>
        public void Stop()
        {
            StopAllCoroutines();
            _isPlaying = false;
        }

        private IEnumerator PlayRoutine()
        {
            _isPlaying = true;

            if (Sequential)
            {
                foreach (var feedback in Feedbacks)
                    if (feedback != null && feedback.Enabled)
                        yield return StartCoroutine(feedback.Execute(this));
            }
            else
            {
                foreach (var feedback in Feedbacks)
                    if (feedback != null && feedback.Enabled)
                        StartCoroutine(feedback.Execute(this));
            }

            _isPlaying = false;
        }
    }
}
