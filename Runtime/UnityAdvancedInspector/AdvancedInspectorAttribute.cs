using System;
using UnityEngine;

namespace UnityAdvancedInspector
{
    /// <summary>
    /// Replace Unity's default editor and enable advanced inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AdvancedInspectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Draw the default inspector editor before the custom one.
        /// </summary>
        /// <value></value>
        public bool DefaultInspector { get; set; } = false;
    }
}