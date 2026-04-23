using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    //  TAG
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(TagAttribute))]
    internal class TagPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[Tag] requires a string field.", MessageType.Error);
                return;
            }
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  LAYER
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(LayerAttribute))]
    internal class LayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.LayerField(position, label, property.intValue);
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                int idx = LayerMask.NameToLayer(property.stringValue);
                idx = EditorGUI.LayerField(position, label, Mathf.Max(0, idx));
                property.stringValue = LayerMask.LayerToName(idx);
            }
            else
            {
                EditorGUI.HelpBox(position, "[Layer] requires int or string.", MessageType.Error);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SORTING LAYER
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    internal class SortingLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var layers = SortingLayer.layers;
            var names  = layers.Select(l => l.name).ToArray();
            var ids    = layers.Select(l => l.id).ToArray();

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int idx = System.Array.FindIndex(ids, id => id == property.intValue);
                idx = Mathf.Clamp(idx, 0, names.Length - 1);
                idx = EditorGUI.Popup(position, label.text, idx, names);
                property.intValue = ids[idx];
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                int idx = System.Array.IndexOf(names, property.stringValue);
                idx = Mathf.Clamp(idx, 0, names.Length - 1);
                idx = EditorGUI.Popup(position, label.text, idx, names);
                property.stringValue = names[idx];
            }
            else
            {
                EditorGUI.HelpBox(position, "[SortingLayer] requires int or string.", MessageType.Error);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SCENE
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(SceneAttribute))]
    internal class ScenePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorGUI.HelpBox(position, "No scenes in Build Settings.", MessageType.Warning);
                return;
            }

            var names   = scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();
            var indices = System.Linq.Enumerable.Range(0, scenes.Length).ToArray();

            if (property.propertyType == SerializedPropertyType.String)
            {
                int cur = System.Array.IndexOf(names, property.stringValue);
                cur = Mathf.Clamp(cur, 0, names.Length - 1);
                int sel = EditorGUI.Popup(position, label.text, cur, names);
                property.stringValue = names[sel];
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int cur = Mathf.Clamp(property.intValue, 0, scenes.Length - 1);
                int sel = EditorGUI.Popup(position, label.text, cur, names);
                property.intValue = sel;
            }
            else
            {
                EditorGUI.HelpBox(position, "[Scene] requires int or string.", MessageType.Error);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  INPUT AXIS
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(InputAxisAttribute))]
    internal class InputAxisPropertyDrawer : PropertyDrawer
    {
        private static string[] _axes;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[InputAxis] requires a string field.", MessageType.Error);
                return;
            }

            if (_axes == null) _axes = FetchAxes();

            int cur = System.Array.IndexOf(_axes, property.stringValue);
            cur = Mathf.Max(0, cur);
            int sel = EditorGUI.Popup(position, label.text, cur, _axes);
            property.stringValue = _axes.Length > 0 ? _axes[sel] : string.Empty;
        }

        private static string[] FetchAxes()
        {
            var inputManager = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "ProjectSettings/InputManager.asset");
            if (inputManager == null) return new[] { "(none)" };

            var so   = new SerializedObject(inputManager);
            var axes = so.FindProperty("m_Axes");
            if (axes == null) return new[] { "(none)" };

            var result = new System.Collections.Generic.List<string>();
            for (int i = 0; i < axes.arraySize; i++)
            {
                var name = axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name");
                if (name != null && !string.IsNullOrEmpty(name.stringValue))
                    result.Add(name.stringValue);
            }
            return result.Distinct().ToArray();
        }
    }
}
