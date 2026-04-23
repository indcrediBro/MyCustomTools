using System;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  MIN VALUE / MAX VALUE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clamps an int or float field to a minimum value in the Inspector.
    /// Can be combined with [MaxValue].
    /// <code>
    /// [MinValue(0f), MaxValue(100f)]
    /// public float health;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MinValueAttribute : PropertyAttribute
    {
        public float Value { get; }
        public MinValueAttribute(float min) => Value = min;
        public MinValueAttribute(int min)   => Value = min;
    }

    /// <summary>
    /// Clamps an int or float field to a maximum value in the Inspector.
    /// <code>
    /// [MaxValue(999)]
    /// public int score;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MaxValueAttribute : PropertyAttribute
    {
        public float Value { get; }
        public MaxValueAttribute(float max) => Value = max;
        public MaxValueAttribute(int max)   => Value = max;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  REQUIRED
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows an error message in the Inspector when a reference-type field is null.
    /// Useful as a reminder to assign a required dependency.
    /// <code>
    /// [Required]
    /// public Rigidbody rb;
    ///
    /// [Required("You must assign the player camera!")]
    /// public Camera playerCam;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiredAttribute : PropertyAttribute
    {
        public string Message { get; }

        public RequiredAttribute(string message = null) => Message = message;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VALIDATE INPUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls a method to validate the field's value and shows an error if it
    /// returns false. The callback must accept the field value as its parameter
    /// and return bool.
    /// <code>
    /// [ValidateInput("IsPositive", "Value must be greater than zero.")]
    /// public float multiplier;
    ///
    /// private bool IsPositive(float v) => v > 0f;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ValidateInputAttribute : PropertyAttribute
    {
        public string CallbackName { get; }
        public string Message      { get; }

        public ValidateInputAttribute(string callbackName, string message = "Invalid value.")
        {
            CallbackName = callbackName;
            Message      = message;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ALLOW NESTING
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opt-in marker for meta attributes (ShowIf, EnableIf, etc.) used inside
    /// nested serializable classes or structs.
    /// Without this the editor will not process meta attributes for nested fields.
    /// <code>
    /// [System.Serializable]
    /// public struct Nested
    /// {
    ///     public bool flag;
    ///
    ///     [ShowIf("flag")]
    ///     [AllowNesting]          // required because it's inside a struct
    ///     public float value;
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AllowNestingAttribute : Attribute { }
}
