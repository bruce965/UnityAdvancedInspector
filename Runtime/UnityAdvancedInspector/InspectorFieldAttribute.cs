using System;
using UnityEngine;

namespace UnityAdvancedInspector
{
    /// <summary>
    /// Show field or property in inspector even if it's not serializable.
    /// <br/>
    /// Requires <see cref="AdvancedInspectorAttribute"/> on the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class InspectorField : PropertyAttribute
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
