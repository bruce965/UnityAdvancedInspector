using UnityEngine;
using UnityEditor;

namespace UnityAdvancedInspector.Editor
{
    [CustomPropertyDrawer(typeof(RequiresTypeAttribute))]
    public class RequiresTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var requiredAttribute = this.attribute as RequiresTypeAttribute;

                var content = EditorGUI.BeginProperty(position, label, property);

                property.objectReferenceValue = EditorGUI.ObjectField(position, content, property.objectReferenceValue, requiredAttribute.Type, true);

                EditorGUI.EndProperty();
            }
            else
            {
                var previousColor = GUI.color;
                GUI.color = Color.red;

                EditorGUI.LabelField(position, label, InspectorFieldDrawer.TempContent("Property is not a reference type"));

                GUI.color = previousColor;
            }
        }
    }
}
