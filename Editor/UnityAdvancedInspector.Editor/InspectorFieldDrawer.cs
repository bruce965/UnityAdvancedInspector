using System;
using UnityEditor;
using UnityEngine;

namespace UnityAdvancedInspector.Editor
{
    [CustomPropertyDrawer(typeof(InspectorField))]
    class InspectorFieldDrawer : PropertyDrawer
    {
        static readonly GUIContent _tempLabel = new GUIContent();

        public new InspectorField attribute
            => (InspectorField)base.attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && !attribute.ReadOnly;

            EditorGUI.PropertyField(position, property, attribute.Label == null ? label : TempContent(attribute.Label), true);

            GUI.enabled = guiEnabled;
        }

        public static T EditorField<T>(Rect position, GUIContent label, T value, GUIStyle style)
            => (T)EditorField(typeof(T), position, label, value, style);

        public static object EditorField(Type type, Rect position, GUIContent label, object value, GUIStyle style)
        {
            if (style == null)
                style = EditorStyles.textField;

            switch (type)
            {
                case var t when t == typeof(string):
                    return EditorGUI.TextField(position, label, (string)value, style);

                case var t when t == typeof(int):
                    return EditorGUI.IntField(position, label, (int)value, style);

                case var t when t.IsByRef:
                    return EditorGUI.ObjectField(position, label, (UnityEngine.Object)value, type, true);

                default:
                    var previousColor = GUI.color;
                    GUI.color = Color.red;

                    EditorGUI.LabelField(position, label, TempContent($"Unsupported field type '{type.Name}'."), style);

                    GUI.color = previousColor;
                    return value;
            }
        }

        public static GUIContent TempContent(string label)
        {
            _tempLabel.image = null;
            _tempLabel.text = label;
            _tempLabel.tooltip = null;
            return _tempLabel;
        }
    }
}
