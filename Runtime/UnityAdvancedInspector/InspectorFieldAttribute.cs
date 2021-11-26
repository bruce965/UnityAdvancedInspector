using System;
using UnityEngine;

namespace UnityAdvancedInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class InspectorFieldAttribute : PropertyAttribute
    {
        /// <summary>
        /// Optional label to display in front of the text field.
        /// </summary>
        /// <value></value>
        public string Label { get; set; }

        /// <summary>
        /// Is this GUI element disabled (read-only)?
        /// </summary>
        /// <value></value>
        public bool Disabled { get; set; } = false;
    }
}
