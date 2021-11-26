using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityAdvancedInspector.Editor
{
    [CustomEditor(typeof(Object), true)]
    class AdvancedEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!target.GetType().TryGetCustomAttributeRecursively<InspectorEditorAttribute>(out var type, out var editor))
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
                    var inspector = member.GetCustomAttribute<InspectorFieldAttribute>();
                    if (inspector == null)
                        continue;

                    typeof(AdvancedEditor)
                        .GetMethod(nameof(DrawMember), BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(member.GetFieldOrPropertyType())
                        .Invoke(this, new object[] { inspector, target, member });
                }
            }
        }

        void DrawMember<T>(InspectorFieldAttribute inspector, Object target, MemberInfo member)
        {
            var guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && !inspector.Disabled;

            var label = inspector.Label ?? member.Name;

            if (!member.TryGetValue<T>(target, out var value))
            {
                EditorGUILayout.LabelField(label, $"Failed to read.");
                return;
            }

            var newValue = InspectorFieldDrawer.LayoutField(label, value);

            if (member.CanWrite())
            {
                var isChanged = !ReferenceEquals(newValue, value) && (newValue == null || !newValue.Equals(value));
                if (isChanged)
                {
                    Debug.Log($"Value changed. Was '{value}', is now '{newValue}'");
                    if (!member.TrySetValue(target, newValue))
                        Debug.Assert(false, target);  // will never happen
                }
            }

            GUI.enabled = guiEnabled;
        }
    }
}
