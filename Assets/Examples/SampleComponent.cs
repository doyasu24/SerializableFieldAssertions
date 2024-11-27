using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerializableFieldAssertions.Examples
{
    public class SampleComponent : MonoBehaviour
    {
        // UnityEngine.Object marked as [SerializeField] is serialized, and it can be null.
        [SerializeField] private GameObject myGameObject = null!;

        // public field UnityEngine.Object is serialized, and it can be null.
        public Transform MyTransform = null!;

        // UnityEngine.Object marked as [NonSerialized] is not serialized.
        [NonSerialized] public Transform MyTransformNonSerialized = null!;

        // class marked as [Serializable] is serialized, but it cannot be null. Default constructor is called.
        [SerializeField] private MySerializableClass mySerializableClass = null!;

        // ScriptableObject can be null.
        [SerializeField] private MyScriptableObject myScriptableObject = null!;

        // List<T> where T : UnityEngine.Object can contain null elements, but the list itself cannot be null.
        [SerializeField] private List<Transform> myTransforms = null!;

        // T[] where T : UnityEngine.Object can contain null elements, but the array itself cannot be null.
        [SerializeField] private Transform[] myTransformArray = null!;

        // List<T> where T : class marked as [Serializable] cannot contain null elements, and the list itself cannot be null.
        [SerializeField] private List<MySerializableClass> mySerializableClasses = null!;

        // List<T> where T : ScriptableObject can contain null elements, but the list itself cannot be null.
        [SerializeField] private List<MyScriptableObject> myScriptableObjects = null!;

        private void Awake()
        {
            // Asserts that all serializable fields are not null with readable message.
            // 
            // --- output example ---
            // AssertionException: SampleComponent.myGameObject is null.
            // Hierarchy: MyGameObject
            // Scene: TestScene
            // 
            // --- output example (Array or List) ---
            // AssertionException: SampleComponent.myTransformArray contains null elements.
            // Index: 0
            // Hierarchy: MyGameObject
            // Scene: TestScene
            SerializableFieldAssert.AreNotNullAll(this);

            // Asserts that specified serializable field is not null with readable message.
            SerializableFieldAssert.IsNotNull(this, nameof(myGameObject));
        }
    }
}
