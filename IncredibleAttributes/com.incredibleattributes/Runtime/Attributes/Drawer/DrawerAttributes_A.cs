using System;
using UnityEngine;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ANIMATOR PARAM
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows a dropdown of all Animator parameters from a named Animator field.
    /// Works on <c>int</c> (param hash) and <c>string</c> (param name) fields.
    /// <code>
    /// public Animator anim;
    /// [AnimatorParam("anim")] public int runHash;
    /// [AnimatorParam("anim")] public string jumpParam;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AnimatorParamAttribute : PropertyAttribute
    {
        public string AnimatorName { get; }
        public AnimatorControllerParameterType? ParameterType { get; }

        public AnimatorParamAttribute(string animatorName)
            => AnimatorName = animatorName;

        public AnimatorParamAttribute(string animatorName, AnimatorControllerParameterType parameterType)
        {
            AnimatorName = animatorName;
            ParameterType = parameterType;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BUTTON
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a button in the Inspector that invokes the decorated method when clicked.
    /// Works on instance and static methods with zero parameters.
    /// <code>
    /// [Button]
    /// private void DoSomething() { }
    ///
    /// [Button("Reset Health", EButtonEnableMode.Playmode)]
    /// private void ResetHealth() { }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public EButtonEnableMode EnableMode { get; }

        public ButtonAttribute(EButtonEnableMode enableMode = EButtonEnableMode.Always)
            => EnableMode = enableMode;

        public ButtonAttribute(string label, EButtonEnableMode enableMode = EButtonEnableMode.Always)
        {
            Label      = label;
            EnableMode = enableMode;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CURVE RANGE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Constrains an AnimationCurve field to a specific rectangle and optional colour.
    /// <code>
    /// [CurveRange(0, 0, 1, 1, EColor.Green)]
    /// public AnimationCurve curve;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class CurveRangeAttribute : PropertyAttribute
    {
        public Rect Range  { get; }
        public Color Color { get; }

        public CurveRangeAttribute(EColor color = EColor.Green)
        {
            Range = new Rect(0, 0, 1, 1);
            Color = color.ToColor();
        }

        public CurveRangeAttribute(float xMin, float yMin, float xMax, float yMax,
                                   EColor color = EColor.Green)
        {
            Range = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            Color = color.ToColor();
        }
    }
}
