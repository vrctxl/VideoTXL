using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace Texel
{
    public class EditorTools
    {
        public static bool[] MultiArraySize(SerializedObject serializedObject, bool[] foldoutArray, params SerializedProperty[] props)
        {
            if (props.Length == 0)
                return foldoutArray;

            int oldCount = props[0].arraySize;
            int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", props[0].arraySize));
            if (newCount != oldCount)
            {
                for (int i = oldCount; i < newCount; i++)
                {
                    for (int j = 0; j < props.Length; j++)
                        props[j].InsertArrayElementAtIndex(i);
                }

                for (int j = 0; j < props.Length; j++)
                    props[j].arraySize = newCount;

                serializedObject.ApplyModifiedProperties();
            }

            foreach (SerializedProperty prop in props)
            {
                if (prop.arraySize != newCount)
                    prop.arraySize = newCount;
            }

            if (foldoutArray.Length != newCount)
                Array.Resize(ref foldoutArray, newCount);

            return foldoutArray;
        }

        public static string GetObjectName(SerializedProperty list, int index)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            string name = "none";
            if (entry != null && entry.objectReferenceValue != null)
                name = ((MonoBehaviour)entry.objectReferenceValue).name;

            return name;
        }

        public static string GetMeshRendererName(SerializedProperty list, int index)
        {
            SerializedProperty mesh = list.GetArrayElementAtIndex(index);
            string name = "none";
            if (mesh != null && mesh.objectReferenceValue != null)
                name = ((MeshRenderer)mesh.objectReferenceValue).name;

            return name;
        }

        public static string GetMaterialName(SerializedProperty list, int index)
        {
            SerializedProperty matUpdate = list.GetArrayElementAtIndex(index);
            string name = "none";
            if (matUpdate != null && matUpdate.objectReferenceValue != null)
                name = ((Material)matUpdate.objectReferenceValue).name;

            return name;
        }
    }
}