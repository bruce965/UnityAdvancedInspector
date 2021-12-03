using System;
using UnityEngine;

public class RequiresTypeAttribute : PropertyAttribute
{
    public Type Type { get; }

    public RequiresTypeAttribute(Type type)
    {
        Type = type;
    }
}
