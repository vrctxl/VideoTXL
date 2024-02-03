using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public static class SerializeUtility
    {
        static SerializedProperty GetElementSafe(SerializedProperty arr, int index)
        {
            if (arr.arraySize <= index)
                arr.arraySize = index + 1;
            return arr.GetArrayElementAtIndex(index);
        }

        public static int AddElement(SerializedProperty main, params SerializedProperty[] props)
        {
            int index = main.arraySize;

            main.arraySize++;
            foreach (var prop in props)
                prop.arraySize++;

            return index;
        }

        public static int AddElement(params SerializedProperty[] props)
        {
            int index = 0;
            if (props.Length == 0)
                return index;

            index = props[0].arraySize;
            foreach (var prop in props)
                prop.arraySize++;

            return index;
        }

        public static void RemoveElement(params SerializedProperty[] props)
        {
            foreach (var prop in props)
                prop.arraySize--;
        }

        public static void RemoveElementAt(int index, params SerializedProperty[] props)
        {
            foreach (var prop in props)
            {
                int sz = prop.arraySize;
                prop.DeleteArrayElementAtIndex(index);

                // Sometimes we have to try twice :[
                if (prop.arraySize == sz)
                    prop.DeleteArrayElementAtIndex(index);
            }
        }

        public static bool objectRefValid(SerializedProperty propList, int index)
        {
            SerializedProperty matProperties = propList.GetArrayElementAtIndex(index);
            return matProperties.objectReferenceValue != null;
        }
    }
}
