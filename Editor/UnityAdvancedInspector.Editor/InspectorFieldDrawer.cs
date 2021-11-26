using System;
using UnityEditor;
using UnityEngine;

namespace UnityAdvancedInspector.Editor
{
    [CustomPropertyDrawer(typeof(InspectorFieldAttribute))]
    class InspectorFieldDrawer : PropertyDrawer
    {
        public new InspectorFieldAttribute attribute => (InspectorFieldAttribute)base.attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && !attribute.Disabled;

            EditorGUI.PropertyField(position, property, attribute.Label == null ? label : new GUIContent(attribute.Label), true);

            GUI.enabled = guiEnabled;
        }

        public static T LayoutField<T>(string label, T value, params GUILayoutOption[] options)
        {
            switch (typeof(T))
            {
                case var t when t == typeof(string):
                    return (T)(object)EditorGUILayout.TextField(label, (string)(object)value, options);

                case var t when t == typeof(int):
                    return (T)(object)EditorGUILayout.IntField(label, (int)(object)value, options);

                default:
                    EditorGUILayout.LabelField(label, $"Unsupported field type '{typeof(T).Name}'.");
                    return value;
            }
        }
    }
}
