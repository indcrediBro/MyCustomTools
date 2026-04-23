using System;
using UnityEngine;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  GUI COLOR  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tints the field row with a given colour. Useful for visually grouping
    /// or highlighting important fields.
    /// <code>
    /// [GUIColor(EColor.Red)]
    /// public float health;
    ///
    /// [GUIColor(1f, 0.8f, 0f)]
    /// public int gold;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class GUIColorAttribute : Attribute
    {
        public Color Color { get; }

        public GUIColorAttribute(EColor color)
            => Color = color.ToColor();

        public GUIColorAttribute(float r, float g, float b, float a = 1f)
            => Color = new Color(r, g, b, a);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HORIZONTAL LINE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a coloured horizontal line as a decorator above the field.
    /// <code>
    /// [HorizontalLine(EColor.Gray)]
    /// public int nextSection;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HorizontalLineAttribute : PropertyAttribute
    {
        public Color  Color     { get; }
        public float  Height    { get; }

        public HorizontalLineAttribute(EColor color = EColor.Gray, float height = 1f)
        {
            Color  = color.ToColor();
            Height = height;
        }

        public HorizontalLineAttribute(float r, float g, float b, float height = 1f)
        {
            Color  = new Color(r, g, b);
            Height = height;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INFO BOX
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws an informational HelpBox above the field.
    /// <code>
    /// [InfoBox("Speed is in units per second.", EInfoBoxType.Normal)]
    /// public float speed;
    ///
    /// [InfoBox("showWarning")]          // name of a bool field/property/method
    /// [InfoBox("Caution!", EInfoBoxType.Warning, "showWarning")]
    /// public float dangerZone;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public string        Text        { get; }
        public EInfoBoxType  Type        { get; }
        /// <summary>
        /// Optional name of a bool field/property/method that controls visibility of this box.
        /// Leave null to always show.
        /// </summary>
        public string        ConditionName { get; }

        public InfoBoxAttribute(string text, EInfoBoxType type = EInfoBoxType.Normal,
                                string conditionName = null)
        {
            Text          = text;
            Type          = type;
            ConditionName = conditionName;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INPUT AXIS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces a <c>string</c> field with a dropdown of axes from the legacy
    /// Input Manager (Edit → Project Settings → Input).
    /// <code>
    /// [InputAxis]
    /// public string moveAxis;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAxisAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  LAYER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces an <c>int</c> or <c>string</c> field with a layer-name dropdown.
    /// <code>
    /// [Layer] public int groundLayer;
    /// [Layer] public string enemyLayerName;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LayerAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  MIN MAX SLIDER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a two-handle slider. X = min value, Y = max value.
    /// <code>
    /// [MinMaxSlider(0f, 100f)]
    /// public Vector2 spawnDelay;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxSliderAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public MinMaxSliderAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
