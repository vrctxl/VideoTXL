
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public abstract class UtilityTxl : UdonSharpBehaviour
    {
        public static void ArraySort(int[] arr)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                for (int j = i + 1; j < arr.Length; j++)
                {
                    if (arr[i] > arr[j])
                    {
                        var temp = arr[i];
                        arr[i] = arr[j];
                        arr[j] = temp;
                    }
                }
            }
        }

        public static Array ArrayAddElement(Array arr, object elem, Type type)
        {
            Array newArr;
            int count = 0;

            if (Utilities.IsValid(arr))
            {
                count = arr.Length;
                newArr = Array.CreateInstance(type, count + 1);
                Array.Copy(arr, newArr, count);
            }
            else
                newArr = Array.CreateInstance(type, 1);

            newArr.SetValue(elem, count);
            return newArr;
        }

        public static Array ArrayMinSize(Array arr, int size, Type type)
        {
            if (Utilities.IsValid(arr))
            {
                int count = arr.Length;
                if (count < size)
                {
                    Array newArr = Array.CreateInstance(type, size);
                    Array.Copy(arr, newArr, count);
                    return newArr;
                }
            }

            return arr;
        }

        public static Array ArrayCompact(Array arr)
        {
            if (!Utilities.IsValid(arr))
                return null;

            int size = arr.Length;
            int valid = 0;
            Type type = null;

            for (int i = 0; i < size; i++)
            {
                object val = arr.GetValue(i);
                if (val != null)
                {
                    valid += 1;
                    type = val.GetType();
                }
            }

            if (size == valid)
                return arr;

            if (valid == 0)
                return null;

            Array newArr = Array.CreateInstance(type, valid);
            valid = 0;

            for (int i = 0; i < size; i++)
            {
                object val = arr.GetValue(i);
                if (val != null)
                    newArr.SetValue(val, valid++);
            }

            return newArr;
        }
    }
}
