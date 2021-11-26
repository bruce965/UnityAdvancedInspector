using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace UnityAdvancedInspector.Editor
{
    static class ReflectionExtensions
    {
        class GetSet
        {
            public Func<object, object> Get { get; }
            public Action<object, object> Set { get; }

            public GetSet(Func<object, object> get, Action<object, object> set)
            {
                Get = get;
                Set = set;
            }
        }

        static readonly ConditionalWeakTable<MethodInfo, GetSet> byRefGetSet
            = new ConditionalWeakTable<MethodInfo, GetSet>();

        /// <summary>
        /// Try to get an <see cref="Attribute"/> from a class, recursively visiting all base classes.
        /// </summary>
        /// <param name="type">Specialization of the class where to look for the attribute.</param>
        /// <param name="baseType">Class where the attribute was found.</param>
        /// <param name="attribute">Attribute.</param>
        /// <typeparam name="T">Attribute type.</typeparam>
        /// <returns><c>true</c> if attribute was found.</returns>
        public static bool TryGetCustomAttributeRecursively<T>(this Type type, out Type baseType, out T attribute)
            where T : Attribute
        {
            do
            {
                attribute = type.GetCustomAttribute<T>();
                if (attribute != null)
                {
                    baseType = type;
                    return true;
                }
            }
            while ((type = type.BaseType) != null);

            baseType = null;
            attribute = null;
            return false;
        }

        public static bool CanRead(this MemberInfo member)
        {
            if (member is FieldInfo)
                return true;

            if (member is PropertyInfo property)
                return property.GetGetMethod(true) != null;

            return false;
        }

        public static bool CanWrite(this MemberInfo member)
        {
            if (member is FieldInfo)
                return true;

            if (member is PropertyInfo property)
                return property.GetSetMethod(true) != null;

            return false;
        }

        public static Type GetFieldOrPropertyType(this MemberInfo member)
        {
            if (member is FieldInfo field)
                return field.FieldType;

            if (member is PropertyInfo property)
                return property.PropertyType;

            throw new NotSupportedException($"Member type not supported.");
        }

        public static bool TryGetValue<T>(this MemberInfo member, object target, out T value)
        {
            var memberType = member.GetFieldOrPropertyType();

            if (memberType.IsByRef && typeof(T).MakeByRefType().IsEquivalentTo(memberType))
            {
                if (member is PropertyInfo property)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter == null)
                    {
                        value = default;
                        return false;
                    }

                    value = (T)InvokeResolveByRef(getter).Get(target);
                    return true;
                }
            }

            if (typeof(T).IsAssignableFrom(memberType))
            {
                if (member is FieldInfo field)
                {
                    value = (T)field.GetValue(target);
                    return true;
                }

                if (member is PropertyInfo property)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter == null)
                    {
                        value = default;
                        return false;
                    }

                    value = (T)getter.Invoke(target, null);
                    return true;
                }
            }

            throw new NotSupportedException($"Member type not supported.");
        }

        public static bool TrySetValue<T>(this MemberInfo member, object target, T value)
        {
            var memberType = member.GetFieldOrPropertyType();

            if (memberType.IsByRef && typeof(T).MakeByRefType().IsEquivalentTo(memberType))
            {
                if (member is PropertyInfo property)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter == null)
                        return false;

                    InvokeResolveByRef(getter).Set(target, value);
                    return true;
                }
            }

            if (memberType.IsAssignableFrom(typeof(T)))
            {
                if (member is FieldInfo field)
                {
                    field.SetValue(target, value);
                    return true;
                }

                if (member is PropertyInfo property)
                {
                    var setter = property.GetSetMethod(true);
                    if (setter == null)
                        return false;

                    setter.Invoke(target, new object[] { value });
                    return true;
                }
            }

            return false;
        }

        static GetSet InvokeResolveByRef(this MethodInfo getter)
        {
            return byRefGetSet.GetValue(getter, getter =>
            {
                if (!getter.ReturnType.IsByRef)
                    throw new ArgumentException("Only \"return by-ref\" methods are supported.", nameof(getter));

                var assemblyName = new AssemblyName($"{typeof(ReflectionExtensions).FullName}.ResolveByRef_{getter.ReflectedType.FullName}.{getter.Name}");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName,
                    #if DEBUG
                    AssemblyBuilderAccess.RunAndSave
                    #else
                    AssemblyBuilderAccess.Run
                    #endif
                    );

                var moduleName = assemblyName.FullName;
                var fileName = $"{moduleName}.dll";
                var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName, fileName);

                var typeBuilder = moduleBuilder.DefineType("Resolver", TypeAttributes.Public);

                var elementType = getter.ReturnType.GetElementType();

                var getMethodBuilder = typeBuilder.DefineMethod(
                    "Get",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard,
                    typeof(object),
                    new[] { getter.ReflectedType });

                var setMethodBuilder = typeBuilder.DefineMethod(
                    "Set",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard,
                    null,
                    new[] { getter.ReflectedType, typeof(object) });

                var get = getMethodBuilder.GetILGenerator();
                get.Emit(OpCodes.Ldarg_0);
                get.Emit(OpCodes.Callvirt, getter);
                get.Emit(Ldind_Type(elementType));
                if (elementType.IsValueType)
                    get.Emit(OpCodes.Box, elementType);
                get.Emit(OpCodes.Ret);

                var set = setMethodBuilder.GetILGenerator();
                set.Emit(OpCodes.Ldarg_0);
                set.Emit(OpCodes.Callvirt, getter);
                set.Emit(OpCodes.Ldarg_1);
                if (elementType.IsValueType)
                    set.Emit(OpCodes.Unbox_Any, elementType);
                set.Emit(Stind_Type(elementType));
                set.Emit(OpCodes.Ret);

                var type = typeBuilder.CreateType();
                var getMethod = type.GetMethod(getMethodBuilder.Name);
                var setMethod = type.GetMethod(setMethodBuilder.Name);

                #if DEBUG

                //assemblyBuilder.Save(fileName);

                #endif

                return new GetSet(
                    get: target => getMethod.Invoke(null, new[] { target }),
                    set: (target, value) => setMethod.Invoke(null, new[] { target, value })
                );
            });
        }

        static OpCode Ldind_Type(Type type)
        {
            switch (type.UnderlyingSystemType)
            {
                case var t when t == typeof(byte): return OpCodes.Ldind_U1;
                case var t when t == typeof(char): return OpCodes.Ldind_U2;
                case var t when t == typeof(decimal): return OpCodes.Ldobj;
                case var t when t == typeof(double): return OpCodes.Ldind_R8;
                case var t when t == typeof(int): return OpCodes.Ldind_I4;
                case var t when t == typeof(long): return OpCodes.Ldind_I8;
                //case var t when t == typeof(nint): return OpCodes.Ldind_I;  // TODO: is `Ldind_I` correct?
                //case var t when t == typeof(nuint): return OpCodes.Ldind_I;  // TODO: is `Ldind_I` correct?
                case var t when t == typeof(object): return OpCodes.Ldind_Ref;
                case var t when t == typeof(sbyte): return OpCodes.Ldind_I1;
                case var t when t == typeof(short): return OpCodes.Ldind_I2;
                case var t when t == typeof(short): return OpCodes.Ldind_R4;
                case var t when t == typeof(uint): return OpCodes.Ldind_U4;
                case var t when t == typeof(ulong): return OpCodes.Ldind_I8;
                case var t when t == typeof(ushort): return OpCodes.Ldind_U2;

                default:
                    throw new NotSupportedException($"Unsupported type: '{type.UnderlyingSystemType}'");
            }
        }

        static OpCode Stind_Type(Type type)
        {
            switch (type.UnderlyingSystemType)
            {
                case var t when t == typeof(byte): return OpCodes.Stind_I1;
                case var t when t == typeof(char): return OpCodes.Stind_I2;
                case var t when t == typeof(decimal): return OpCodes.Stobj;
                case var t when t == typeof(double): return OpCodes.Stind_R8;
                case var t when t == typeof(int): return OpCodes.Stind_I4;
                case var t when t == typeof(long): return OpCodes.Stind_I8;
                //case var t when t == typeof(nint): return OpCodes.Ldind_I;  // TODO: is `Ldind_I` correct?
                //case var t when t == typeof(nuint): return OpCodes.Ldind_I;  // TODO: is `Ldind_I` correct?
                case var t when t == typeof(object): return OpCodes.Stind_Ref;
                case var t when t == typeof(sbyte): return OpCodes.Stind_I1;
                case var t when t == typeof(short): return OpCodes.Stind_I2;
                case var t when t == typeof(short): return OpCodes.Stind_R4;
                case var t when t == typeof(uint): return OpCodes.Stind_I4;
                case var t when t == typeof(ulong): return OpCodes.Stind_I8;
                case var t when t == typeof(ushort): return OpCodes.Stind_I2;

                default:
                    throw new NotSupportedException($"Unsupported type: '{type.UnderlyingSystemType}'");
            }
        }
    }
}
