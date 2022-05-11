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
                return;

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

            bool[] foldoutReturn = foldoutArray;
            if (foldoutArray.Length != newCount)
            {
                foldoutReturn = new bool[newCount];
                Array.Copy(foldoutArray, foldoutReturn, Math.Min(oldCount, newCount));
            }

            return foldoutReturn;
        }

        public static string GetMeshRendererName(SerializedProperty list, int index)
        {
            SerializedProperty mesh = list.GetArrayElementAtIndex(index);
            string name = "none";
            if (mesh != null && mesh.objectReferenceValue != null)
                name = ((MeshRenderer)mesh.objectReferenceValue).name;

            return name;
        }

        public static string GetMaterialName(SerializedPropertyList list, int index)
        {
            SerializedProperty matUpdate = list.GetArrayElementAtIndex(index);
            string name = "none";
            if (matUpdate != null && matUpdate.objectReferenceValue != null)
                name = ((Material)matUpdate.objectReferenceValue).name;

            return name;
        }
    }
}