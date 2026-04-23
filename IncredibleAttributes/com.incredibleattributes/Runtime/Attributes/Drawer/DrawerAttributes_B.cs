using System;
using UnityEngine;

namespace IncredibleAttributes
{
    // ─────────────────────────────────────────────────────────────────────────
    //  DROPDOWN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the default field widget with a dropdown populated by a field,
    /// property, or method that returns an array, List, or DropdownList.
    /// <code>
    /// [Dropdown("_difficultyOptions")]
    /// public int difficulty;
    ///
    /// private DropdownList&lt;int&gt; _difficultyOptions = new()
    /// {
    ///     { "Easy",   0 },
    ///     { "Normal", 1 },
    ///     { "Hard",   2 },
    /// };
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DropdownAttribute : PropertyAttribute
    {
        public string ValuesName { get; }
        public DropdownAttribute(string valuesName) => ValuesName = valuesName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ENUM FLAGS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws an enum field as a multi-select flags dropdown.
    /// The enum should use powers of two for its values.
    /// <code>
    /// [Flags]
    /// public enum Layers { None = 0, Ground = 1, Water = 2, Air = 4 }
    ///
    /// [EnumFlags]
    /// public Layers activeLayers;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumFlagsAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  EXPANDABLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Makes a ScriptableObject field expandable inline — its properties can be
    /// edited directly in the parent inspector without opening a separate window.
    /// <code>
    /// [Expandable]
    /// public EnemyConfig config;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ExpandableAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  INLINE EDITOR  (NEW — always expanded, no toggle)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Like [Expandable] but always fully expanded. Great for config objects
    /// you always want visible.
    /// <code>
    /// [InlineEditor]
    /// public WeaponStats stats;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InlineEditorAttribute : PropertyAttribute { }

    // ─────────────────────────────────────────────────────────────────────────
    //  FILE PATH / FOLDER PATH  (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a browse button to a <c>string</c> field that opens a file picker.
    /// The stored path can be relative to the project root or absolute.
    /// <code>
    /// [FilePath(extensions: "json,txt")]
    /// public string dataFilePath;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FilePathAttribute : PropertyAttribute
    {
        /// <summary>Comma-separated file extensions without dots, e.g. "json,txt".</summary>
        public string Extensions  { get; }
        /// <summary>If true the path is stored relative to the project root.</summary>
        public bool   Relative    { get; }

        public FilePathAttribute(string extensions = "", bool relative = true)
        {
            Extensions = extensions;
            Relative   = relative;
        }
    }

    /// <summary>
    /// Adds a browse button to a <c>string</c> field that opens a folder picker.
    /// <code>
    /// [FolderPath]
    /// public string outputFolder;
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FolderPathAttribute : PropertyAttribute
    {
        /// <summary>If true the path is stored relative to the project root.</summary>
        public bool Relative { get; }
        public FolderPathAttribute(bool relative = true) => Relative = relative;
    }
}
