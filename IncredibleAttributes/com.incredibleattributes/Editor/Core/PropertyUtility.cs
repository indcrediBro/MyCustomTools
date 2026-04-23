using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    /// <summary>
    /// Stateless helpers that evaluate meta-attribute conditions (ShowIf, EnableIf, etc.)
    /// given a target object and a FieldInfo.
    /// </summary>
    internal static class PropertyUtility
    {
        // ── Visibility ─────────────────────────────────────────────────────

        /// <summary>Returns false if the field should be hidden (ShowIf / HideIf).</summary>
        public static bool IsVisible(FieldInfo field, object target)
        {
            var showIf = field.GetCustomAttribute<ShowIfAttribute>();
            if (showIf != null)
                return EvaluateConditions(showIf.Conditions, showIf.Operator, target);

            var hideIf = field.GetCustomAttribute<HideIfAttribute>();
            if (hideIf != null)
                return !EvaluateConditions(hideIf.Conditions, hideIf.Operator, target);

            return true;
        }

        // ── Enabled state ──────────────────────────────────────────────────

        /// <summary>Returns false if the field should be greyed-out (EnableIf / DisableIf / ReadOnly).</summary>
        public static bool IsEnabled(FieldInfo field, object target)
        {
            if (field.GetCustomAttribute<ReadOnlyAttribute>() != null) return false;

            var enableIf = field.GetCustomAttribute<EnableIfAttribute>();
            if (enableIf != null)
                return EvaluateConditions(enableIf.Conditions, enableIf.Operator, target);

            var disableIf = field.GetCustomAttribute<DisableIfAttribute>();
            if (disableIf != null)
                return !EvaluateConditions(disableIf.Conditions, disableIf.Operator, target);

            return true;
        }

        // ── GUIContent / Label ─────────────────────────────────────────────

        /// <summary>Returns the GUIContent for this field, respecting [Label].</summary>
        public static GUIContent GetLabel(FieldInfo field, SerializedProperty property)
        {
            var labelAttr = field?.GetCustomAttribute<LabelAttribute>();
            if (labelAttr != null)
                return new GUIContent(labelAttr.Text);
            return new GUIContent(property.displayName, property.tooltip);
        }

        // ── Validation ─────────────────────────────────────────────────────

        /// <summary>
        /// Appends any validation error messages for this field/property pair.
        /// </summary>
        public static void CollectValidationMessages(SerializedProperty property,
                                                     FieldInfo field,
                                                     object target,
                                                     List<(string label, string msg)> errors)
        {
            if (field == null) return;

            // Required
            var required = field.GetCustomAttribute<RequiredAttribute>();
            if (required != null)
            {
                bool isEmpty = property.propertyType == SerializedPropertyType.ObjectReference
                             ? property.objectReferenceValue == null
                             : string.IsNullOrEmpty(property.stringValue);

                if (isEmpty)
                    errors.Add((property.displayName,
                                !string.IsNullOrEmpty(required.Message)
                                    ? required.Message
                                    : $"'{property.displayName}' is required."));
            }

            // ValidateInput
            var validate = field.GetCustomAttribute<ValidateInputAttribute>();
            if (validate != null)
            {
                object value = GetSerializedValue(property);
                if (value != null)
                {
                    var method = ReflectionUtility.GetMethod(target, validate.CallbackName);
                    if (method != null)
                    {
                        try
                        {
                            bool valid = (bool)method.Invoke(method.IsStatic ? null : target, new[] { value });
                            if (!valid)
                                errors.Add((property.displayName, validate.Message));
                        }
                        catch { /* type mismatch – skip */ }
                    }
                }
            }
        }

        // ── Callback invocation ────────────────────────────────────────────

        public static void InvokeCallback(string callbackName, object target, object argument = null)
            => ReflectionUtility.InvokeMethod(target, callbackName, argument);

        // ── Private helpers ────────────────────────────────────────────────

        private static bool EvaluateConditions(string[] conditions, EConditionOperator op, object target)
        {
            if (conditions == null || conditions.Length == 0) return true;

            if (op == EConditionOperator.And)
            {
                foreach (var c in conditions)
                    if (!ReflectionUtility.EvaluateBoolCondition(target, c)) return false;
                return true;
            }
            else // Or
            {
                foreach (var c in conditions)
                    if (ReflectionUtility.EvaluateBoolCondition(target, c)) return true;
                return false;
            }
        }

        private static object GetSerializedValue(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer       => (object)prop.intValue,
                SerializedPropertyType.Boolean       => prop.boolValue,
                SerializedPropertyType.Float         => prop.floatValue,
                SerializedPropertyType.String        => prop.stringValue,
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue,
                SerializedPropertyType.Vector2       => prop.vector2Value,
                SerializedPropertyType.Vector3       => prop.vector3Value,
                SerializedPropertyType.Vector4       => prop.vector4Value,
                SerializedPropertyType.Color         => prop.colorValue,
                SerializedPropertyType.LayerMask     => prop.intValue,
                SerializedPropertyType.Enum          => prop.enumValueIndex,
                _                                    => null
            };
        }
    }
}
