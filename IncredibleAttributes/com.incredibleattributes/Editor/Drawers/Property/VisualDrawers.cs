using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    //  PROGRESS BAR
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    internal class ProgressBarPropertyDrawer : PropertyDrawer
    {
        private const float BarHeight = 18f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => BarHeight + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ProgressBarAttribute)attribute;

            float current = property.propertyType == SerializedPropertyType.Integer
                ? property.intValue
                : property.floatValue;

            float max = attr.MaxValue;
            if (!string.IsNullOrEmpty(attr.MaxValueName))
            {
                var t  = property.serializedObject.targetObject;
                var fi = ReflectionUtility.GetField(t, attr.MaxValueName);
                var pi = fi == null ? null : (PropertyInfo)null;
                if (fi != null)
                    max = System.Convert.ToSingle(fi.GetValue(t));
                else
                {
                    var prop = ReflectionUtility.GetPropertyValue(t, attr.MaxValueName);
                    if (prop != null) max = System.Convert.ToSingle(prop);
                }
            }

            max = Mathf.Max(max, 0.0001f);
            float pct = Mathf.Clamp01(current / max);

            // Background
            EditorGUI.DrawRect(position, new Color(0.15f, 0.15f, 0.15f));

            // Fill
            var fill = new Rect(position.x, position.y, position.width * pct, position.height);
            EditorGUI.DrawRect(fill, attr.Color);

            // Border
            var border = position;
            GUI.Box(border, GUIContent.none, EditorStyles.helpBox);

            // Label
            string text = $"{attr.Label}: {current:0.##} / {max:0.##}";
            EditorGUI.LabelField(position, text, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            });
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  RESIZABLE TEXT AREA
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(ResizableTextAreaAttribute))]
    internal class ResizableTextAreaPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight;

            // Measure the content height
            var style = EditorStyles.textArea;
            float h   = style.CalcHeight(new GUIContent(property.stringValue),
                                         EditorGUIUtility.currentViewWidth - 20f);
            return EditorGUIUtility.singleLineHeight + 2f + Mathf.Max(h, EditorGUIUtility.singleLineHeight * 2);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[ResizableTextArea] requires string.", MessageType.Error);
                return;
            }

            EditorGUI.LabelField(new Rect(position.x, position.y,
                                          position.width, EditorGUIUtility.singleLineHeight), label);

            var textRect = new Rect(position.x,
                                    position.y + EditorGUIUtility.singleLineHeight + 2f,
                                    position.width,
                                    position.height - EditorGUIUtility.singleLineHeight - 2f);

            property.stringValue = EditorGUI.TextArea(textRect, property.stringValue, EditorStyles.textArea);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SHOW ASSET PREVIEW
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(ShowAssetPreviewAttribute))]
    internal class ShowAssetPreviewPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr    = (ShowAssetPreviewAttribute)attribute;
            float extra = 0f;
            if (property.objectReferenceValue != null)
            {
                var preview = AssetPreview.GetAssetPreview(property.objectReferenceValue);
                if (preview != null)
                    extra = attr.Height + EditorGUIUtility.standardVerticalSpacing;
            }
            return EditorGUI.GetPropertyHeight(property, label, true) + extra;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowAssetPreviewAttribute)attribute;

            // Object field row
            var fieldRect = new Rect(position.x, position.y, position.width,
                                     EditorGUI.GetPropertyHeight(property, label, true));
            EditorGUI.PropertyField(fieldRect, property, label, true);

            if (property.objectReferenceValue == null) return;

            var preview = AssetPreview.GetAssetPreview(property.objectReferenceValue);
            if (preview == null) return;

            float y       = position.y + fieldRect.height + EditorGUIUtility.standardVerticalSpacing;
            var   prevRect = new Rect(position.x + (position.width - attr.Width) * 0.5f,
                                      y, attr.Width, attr.Height);
            GUI.DrawTexture(prevRect, preview, ScaleMode.ScaleToFit);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  EXPANDABLE  (ScriptableObject inline)
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(ExpandableAttribute))]
    internal class ExpandablePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUI.GetPropertyHeight(property, label);
            if (property.objectReferenceValue == null || !property.isExpanded) return h;

            var so   = new SerializedObject(property.objectReferenceValue);
            var iter = so.GetIterator();
            if (!iter.NextVisible(true)) return h;
            do
            {
                if (iter.name == "m_Script") continue;
                h += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            while (iter.NextVisible(false));
            return h + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUI.GetPropertyHeight(property, label);
            var   top   = new Rect(position.x, position.y, position.width, lineH);

            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(top, property, label);
                return;
            }

            property.isExpanded = EditorGUI.Foldout(
                new Rect(top.x, top.y, 14, top.height), property.isExpanded, GUIContent.none, true);
            EditorGUI.PropertyField(top, property, label);

            if (!property.isExpanded) return;

            var so   = new SerializedObject(property.objectReferenceValue);
            so.Update();

            float y    = position.y + lineH + EditorGUIUtility.standardVerticalSpacing;
            var   iter = so.GetIterator();
            if (!iter.NextVisible(true)) return;

            EditorGUI.indentLevel++;
            do
            {
                if (iter.name == "m_Script") continue;
                float h   = EditorGUI.GetPropertyHeight(iter, true);
                var   r   = new Rect(position.x, y, position.width, h);
                EditorGUI.PropertyField(r, iter, true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            while (iter.NextVisible(false));
            EditorGUI.indentLevel--;

            so.ApplyModifiedProperties();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  INLINE EDITOR  (always expanded)
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(InlineEditorAttribute))]
    internal class InlineEditorPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUI.GetPropertyHeight(property, label);
            if (property.objectReferenceValue == null) return h;

            var so   = new SerializedObject(property.objectReferenceValue);
            var iter = so.GetIterator();
            if (!iter.NextVisible(true)) return h;
            do
            {
                if (iter.name == "m_Script") continue;
                h += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            while (iter.NextVisible(false));
            return h + 8f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUI.GetPropertyHeight(property, label);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineH), property, label);

            if (property.objectReferenceValue == null) return;

            var so   = new SerializedObject(property.objectReferenceValue);
            so.Update();

            float y = position.y + lineH + 4f;
            EditorGUI.DrawRect(new Rect(position.x, y - 1, position.width, 1),
                               new Color(0.4f, 0.4f, 0.4f, 0.5f));

            var iter = so.GetIterator();
            if (!iter.NextVisible(true)) return;

            EditorGUI.indentLevel++;
            do
            {
                if (iter.name == "m_Script") continue;
                float h = EditorGUI.GetPropertyHeight(iter, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iter, true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            while (iter.NextVisible(false));
            EditorGUI.indentLevel--;

            so.ApplyModifiedProperties();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CURVE RANGE
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(CurveRangeAttribute))]
    internal class CurveRangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.AnimationCurve)
            {
                EditorGUI.HelpBox(position, "[CurveRange] requires AnimationCurve.", MessageType.Error);
                return;
            }
            var attr = (CurveRangeAttribute)attribute;
            property.animationCurveValue =
                EditorGUI.CurveField(position, label, property.animationCurveValue, attr.Color, attr.Range);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  MIN VALUE
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(MinValueAttribute))]
    internal class MinValuePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (MinValueAttribute)attribute;
            EditorGUI.PropertyField(position, property, label);
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = Mathf.Max(property.intValue, (int)attr.Value);
            else if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue = Mathf.Max(property.floatValue, attr.Value);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  MAX VALUE
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(MaxValueAttribute))]
    internal class MaxValuePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (MaxValueAttribute)attribute;
            EditorGUI.PropertyField(position, property, label);
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = Mathf.Min(property.intValue, (int)attr.Value);
            else if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue = Mathf.Min(property.floatValue, attr.Value);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  REQUIRED
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    internal class RequiredPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUI.GetPropertyHeight(property, label, true);
            if (IsEmpty(property))
                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float fieldH  = EditorGUI.GetPropertyHeight(property, label, true);
            var   fieldR  = new Rect(position.x, position.y, position.width, fieldH);
            EditorGUI.PropertyField(fieldR, property, label, true);

            if (IsEmpty(property))
            {
                var attr = (RequiredAttribute)attribute;
                string msg = !string.IsNullOrEmpty(attr.Message)
                    ? attr.Message : $"'{label.text}' is required.";
                var helpR = new Rect(position.x,
                                     position.y + fieldH + EditorGUIUtility.standardVerticalSpacing,
                                     position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(helpR, msg, MessageType.Error);
            }
        }

        private static bool IsEmpty(SerializedProperty p) =>
            p.propertyType == SerializedPropertyType.ObjectReference
                ? p.objectReferenceValue == null
                : string.IsNullOrEmpty(p.stringValue);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  VALIDATE INPUT
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(ValidateInputAttribute))]
    internal class ValidateInputPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUI.GetPropertyHeight(property, label, true);
            if (!Validate(property)) h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float fieldH = EditorGUI.GetPropertyHeight(property, label, true);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, fieldH), property, label, true);

            if (!Validate(property))
            {
                var attr = (ValidateInputAttribute)attribute;
                EditorGUI.HelpBox(
                    new Rect(position.x, position.y + fieldH + EditorGUIUtility.standardVerticalSpacing,
                             position.width, EditorGUIUtility.singleLineHeight),
                    attr.Message, MessageType.Error);
            }
        }

        private bool Validate(SerializedProperty property)
        {
            var attr   = (ValidateInputAttribute)attribute;
            var target = property.serializedObject.targetObject;
            var method = ReflectionUtility.GetMethod(target, attr.CallbackName);
            if (method == null || method.ReturnType != typeof(bool)) return true;

            var list = new System.Collections.Generic.List<(string, string)>();
            PropertyUtility.CollectValidationMessages(property,
                ReflectionUtility.GetField(target, property.name), target, list);
            return list.Count == 0;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  FILE PATH
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(FilePathAttribute))]
    internal class FilePathPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[FilePath] requires string.", MessageType.Error);
                return;
            }

            var attr     = (FilePathAttribute)attribute;
            float btnW   = 60f;
            float fieldW = position.width - btnW - 4f;

            EditorGUI.PropertyField(new Rect(position.x, position.y, fieldW, position.height), property, label);

            if (GUI.Button(new Rect(position.x + fieldW + 4f, position.y, btnW, position.height), "Browse"))
            {
                string selected = EditorUtility.OpenFilePanel("Select File", "", attr.Extensions);
                if (!string.IsNullOrEmpty(selected))
                {
                    property.stringValue = attr.Relative
                        ? GetRelativePath(selected) : selected;
                }
            }
        }

        private static string GetRelativePath(string absolute)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..");
            if (absolute.StartsWith(projectPath))
                return "." + absolute.Substring(projectPath.Length).Replace('\\', '/');
            return absolute;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  FOLDER PATH
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(FolderPathAttribute))]
    internal class FolderPathPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[FolderPath] requires string.", MessageType.Error);
                return;
            }

            var   attr   = (FolderPathAttribute)attribute;
            float btnW   = 60f;
            float fieldW = position.width - btnW - 4f;

            EditorGUI.PropertyField(new Rect(position.x, position.y, fieldW, position.height), property, label);

            if (GUI.Button(new Rect(position.x + fieldW + 4f, position.y, btnW, position.height), "Browse"))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    property.stringValue = attr.Relative
                        ? GetRelativePath(selected) : selected;
                }
            }
        }

        private static string GetRelativePath(string absolute)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..");
            if (absolute.StartsWith(projectPath))
                return "." + absolute.Substring(projectPath.Length).Replace('\\', '/');
            return absolute;
        }
    }
}
