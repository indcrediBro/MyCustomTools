using UnityEditor;
using UnityEngine;

namespace Core.Settings
{
    [CustomEditor(typeof(SettingsSliderBinder))]
    public class SettingsSliderBinderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var typeProp = serializedObject.FindProperty("Type");
            var floatProp = serializedObject.FindProperty("FloatPath");
            var intProp = serializedObject.FindProperty("IntPath");
            var minProp = serializedObject.FindProperty("MinValue");
            var maxProp = serializedObject.FindProperty("MaxValue");
            var labelProp = serializedObject.FindProperty("ValueLabel");
            var formatProp = serializedObject.FindProperty("LabelFormat");

            EditorGUILayout.PropertyField(typeProp);

            // 👇 Show only relevant field
            if ((SettingsSliderBinder.BindingType)typeProp.enumValueIndex == SettingsSliderBinder.BindingType.Float)
            {
                EditorGUILayout.PropertyField(floatProp);
            }
            else
            {
                EditorGUILayout.PropertyField(intProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Range", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minProp);
            EditorGUILayout.PropertyField(maxProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Label", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(labelProp);
            EditorGUILayout.PropertyField(formatProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}