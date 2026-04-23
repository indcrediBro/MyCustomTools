using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    //  ANIMATOR PARAM
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(AnimatorParamAttribute))]
    internal class AnimatorParamPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr     = (AnimatorParamAttribute)attribute;
            var target   = property.serializedObject.targetObject;
            var animField = ReflectionUtility.GetField(target, attr.AnimatorName);

            if (animField == null || !(animField.GetValue(target) is Animator animator) || animator == null)
            {
                EditorGUI.HelpBox(position,
                    $"[AnimatorParam] Could not find Animator '{attr.AnimatorName}'.",
                    MessageType.Warning);
                return;
            }

            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var paramList = controller.parameters
                .Where(p => attr.ParameterType == null || p.type == attr.ParameterType.Value)
                .ToList();

            if (paramList.Count == 0)
            {
                EditorGUI.HelpBox(position, "No matching parameters found.", MessageType.Info);
                return;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                var hashes = paramList.Select(p => Animator.StringToHash(p.name)).ToArray();
                var names  = paramList.Select(p => p.name).ToArray();
                int cur    = System.Array.IndexOf(hashes, property.intValue);
                cur = Mathf.Max(0, cur);
                int sel = EditorGUI.Popup(position, label.text, cur, names);
                property.intValue = hashes[sel];
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                var names = paramList.Select(p => p.name).ToArray();
                int cur   = System.Array.IndexOf(names, property.stringValue);
                cur = Mathf.Max(0, cur);
                int sel = EditorGUI.Popup(position, label.text, cur, names);
                property.stringValue = names[sel];
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ENUM FLAGS
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    internal class EnumFlagsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.HelpBox(position, "[EnumFlags] requires an enum field.", MessageType.Error);
                return;
            }
            property.intValue = EditorGUI.MaskField(position, label, property.intValue,
                                                    property.enumDisplayNames);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  DROPDOWN
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(DropdownAttribute))]
    internal class DropdownPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr   = (DropdownAttribute)attribute;
            var target = property.serializedObject.targetObject;

            // Try to get values from field, property, or method
            object raw = ReflectionUtility.GetFieldValue(target, attr.ValuesName)
                      ?? ReflectionUtility.GetPropertyValue(target, attr.ValuesName);

            if (raw == null)
            {
                var m = ReflectionUtility.GetMethod(target, attr.ValuesName);
                if (m != null && m.GetParameters().Length == 0)
                    raw = m.Invoke(m.IsStatic ? null : target, null);
            }

            if (raw == null)
            {
                EditorGUI.HelpBox(position,
                    $"[Dropdown] Could not find '{attr.ValuesName}'.", MessageType.Error);
                return;
            }

            // Extract display names and values
            string[] displayNames;
            object[] values;

            if (raw is IDropdownList dl)
            {
                displayNames = dl.GetDisplayNames();
                values       = dl.GetValues();
            }
            else if (raw is System.Array arr)
            {
                values       = arr.Cast<object>().ToArray();
                displayNames = values.Select(v => v?.ToString() ?? "null").ToArray();
            }
            else if (raw is System.Collections.IList lst)
            {
                values       = lst.Cast<object>().ToArray();
                displayNames = values.Select(v => v?.ToString() ?? "null").ToArray();
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (values.Length == 0) { EditorGUI.PropertyField(position, property, label); return; }

            // Find current index
            object currentValue = GetPropertyValue(property);
            int cur = 0;
            for (int i = 0; i < values.Length; i++)
                if (Equals(values[i], currentValue)) { cur = i; break; }

            int sel = EditorGUI.Popup(position, label.text, cur, displayNames);
            SetPropertyValue(property, values[sel]);
        }

        private static object GetPropertyValue(SerializedProperty p) => p.propertyType switch
        {
            SerializedPropertyType.Integer  => (object)p.intValue,
            SerializedPropertyType.Float    => p.floatValue,
            SerializedPropertyType.String   => p.stringValue,
            SerializedPropertyType.Boolean  => p.boolValue,
            SerializedPropertyType.Vector2  => p.vector2Value,
            SerializedPropertyType.Vector3  => p.vector3Value,
            _                               => null
        };

        private static void SetPropertyValue(SerializedProperty p, object value)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: p.intValue   = System.Convert.ToInt32(value);   break;
                case SerializedPropertyType.Float:   p.floatValue = System.Convert.ToSingle(value);  break;
                case SerializedPropertyType.String:  p.stringValue = value?.ToString() ?? "";        break;
                case SerializedPropertyType.Boolean: p.boolValue  = System.Convert.ToBoolean(value); break;
                case SerializedPropertyType.Vector2: if (value is Vector2 v2) p.vector2Value = v2;   break;
                case SerializedPropertyType.Vector3: if (value is Vector3 v3) p.vector3Value = v3;   break;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  MIN MAX SLIDER
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    internal class MinMaxSliderPropertyDrawer : PropertyDrawer
    {
        private const float FieldWidth   = 50f;
        private const float FieldPadding = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.HelpBox(position, "[MinMaxSlider] requires Vector2.", MessageType.Error);
                return;
            }

            var attr  = (MinMaxSliderAttribute)attribute;
            var value = property.vector2Value;

            position = EditorGUI.PrefixLabel(position, label);

            float minField = value.x;
            float maxField = value.y;

            var r = position;
            r.width = FieldWidth;
            minField = EditorGUI.FloatField(r, minField);

            r.x    += FieldWidth + FieldPadding;
            r.width = position.width - FieldWidth * 2 - FieldPadding * 2;
            EditorGUI.MinMaxSlider(r, ref minField, ref maxField, attr.Min, attr.Max);

            r.x    += r.width + FieldPadding;
            r.width = FieldWidth;
            maxField = EditorGUI.FloatField(r, maxField);

            value.x = Mathf.Clamp(minField, attr.Min, maxField);
            value.y = Mathf.Clamp(maxField, minField, attr.Max);
            property.vector2Value = value;
        }
    }
}
