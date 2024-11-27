using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace SerializableFieldAssertions
{
    public static class SerializableFieldAssert
    {
        /// <summary>
        /// Asserts that all serializable fields are not null with readable message.
        /// Nullable fields are ignored.
        /// </summary>
        /// https://docs.unity3d.com/ja/2022.3/Manual/script-Serialization.html
        /// <param name="component"></param>
        [Conditional("UNITY_ASSERTIONS")] // same as UnityEngine.Assertions.Assert.IsNotNull
        public static void AreNotNullAll(Component component)
        {
            var type = component.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (!IsMaybeSerializableField(field)) continue;
                if (IsNullableField(field)) continue;
                AssertNotNullEach(component, type, field);
            }
        }

        /// <summary>
        /// Asserts that specified serializable field is not null with readable message.
        /// </summary>
        /// https://docs.unity3d.com/ja/2022.3/Manual/script-Serialization.html
        /// <param name="component"></param>
        /// <param name="fieldName">specified field name. ex: nameof(field)</param>
        [Conditional("UNITY_ASSERTIONS")] // same as UnityEngine.Assertions.Assert.IsNotNull
        public static void IsNotNull(Component component, string fieldName)
        {
            var type = component.GetType();
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new ArgumentException($"Field not found: \"{fieldName}\" in {type.Name}");
            if (!IsMaybeSerializableField(field))
                throw new ArgumentException($"Field is not serializable: {fieldName} in {type.Name}");
            if (IsNullableField(field)) throw new ArgumentException($"Field is nullable: {fieldName} in {type.Name}");
            AssertNotNullEach(component, type, field);
        }

        private static void AssertNotNullEach(Component component, Type componentType, FieldInfo field)
        {
            var value = field.GetValue(component);
            var fieldType = field.FieldType;
            if (typeof(Object).IsAssignableFrom(fieldType)) // ex. Component, GameObject, ScriptableObject
            {
                var obj = value as Object;
                Assert.IsNotNull(obj,
                    $"{componentType.Name}.{field.Name} is null.\nHierarchy: {GetHierarchyPath(component.transform)}\nScene: {component.gameObject.scene.name}");
            }
            // List<T> where T : UnityEngine.Object can contain null elements.
            else if (IsObjectList(fieldType))
            {
                var list = (IList)value; // list cannot be null
                for (var i = 0; i < list.Count; i++)
                {
                    var obj = list[i] as Object;
                    Assert.IsNotNull(obj,
                        $"{componentType.Name}.{field.Name} contains null elements.\nIndex: {i}\nHierarchy: {GetHierarchyPath(component.transform)}\nScene: {component.gameObject.scene.name}");
                }
            }
            // T[] where T : UnityEngine.Object can contain null elements.
            else if (IsObjectArray(fieldType))
            {
                var array = (Object[])value; // array cannot be null
                for (var i = 0; i < array.Length; i++)
                {
                    Assert.IsNotNull(array[i],
                        $"{componentType.Name}.{field.Name} contains null elements.\nIndex: {i}\nHierarchy: {GetHierarchyPath(component.transform)}\nScene: {component.gameObject.scene.name}");
                }
            }
        }

        /// <summary>
        /// True if the specified type is T[] where T : UnityEngine.Object.
        /// </summary>
        private static bool IsObjectArray(Type type) =>
            type.IsArray && typeof(Object).IsAssignableFrom(type.GetElementType());

        /// <summary>
        /// True if the specified type is List&lt;T&gt; where T : UnityEngine.Object.
        /// </summary>
        private static bool IsObjectList(Type type)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>)) return false;
            var genericArgument = type.GetGenericArguments()[0];
            return typeof(Object).IsAssignableFrom(genericArgument);
        }


        /// <summary>
        /// maybe serializable field:<br />
        /// - [SerializeField]<br />
        /// - public (not marked with [NonSerialized])
        /// </summary>
        private static bool IsMaybeSerializableField(FieldInfo field)
        {
            // Priority: [NonSerialized] > [SerializeField]
            // If the field is marked with [NonSerialized] and [SerializeField], [SerializeField] will be ignored.

            // Check if the field is marked with [NonSerialized] -> not serializable
            if (field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0) return false;

            // Check if the field is marked with [SerializeField] -> serializable
            if (field.GetCustomAttributes(typeof(SerializeField), false).Length > 0) return true;

            // Check if the field is public -> serializable
            if (field.IsPublic) return true;

            // not public and not marked with [SerializeField] -> not serializable
            return false;
        }

        private static bool IsNullableField(FieldInfo field)
        {
            // System.Runtime.CompilerServices.NullableAttribute is internal, so it cannot be accessed.
            // Instead, AttributeType.FullName is used to check if it is Nullable.
            const string nullableAttributeFullName = "System.Runtime.CompilerServices.NullableAttribute";
            var nullableAttribute = field.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName == nullableAttributeFullName);
            return nullableAttribute != null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }
    }
}
