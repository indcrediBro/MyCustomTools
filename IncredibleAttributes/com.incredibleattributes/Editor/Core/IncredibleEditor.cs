using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Auto-apply to ALL MonoBehaviours and ScriptableObjects — no inheritance!
    // ──────────────────────────────────────────────────────────────────────────

    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    internal class IncredibleMonoBehaviourEditor : IncredibleEditor { }

    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    internal class IncredibleScriptableObjectEditor : IncredibleEditor { }

    // ──────────────────────────────────────────────────────────────────────────
    //  Core editor — contains all drawing logic, exposed as static methods so
    //  IncredibleEditorGUI can call it without inheritance.
    // ──────────────────────────────────────────────────────────────────────────

    public class IncredibleEditor : UnityEditor.Editor
    {
        // ── Per-editor state ───────────────────────────────────────────────
        private List<SerializedProperty> _props;
        private IEnumerable<FieldInfo>   _nonSerializedFields;
        private IEnumerable<PropertyInfo> _nativeProperties;
        private IEnumerable<MethodInfo>   _methods;
        private readonly Dictionary<string, ReorderableList> _lists      = new();
        private readonly Dictionary<string, bool>            _foldouts   = new();

        // ── Unity lifecycle ────────────────────────────────────────────────

        protected virtual void OnEnable()  => Refresh();
        protected virtual void OnDisable() => _lists.Clear();

        private void Refresh()
        {
            _props = CollectSerializedProperties(serializedObject);

            _nonSerializedFields = ReflectionUtility.GetAllFields(target,
                f => f.GetCustomAttribute<ShowNonSerializedFieldAttribute>() != null);

            _nativeProperties = ReflectionUtility.GetAllProperties(target,
                p => p.GetCustomAttribute<ShowNativePropertyAttribute>() != null);

            _methods = ReflectionUtility.GetAllMethods(target,
                m => m.GetCustomAttribute<ButtonAttribute>() != null ||
                     m.GetCustomAttribute<ButtonGroupAttribute>() != null);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawFull(serializedObject, target, false, _props, _nonSerializedFields,
                     _nativeProperties, _methods, _foldouts, _lists);
            serializedObject.ApplyModifiedProperties();
        }

        // ── Static draw entry (used by IncredibleEditorGUI public API) ─────

        /// <summary>Full draw pass — called by the auto-editors and public API.</summary>
        internal static void DrawFull(SerializedObject so, UnityEngine.Object target,
                                      bool includeScriptField = false)
        {
            var props = CollectSerializedProperties(so);
            var nonSer = ReflectionUtility.GetAllFields(target,
                f => f.GetCustomAttribute<ShowNonSerializedFieldAttribute>() != null);
            var natProps = ReflectionUtility.GetAllProperties(target,
                p => p.GetCustomAttribute<ShowNativePropertyAttribute>() != null);
            var methods = ReflectionUtility.GetAllMethods(target,
                m => m.GetCustomAttribute<ButtonAttribute>() != null ||
                     m.GetCustomAttribute<ButtonGroupAttribute>() != null);

            DrawFull(so, target, includeScriptField, props, nonSer, natProps, methods,
                     new Dictionary<string, bool>(), new Dictionary<string, ReorderableList>());
        }

        private static void DrawFull(SerializedObject so,
                                     UnityEngine.Object target,
                                     bool includeScriptField,
                                     List<SerializedProperty> props,
                                     IEnumerable<FieldInfo> nonSer,
                                     IEnumerable<PropertyInfo> natProps,
                                     IEnumerable<MethodInfo> methods,
                                     Dictionary<string, bool> foldouts,
                                     Dictionary<string, ReorderableList> lists)
        {
            if (includeScriptField)
                using (new EditorGUI.DisabledScope(true))
                {
                    var sp = so.FindProperty("m_Script");
                    if (sp != null) EditorGUILayout.PropertyField(sp);
                }
            else
            {
                // Always skip m_Script silently
            }

            DrawSerializedProperties(so, target, props, foldouts, lists);
            DrawNonSerializedFields(target, nonSer);
            DrawNativeProperties(target, natProps);
            DrawButtons(target, methods, foldouts);
        }

        // ── Serialized properties ──────────────────────────────────────────

        private static void DrawSerializedProperties(SerializedObject so,
                                                     UnityEngine.Object target,
                                                     List<SerializedProperty> props,
                                                     Dictionary<string, bool> foldouts,
                                                     Dictionary<string, ReorderableList> lists)
        {
            var errors = new List<(string label, string msg)>();
            string currentBox     = null;
            string currentFoldout = null;
            bool   foldoutOpen    = true;

            foreach (var prop in props)
            {
                if (prop.name == "m_Script") continue;

                var fi = ReflectionUtility.GetField(target, prop.name);

                // ── Group transitions ──────────────────────────────────────

                string newBox = fi?.GetCustomAttribute<BoxGroupAttribute>()?.GroupName;
                if (newBox != currentBox)
                {
                    if (currentBox != null) { EditorGUILayout.EndVertical(); GUILayout.Space(3); }
                    if (newBox     != null) { GUILayout.Space(3); EditorGUILayout.BeginVertical("box");
                        if (!string.IsNullOrEmpty(newBox))
                            EditorGUILayout.LabelField(newBox, EditorStyles.boldLabel); }
                    currentBox = newBox;
                }

                string newFoldout = fi?.GetCustomAttribute<FoldoutAttribute>()?.FoldoutName;
                if (newFoldout != currentFoldout)
                {
                    if (currentFoldout != null) EditorGUI.indentLevel--;
                    if (newFoldout     != null)
                    {
                        if (!foldouts.ContainsKey(newFoldout)) foldouts[newFoldout] = true;
                        foldouts[newFoldout] = EditorGUILayout.Foldout(foldouts[newFoldout], newFoldout, true, EditorStyles.foldoutHeader);
                        foldoutOpen          = foldouts[newFoldout];
                        EditorGUI.indentLevel++;
                    }
                    else foldoutOpen = true;
                    currentFoldout = newFoldout;
                }

                if (!foldoutOpen) continue;

                // ── Visibility (ShowIf / HideIf) ───────────────────────────
                if (fi != null && !PropertyUtility.IsVisible(fi, target)) continue;

                DrawProperty(prop, fi, target, true, errors, lists, so);
            }

            // Close hanging groups
            if (currentFoldout != null) EditorGUI.indentLevel--;
            if (currentBox     != null) { EditorGUILayout.EndVertical(); GUILayout.Space(3); }

            // Validation errors
            if (errors.Count > 0)
            {
                EditorGUILayout.Space(4);
                foreach (var (lbl, msg) in errors)
                    EditorGUILayout.HelpBox($"[{lbl}] {msg}", MessageType.Error);
            }
        }

        // ── Draw single property (also used from public API) ──────────────

        /// <summary>
        /// Draws one property with full IncredibleAttributes decoration.
        /// Returns false if the property was hidden by ShowIf/HideIf.
        /// </summary>
        internal static bool DrawProperty(SerializedProperty prop,
                                          FieldInfo fi,
                                          UnityEngine.Object target,
                                          bool includeChildren = true)
            => DrawProperty(prop, fi, target, includeChildren,
                            null, null, prop.serializedObject);

        private static bool DrawProperty(SerializedProperty prop,
                                         FieldInfo fi,
                                         UnityEngine.Object target,
                                         bool includeChildren,
                                         List<(string, string)> errors,
                                         Dictionary<string, ReorderableList> lists,
                                         SerializedObject so)
        {
            if (fi != null && !PropertyUtility.IsVisible(fi, target)) return false;

            bool   enabled    = fi == null || PropertyUtility.IsEnabled(fi, target);
            var    label      = fi != null ? PropertyUtility.GetLabel(fi, prop) : new GUIContent(prop.displayName);
            Color  prevColor  = GUI.color;
            int    prevIndent = EditorGUI.indentLevel;

            // GUIColor
            var guiColor = fi?.GetCustomAttribute<GUIColorAttribute>();
            if (guiColor != null) GUI.color = guiColor.Color;

            // Indent
            var indent = fi?.GetCustomAttribute<IndentAttribute>();
            if (indent != null) EditorGUI.indentLevel += indent.Levels;

            // Prefix / Suffix
            var prefix = fi?.GetCustomAttribute<PrefixAttribute>();
            var suffix = fi?.GetCustomAttribute<SuffixAttribute>();
            bool hasFix = prefix != null || suffix != null;

            // OnValueChanged
            var ovc = fi?.GetCustomAttribute<OnValueChangedAttribute>();

            using (new EditorGUI.DisabledScope(!enabled))
            {
                // ReorderableList
                var reorderAttr = fi?.GetCustomAttribute<ReorderableListAttribute>();
                if (reorderAttr != null && prop.isArray)
                {
                    DrawReorderableList(prop, fi, lists ?? new Dictionary<string, ReorderableList>());
                }
                else if (hasFix)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (prefix != null) GUILayout.Label(prefix.Text, GUILayout.ExpandWidth(false));

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(prop, label, includeChildren);
                    bool changed = EditorGUI.EndChangeCheck();

                    if (suffix != null) GUILayout.Label(suffix.Text, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();

                    if (changed && ovc != null)
                    {
                        so?.ApplyModifiedProperties();
                        PropertyUtility.InvokeCallback(ovc.CallbackName, target);
                        so?.Update();
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(prop, label, includeChildren);
                    bool changed = EditorGUI.EndChangeCheck();

                    if (changed && ovc != null)
                    {
                        so?.ApplyModifiedProperties();
                        PropertyUtility.InvokeCallback(ovc.CallbackName, target);
                        so?.Update();
                    }
                }
            }

            // Validation
            if (errors != null && fi != null)
                PropertyUtility.CollectValidationMessages(prop, fi, target, errors);

            // Restore state
            GUI.color             = prevColor;
            EditorGUI.indentLevel = prevIndent;
            return true;
        }

        // ── ReorderableList helper ─────────────────────────────────────────

        private static void DrawReorderableList(SerializedProperty prop,
                                                FieldInfo fi,
                                                Dictionary<string, ReorderableList> lists)
        {
            string key = prop.propertyPath;
            if (!lists.TryGetValue(key, out var list))
            {
                list = new ReorderableList(prop.serializedObject, prop, true, true, true, true)
                {
                    drawHeaderCallback  = r => EditorGUI.LabelField(r, prop.displayName, EditorStyles.boldLabel),
                    drawElementCallback = (r, i, active, focused) =>
                    {
                        r.y      += 2;
                        r.height -= 4;
                        EditorGUI.PropertyField(r, prop.GetArrayElementAtIndex(i),
                                                GUIContent.none, true);
                    },
                    elementHeightCallback = i =>
                        EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(i), true) + 4
                };
                lists[key] = list;
            }
            list.DoLayoutList();
        }

        // ── Non-serialized fields ──────────────────────────────────────────

        private static void DrawNonSerializedFields(UnityEngine.Object target, IEnumerable<FieldInfo> fields)
        {
            var list = fields.ToList();
            if (list.Count == 0) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Non-Serialized Fields", EditorStyles.boldLabel);
            IncredibleEditorGUI.HorizontalLine(new Color(0.3f, 0.3f, 0.3f, 0.4f));

            using (new EditorGUI.DisabledScope(true))
                foreach (var f in list)
                    DrawReadOnlyValue(ObjectNames.NicifyVariableName(f.Name), f.GetValue(target));
        }

        // ── Native properties ──────────────────────────────────────────────

        private static void DrawNativeProperties(UnityEngine.Object target, IEnumerable<PropertyInfo> props)
        {
            var list = props.ToList();
            if (list.Count == 0) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Native Properties", EditorStyles.boldLabel);
            IncredibleEditorGUI.HorizontalLine(new Color(0.3f, 0.3f, 0.3f, 0.4f));

            using (new EditorGUI.DisabledScope(true))
                foreach (var p in list)
                {
                    try { DrawReadOnlyValue(ObjectNames.NicifyVariableName(p.Name), p.GetValue(target)); }
                    catch { EditorGUILayout.LabelField(p.Name, "Error reading value"); }
                }
        }

        // ── Buttons ────────────────────────────────────────────────────────

        private static void DrawButtons(UnityEngine.Object target,
                                        IEnumerable<MethodInfo> methods,
                                        Dictionary<string, bool> foldouts)
        {
            var solo   = methods.Where(m => m.GetCustomAttribute<ButtonAttribute>() != null).ToList();
            var groups = methods
                .Where(m => m.GetCustomAttribute<ButtonGroupAttribute>() != null)
                .GroupBy(m => m.GetCustomAttribute<ButtonGroupAttribute>().GroupName)
                .ToList();

            if (solo.Count == 0 && groups.Count == 0) return;
            EditorGUILayout.Space(6);

            foreach (var m in solo)
            {
                var attr   = m.GetCustomAttribute<ButtonAttribute>();
                string lbl = string.IsNullOrEmpty(attr.Label) ? ObjectNames.NicifyVariableName(m.Name) : attr.Label;

                bool canClick = attr.EnableMode switch
                {
                    EButtonEnableMode.Editor   => !Application.isPlaying,
                    EButtonEnableMode.Playmode => Application.isPlaying,
                    _                          => true
                };

                var guiColor = m.GetCustomAttribute<GUIColorAttribute>();
                Color prev   = GUI.backgroundColor;
                if (guiColor != null) GUI.backgroundColor = guiColor.Color;

                using (new EditorGUI.DisabledScope(!canClick))
                    if (GUILayout.Button(lbl))
                        m.Invoke(m.IsStatic ? null : target, null);

                GUI.backgroundColor = prev;
            }

            foreach (var group in groups)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var m in group)
                {
                    var attr   = m.GetCustomAttribute<ButtonGroupAttribute>();
                    string lbl = string.IsNullOrEmpty(attr.Label) ? ObjectNames.NicifyVariableName(m.Name) : attr.Label;

                    var guiColor = m.GetCustomAttribute<GUIColorAttribute>();
                    Color prev   = GUI.backgroundColor;
                    if (guiColor != null) GUI.backgroundColor = guiColor.Color;

                    if (GUILayout.Button(lbl))
                        m.Invoke(m.IsStatic ? null : target, null);

                    GUI.backgroundColor = prev;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── Read-only value drawer ─────────────────────────────────────────

        internal static void DrawReadOnlyValue(string label, object value)
        {
            switch (value)
            {
                case bool    v: EditorGUILayout.Toggle(label, v);                                        break;
                case int     v: EditorGUILayout.IntField(label, v);                                      break;
                case long    v: EditorGUILayout.LongField(label, v);                                     break;
                case float   v: EditorGUILayout.FloatField(label, v);                                    break;
                case double  v: EditorGUILayout.DoubleField(label, v);                                   break;
                case string  v: EditorGUILayout.TextField(label, v);                                     break;
                case Vector2 v: EditorGUILayout.Vector2Field(label, v);                                  break;
                case Vector3 v: EditorGUILayout.Vector3Field(label, v);                                  break;
                case Vector4 v: EditorGUILayout.Vector4Field(label, v);                                  break;
                case Color   v: EditorGUILayout.ColorField(label, v);                                    break;
                case Bounds  v: EditorGUILayout.BoundsField(label, v);                                   break;
                case Rect    v: EditorGUILayout.RectField(label, v);                                     break;
                case UnityEngine.Object o:
                    EditorGUILayout.ObjectField(label, o, o?.GetType() ?? typeof(UnityEngine.Object), true);
                    break;
                default:
                    EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
                    break;
            }
        }

        // ── Collect properties helper ──────────────────────────────────────

        private static List<SerializedProperty> CollectSerializedProperties(SerializedObject so)
        {
            var result   = new List<SerializedProperty>();
            var iterator = so.GetIterator();
            if (!iterator.NextVisible(true)) return result;
            do { result.Add(so.FindProperty(iterator.name) ?? iterator.Copy()); }
            while (iterator.NextVisible(false));
            return result;
        }
    }
}
