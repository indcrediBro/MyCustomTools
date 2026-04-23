using System;
using UnityEngine;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  PROGRESS BAR
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a filled bar instead of a plain number field.
    /// <code>
    /// [ProgressBar("Health", 100f, EColor.Red)]
    /// public float hp = 75f;
    ///
    /// // Dynamic max from another field or method:
    /// [ProgressBar("Mana", "maxMana", EColor.Blue)]
    /// public float mana;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ProgressBarAttribute : PropertyAttribute
    {
        public string  Label      { get; }
        public float   MaxValue   { get; }
        /// <summary>Name of a field/property/method that returns the max value (overrides MaxValue).</summary>
        public string  MaxValueName { get; }
        public Color   Color      { get; }

        public ProgressBarAttribute(string label, float maxValue, EColor color = EColor.Green)
        {
            Label    = label;
            MaxValue = maxValue;
            Color    = color.ToColor();
        }

        /// <summary>Use a field/property/method for dynamic max value.</summary>
        public ProgressBarAttribute(string label, string maxValueName, EColor color = EColor.Green)
        {
            Label        = label;
            MaxValueName = maxValueName;
            Color        = color.ToColor();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  REORDERABLE LIST
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws an array or List with a polished reorderable list UI with drag
    /// handles, add/remove buttons, and consistent spacing.
    /// <code>
    /// [ReorderableList]
    /// public string[] weapons;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReorderableListAttribute : Attribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  RESIZABLE TEXT AREA
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the single-line string widget with a text area that grows to
    /// fit all of the text. Unlike Unity's built-in TextArea attribute there is
    /// no fixed row cap.
    /// <code>
    /// [ResizableTextArea]
    /// public string description;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ResizableTextAreaAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  SCENE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces a <c>string</c> (scene name) or <c>int</c> (build index) field
    /// with a dropdown populated from the scenes in Build Settings.
    /// <code>
    /// [Scene] public string mainMenuScene;
    /// [Scene] public int    gameplaySceneIndex;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  SHOW ASSET PREVIEW
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows a thumbnail preview below the object field.
    /// Works with Sprite, Texture, Mesh, GameObject, AudioClip, etc.
    /// <code>
    /// [ShowAssetPreview(64, 64)]
    /// public Sprite icon;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowAssetPreviewAttribute : PropertyAttribute
    {
        public int Width  { get; }
        public int Height { get; }

        public ShowAssetPreviewAttribute(int width = 64, int height = 64)
        {
            Width  = width;
            Height = height;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SHOW NATIVE PROPERTY
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Displays a read-only C# property (getter) in the inspector.
    /// Supports: bool, int, long, float, double, string, Vector2/3/4,
    /// Color, Bounds, Rect, UnityEngine.Object.
    /// <code>
    /// public List&lt;Transform&gt; enemies;
    /// [ShowNativeProperty]
    /// public int EnemyCount => enemies.Count;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ShowNativePropertyAttribute : Attribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  SHOW NON SERIALIZED FIELD
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Makes a normally hidden (non-serialized) field visible in the inspector
    /// as a read-only row. Supports the same types as ShowNativeProperty.
    /// Values update when you enter Play Mode.
    /// <code>
    /// [ShowNonSerializedField]
    /// private int _frameCount;
    ///
    /// [ShowNonSerializedField]
    /// private const float Gravity = -9.81f;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowNonSerializedFieldAttribute : Attribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  SORTING LAYER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces an <c>int</c> or <c>string</c> field with a sorting-layer dropdown.
    /// <code>
    /// [SortingLayer] public string layerName;
    /// [SortingLayer] public int    layerID;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SortingLayerAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREFIX / SUFFIX  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a static text label to the LEFT of the field widget (after the field name).
    /// Great for units or context: e.g. "€", "m/s", "px".
    /// <code>
    /// [Prefix("$")]
    /// public float price;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PrefixAttribute : Attribute
    {
        public string Text { get; }
        public PrefixAttribute(string text) => Text = text;
    }

    /// <summary>
    /// Draws a static text label to the RIGHT of the field widget.
    /// <code>
    /// [Suffix("m/s")]
    /// public float speed;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SuffixAttribute : Attribute
    {
        public string Text { get; }
        public SuffixAttribute(string text) => Text = text;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TAG
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces a <c>string</c> field with a Unity tag dropdown.
    /// <code>
    /// [Tag]
    /// public string enemyTag;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TagAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  TITLE  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a bold section title (and optional subtitle) as a decorator above the field.
    /// Cleaner than a plain HorizontalLine when you need a labelled section break.
    /// <code>
    /// [Title("Combat Settings")]
    /// public float damage;
    ///
    /// [Title("Audio", "Volumes are 0-1")]
    /// public float musicVolume;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TitleAttribute : PropertyAttribute
    {
        public string Text     { get; }
        public string Subtitle { get; }
        public bool   Line     { get; }

        public TitleAttribute(string text, string subtitle = null, bool line = true)
        {
            Text     = text;
            Subtitle = subtitle;
            Line     = line;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INDENT  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Indents the field by the specified number of levels (default 1).
    /// <code>
    /// public bool advancedMode;
    /// [Indent]
    /// public float advancedValue;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IndentAttribute : Attribute
    {
        public int Levels { get; }
        public IndentAttribute(int levels = 1) => Levels = levels;
    }
}
