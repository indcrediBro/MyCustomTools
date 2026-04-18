using System.Collections;
using UnityEngine;

namespace JUICE
{
    public enum AnimParamType
    {
        Trigger,
        Bool,
        Int,
        Float
    }

    /// <summary>
    /// Sets a parameter on an Animator when played.
    /// Supports Trigger, Bool, Int, and Float parameter types.
    /// </summary>
    [System.Serializable]
    public class AnimationParameterFeedback : Feedback
    {
        public override string DefaultLabel => "💃 Animation Parameter";

        [Header("Target")] [Tooltip("The Animator to control. Falls back to the owner's Animator.")]
        public Animator TargetAnimator;

        [Header("Parameter")] public string ParameterName = "Hit";
        public AnimParamType ParameterType = AnimParamType.Trigger;

        [Header("Values")] [Tooltip("Used for Bool type.")]
        public bool BoolValue = true;

        [Tooltip("Used for Int type.")] public int IntValue = 1;
        [Tooltip("Used for Float type.")] public float FloatValue = 1f;

        [Header("Reset Bool After Delay")] [Tooltip("For Bool type: optionally reset to false after a delay.")]
        public bool ResetBoolAfterDelay = false;

        [Min(0f)] public float ResetDelay = 0.1f;

        protected override IEnumerator Play(GameObject owner)
        {
            Animator anim = TargetAnimator != null ? TargetAnimator : owner.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogWarning("[JUICE] AnimParam: no Animator found.");
                yield break;
            }

            switch (ParameterType)
            {
                case AnimParamType.Trigger: anim.SetTrigger(ParameterName); break;
                case AnimParamType.Bool:
                    anim.SetBool(ParameterName, BoolValue);
                    if (ResetBoolAfterDelay)
                    {
                        yield return new WaitForSeconds(ResetDelay);
                        anim.SetBool(ParameterName, !BoolValue);
                    }

                    break;
                case AnimParamType.Int: anim.SetInteger(ParameterName, IntValue); break;
                case AnimParamType.Float: anim.SetFloat(ParameterName, FloatValue); break;
            }

            yield return null;
        }
    }
}