using System;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  BOX GROUP
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Groups consecutive fields with this attribute name into a labelled box.
    /// <code>
    /// [BoxGroup("Movement")]
    /// public float speed;
    /// [BoxGroup("Movement")]
    /// public float jumpHeight;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BoxGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public BoxGroupAttribute(string groupName = "") => GroupName = groupName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FOLDOUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Groups consecutive fields into a collapsible foldout.
    /// <code>
    /// [Foldout("Debug Info")]
    /// public int debugSeed;
    /// [Foldout("Debug Info")]
    /// public bool debugDraw;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldoutAttribute : Attribute
    {
        public string FoldoutName { get; }
        public FoldoutAttribute(string foldoutName) => FoldoutName = foldoutName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ENABLE IF / DISABLE IF
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enables the field only when the named condition(s) are true.
    /// The condition can be a bool field, bool property, or zero-param bool method.
    /// <code>
    /// public bool useDamageMultiplier;
    /// [EnableIf("useDamageMultiplier")]
    /// public float damageMultiplier;
    ///
    /// // Multiple conditions:
    /// [EnableIf(EConditionOperator.And, "isAlive", "hasWeapon")]
    /// public float attackPower;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnableIfAttribute : Attribute
    {
        public string[]            Conditions { get; }
        public EConditionOperator  Operator   { get; }

        public EnableIfAttribute(string condition)
        {
            Conditions = new[] { condition };
            Operator   = EConditionOperator.And;
        }

        public EnableIfAttribute(EConditionOperator op, params string[] conditions)
        {
            Conditions = conditions;
            Operator   = op;
        }
    }

    /// <summary>
    /// Disables the field when the named condition(s) are true.
    /// <code>
    /// [DisableIf("isLocked")]
    /// public float value;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisableIfAttribute : Attribute
    {
        public string[]            Conditions { get; }
        public EConditionOperator  Operator   { get; }

        public DisableIfAttribute(string condition)
        {
            Conditions = new[] { condition };
            Operator   = EConditionOperator.And;
        }

        public DisableIfAttribute(EConditionOperator op, params string[] conditions)
        {
            Conditions = conditions;
            Operator   = op;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SHOW IF / HIDE IF
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the field only when the named condition(s) are true.
    /// <code>
    /// public bool hasShield;
    /// [ShowIf("hasShield")]
    /// public float shieldStrength;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : Attribute
    {
        public string[]            Conditions { get; }
        public EConditionOperator  Operator   { get; }

        public ShowIfAttribute(string condition)
        {
            Conditions = new[] { condition };
            Operator   = EConditionOperator.And;
        }

        public ShowIfAttribute(EConditionOperator op, params string[] conditions)
        {
            Conditions = conditions;
            Operator   = op;
        }
    }

    /// <summary>
    /// Hides the field when the named condition(s) are true.
    /// <code>
    /// [HideIf("hideDebugInfo")]
    /// public int debugValue;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HideIfAttribute : Attribute
    {
        public string[]            Conditions { get; }
        public EConditionOperator  Operator   { get; }

        public HideIfAttribute(string condition)
        {
            Conditions = new[] { condition };
            Operator   = EConditionOperator.And;
        }

        public HideIfAttribute(EConditionOperator op, params string[] conditions)
        {
            Conditions = conditions;
            Operator   = op;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LABEL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Overrides the display name of the field in the inspector.
    /// <code>
    /// [Label("Max HP")]
    /// public float maxHealthPoints;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAttribute : Attribute
    {
        public string Text { get; }
        public LabelAttribute(string text) => Text = text;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ON VALUE CHANGED
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls a method whenever the field value changes in the Inspector.
    /// The callback can be parameterless OR accept the new value as its first argument.
    /// <code>
    /// [OnValueChanged("OnSpeedChanged")]
    /// public float speed;
    ///
    /// private void OnSpeedChanged() { Debug.Log("Speed changed to " + speed); }
    /// // or
    /// private void OnSpeedChanged(float newSpeed) { ... }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OnValueChangedAttribute : Attribute
    {
        public string CallbackName { get; }
        public OnValueChangedAttribute(string callbackName) => CallbackName = callbackName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  READ ONLY
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the field greyed-out (not editable) in the Inspector while still
    /// displaying its current value.
    /// <code>
    /// [ReadOnly]
    /// public Vector3 velocity;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : Attribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  BUTTON GROUP  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Groups method buttons horizontally in a single row. Give multiple methods
    /// the same group name and they will share a row.
    /// <code>
    /// [ButtonGroup("Manage")]
    /// private void SpawnEnemy() { }
    ///
    /// [ButtonGroup("Manage")]
    /// private void KillAll() { }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public string Label     { get; }

        public ButtonGroupAttribute(string groupName = "Default", string label = null)
        {
            GroupName = groupName;
            Label     = label;
        }
    }
}
