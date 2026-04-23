using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    /// <summary>
    /// Reflection helpers with per-type member caching.
    /// Avoids repeated GetFields/GetMethods calls, which is the main
    /// source of reflection overhead in large inspectors.
    /// </summary>
    internal static class ReflectionUtility
    {
        // ── Cache ──────────────────────────────────────────────────────────
        private static readonly Dictionary<Type, FieldInfo[]>    _fieldCache    = new();
        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();
        private static readonly Dictionary<Type, MethodInfo[]>   _methodCache   = new();

        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static |
                                         BindingFlags.Public   | BindingFlags.NonPublic;

        // ── Fields ─────────────────────────────────────────────────────────

        public static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate = null)
        {
            if (target == null) yield break;
            var fields = GetCachedFields(target.GetType());
            foreach (var f in fields)
                if (predicate == null || predicate(f))
                    yield return f;
        }

        public static FieldInfo GetField(object target, string fieldName)
        {
            if (target == null) return null;
            return GetCachedFields(target.GetType()).FirstOrDefault(f => f.Name == fieldName);
        }

        public static object GetFieldValue(object target, string fieldName)
        {
            var f = GetField(target, fieldName);
            return f?.GetValue(target);
        }

        // ── Properties ─────────────────────────────────────────────────────

        public static IEnumerable<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate = null)
        {
            if (target == null) yield break;
            var props = GetCachedProperties(target.GetType());
            foreach (var p in props)
                if (predicate == null || predicate(p))
                    yield return p;
        }

        public static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null) return null;
            var p = GetCachedProperties(target.GetType()).FirstOrDefault(x => x.Name == propertyName);
            return p?.GetValue(target);
        }

        // ── Methods ────────────────────────────────────────────────────────

        public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate = null)
        {
            if (target == null) yield break;
            var methods = GetCachedMethods(target.GetType());
            foreach (var m in methods)
                if (predicate == null || predicate(m))
                    yield return m;
        }

        public static MethodInfo GetMethod(object target, string methodName)
        {
            if (target == null) return null;
            return GetCachedMethods(target.GetType()).FirstOrDefault(m => m.Name == methodName);
        }

        /// <summary>
        /// Tries to evaluate a named bool condition on the target.
        /// The name can refer to: a bool field, a bool property, or a zero-param bool method.
        /// Returns true if the condition cannot be found (fail-open).
        /// </summary>
        public static bool EvaluateBoolCondition(object target, string conditionName)
        {
            if (target == null || string.IsNullOrEmpty(conditionName)) return true;

            var type = target.GetType();

            // Try field
            var field = GetCachedFields(type).FirstOrDefault(f => f.Name == conditionName);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);

            // Try property
            var prop = GetCachedProperties(type).FirstOrDefault(p => p.Name == conditionName);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
                return (bool)prop.GetValue(target);

            // Try method
            var method = GetCachedMethods(type).FirstOrDefault(m => m.Name == conditionName);
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                return (bool)method.Invoke(method.IsStatic ? null : target, null);

            Debug.LogWarning($"[IncredibleAttributes] Condition '{conditionName}' not found on {type.Name}. " +
                             "Make sure it's a bool field, property, or zero-param bool method.");
            return true;
        }

        /// <summary>Invokes a method by name. Accepts optional single argument (the field value).</summary>
        public static void InvokeMethod(object target, string methodName, object argument = null)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            var method = GetMethod(target, methodName);
            if (method == null)
            {
                Debug.LogWarning($"[IncredibleAttributes] Method '{methodName}' not found on {target.GetType().Name}.");
                return;
            }

            var parameters = method.GetParameters();
            try
            {
                if (parameters.Length == 0)
                    method.Invoke(method.IsStatic ? null : target, null);
                else if (parameters.Length == 1 && argument != null)
                    method.Invoke(method.IsStatic ? null : target, new[] { argument });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ── Private cache helpers ──────────────────────────────────────────

        private static FieldInfo[] GetCachedFields(Type type)
        {
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                // Walk hierarchy so we get fields from base classes too
                var list = new List<FieldInfo>();
                var t    = type;
                while (t != null && t != typeof(object))
                {
                    list.AddRange(t.GetFields(ALL | BindingFlags.DeclaredOnly));
                    t = t.BaseType;
                }
                fields = list.ToArray();
                _fieldCache[type] = fields;
            }
            return fields;
        }

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            if (!_propertyCache.TryGetValue(type, out var props))
            {
                var list = new List<PropertyInfo>();
                var t    = type;
                while (t != null && t != typeof(object))
                {
                    list.AddRange(t.GetProperties(ALL | BindingFlags.DeclaredOnly));
                    t = t.BaseType;
                }
                props = list.ToArray();
                _propertyCache[type] = props;
            }
            return props;
        }

        private static MethodInfo[] GetCachedMethods(Type type)
        {
            if (!_methodCache.TryGetValue(type, out var methods))
            {
                var list = new List<MethodInfo>();
                var t    = type;
                while (t != null && t != typeof(object))
                {
                    list.AddRange(t.GetMethods(ALL | BindingFlags.DeclaredOnly));
                    t = t.BaseType;
                }
                methods = list.ToArray();
                _methodCache[type] = methods;
            }
            return methods;
        }
    }
}
