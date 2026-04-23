using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    /// <summary>
    /// Public static API for drawing IncredibleAttributes-aware fields.
    /// Call this from your own <c>CustomEditor</c> if you want the full
    /// attribute stack without inheriting from anything.
    ///
    /// <code>
    /// public override void OnInspectorGUI()
    /// {
    ///     serializedObject.Update();
    ///     IncredibleEditorGUI.DrawAllProperties(serializedObject, target);
    ///     serializedObject.ApplyModifiedProperties();
    /// }
    /// </code>
    /// </summary>
    public static class IncredibleEditorGUI
    {
        // ── Public entry points ────────────────────────────────────────────

        /// <summary>
        /// Draws all serialized properties, then non-serialized fields, native
        /// properties, and method buttons — exactly what the auto-editor does.
        /// </summary>
        public static void DrawAllProperties(SerializedObject so, UnityEngine.Object target,
                                             bool includeScriptField = false)
        {
            IncredibleEditor.DrawFull(so, target, includeScriptField);
        }

        /// <summary>
        /// Draws a single property with full IncredibleAttributes support
        /// (ShowIf, Label, GUIColor, Indent, Suffix/Prefix, etc.).
        /// Returns false if the property was hidden.
        /// </summary>
        public static bool PropertyField(SerializedProperty property, FieldInfo fieldInfo,
                                         UnityEngine.Object target, bool includeChildren = true)
        {
            return IncredibleEditor.DrawProperty(property, fieldInfo, target, includeChildren);
        }

        // ── Low-level drawing helpers (useful in custom drawers) ───────────

        /// <summary>Draws a coloured horizontal line.</summary>
        public static void HorizontalLine(Color color, float height = 1f, float marginTop = 4f, float marginBottom = 4f)
        {
            GUILayout.Space(marginTop);
            var rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, color);
            GUILayout.Space(marginBottom);
        }

        /// <summary>Draws a section title with optional subtitle.</summary>
        public static void Title(string text, string subtitle = null, bool line = true)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                EditorGUILayout.LabelField(subtitle, style);
            }
            if (line) HorizontalLine(new Color(0.3f, 0.3f, 0.3f, 0.5f), 1f, 2f, 4f);
        }

        /// <summary>Draws a Unity HelpBox styled as a compact message.</summary>
        public static void InfoBox(string text, EInfoBoxType type = EInfoBoxType.Normal)
        {
            var mt = type switch
            {
                EInfoBoxType.Warning => MessageType.Warning,
                EInfoBoxType.Error   => MessageType.Error,
                _                   => MessageType.Info
            };
            EditorGUILayout.HelpBox(text, mt);
        }
    }
}
