using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityAdvancedInspector.Editor
{
    [CustomEditor(typeof(Object), true)]
    class AdvancedInspectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!target.GetType().TryGetCustomAttributeRecursively<AdvancedInspectorAttribute>(out var type, out var editor))
            {
                base.OnInspectorGUI();
                return;
            }

            using (new LocalizationGroup(target))
            {
                if (editor.DefaultInspector)
                    DrawDefaultInspector();

                serializedObject.UpdateIfRequiredOrScript();

                Undo.RecordObject(target, "Inspector");

                foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    var inspector = member.GetCustomAttribute<InspectorField>();
                    if (inspector == null)
                        continue;

                    // TODO: find a way to detect when property is edited and show in inspector.

                    typeof(AdvancedInspectorEditor)
                        .GetMethod(nameof(DrawMember), BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(member.GetFieldOrPropertyType())
                        .Invoke(this, new object[] { inspector, target, member });
                }
            }
        }

        void DrawMember<T>(InspectorField inspector, Object target, MemberInfo member)
        {
            var guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && !inspector.Disabled;

            var label = inspector.Label ?? member.Name;

            if (!member.TryGetValue<T>(target, out var value))
            {
                var previousColor = GUI.color;
                GUI.color = Color.red;

                EditorGUILayout.LabelField(label, "Failed to read.");

                GUI.color = previousColor;
                return;
            }

            var position = EditorGUILayout.GetControlRect();
            var newValue = InspectorFieldDrawer.EditorField(position, InspectorFieldDrawer.TempContent(label), value, null);

            if (member.CanWrite())
            {
                var isChanged = !ReferenceEquals(newValue, value) && (newValue == null || !newValue.Equals(value));
                if (isChanged)
                {
                    //Debug.Log($"Value changed. Was '{value}', is now '{newValue}'");

                    if (!member.TrySetValue(target, newValue))
                        Debug.Assert(false, target);  // will never happen
                }
            }

            GUI.enabled = guiEnabled;
        }
    }
}
