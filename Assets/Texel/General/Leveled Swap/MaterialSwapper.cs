
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("Texel/State/Material Swapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MaterialSwapper : UdonSharpBehaviour
    {
        [Header("Default State")]
        public bool defaultVR = true;
        public bool defaultDesktop = true;
        public bool defaultQuest = true;

        public MeshRenderer[] meshList;
        public Material[] materialList;
        public int[] indexList;

        Material[] originalMaterialList;

        bool applied = false;

        Component[] handlers;
        int handlerCount = 0;
        string[] handlerEvents;

        private void Start()
        {
            originalMaterialList = new Material[meshList.Length];
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                if (!Utilities.IsValid(materialList[i]))
                {
                    meshList[i] = null;
                    continue;
                }

                Material[] materials = meshList[i].sharedMaterials;
                if (indexList[i] < 0 || indexList[i] >= materials.Length)
                {
                    meshList[i] = null;
                    continue;
                }

                originalMaterialList[i] = materials[indexList[i]];
            }

#if UNITY_ANDROID
            if (defaultQuest)
                SendCustomEventDelayedFrames("_Apply", 1);
#else
            if (Networking.LocalPlayer.IsUserInVR())
            {
                if (defaultVR)
                    SendCustomEventDelayedFrames("_Apply", 1);
            }
            else if (defaultDesktop)
                SendCustomEventDelayedFrames("_Apply", 1);
#endif
        }

        public bool IsApplied
        {
            get { return applied; }
        }

        public void _Reset()
        {
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                Material[] materials = meshList[i].sharedMaterials;
                materials[indexList[i]] = originalMaterialList[i];
                meshList[i].sharedMaterials = materials;
            }

            applied = false;
            _UpdateHandlers();
        }

        public void _Apply()
        {
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                Material[] materials = meshList[i].sharedMaterials;
                materials[indexList[i]] = materialList[i];
                meshList[i].sharedMaterials = materials;
            }

            applied = true;
            _UpdateHandlers();
        }

        public void _Regsiter(Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            for (int i = 0; i < handlerCount; i++)
            {
                if (handlers[i] == handler)
                    return;
            }

            handlers = (Component[])_AddElement(handlers, handler, typeof(Component));
            handlerEvents = (string[])_AddElement(handlerEvents, eventName, typeof(string));

            handlerCount += 1;
        }

        void _UpdateHandlers()
        {
            for (int i = 0; i < handlerCount; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[i];
                script.SendCustomEvent(handlerEvents[i]);
            }
        }

        Array _AddElement(Array arr, object elem, Type type)
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
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(MaterialSwapper))]
    internal class MaterialSwapperInspector : Editor
    {
        static bool _showMaterialListFoldout = true;
        static bool[] _ShowMaterialFoldout = new bool[0];

        SerializedProperty defaultVRProperty;
        SerializedProperty defaultDesktopProperty;
        SerializedProperty defaultQuestProperty;

        SerializedProperty meshListProperty;
        SerializedProperty materialListProperty;
        SerializedProperty indexListProperty;

        private void OnEnable()
        {
            defaultVRProperty = serializedObject.FindProperty(nameof(MaterialSwapper.defaultVR));
            defaultDesktopProperty = serializedObject.FindProperty(nameof(MaterialSwapper.defaultDesktop));
            defaultQuestProperty = serializedObject.FindProperty(nameof(MaterialSwapper.defaultQuest));

            meshListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.meshList));
            materialListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.materialList));
            indexListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.indexList));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(defaultVRProperty);
            EditorGUILayout.PropertyField(defaultDesktopProperty);
            EditorGUILayout.PropertyField(defaultQuestProperty);

            OverrideFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void OverrideFoldout()
        {
            _showMaterialListFoldout = EditorGUILayout.Foldout(_showMaterialListFoldout, "Mesh Material Overrides");
            if (_showMaterialListFoldout)
            {
                EditorGUI.indentLevel++;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", meshListProperty.arraySize));
                if (newCount != meshListProperty.arraySize)
                    meshListProperty.arraySize = newCount;
                if (newCount != materialListProperty.arraySize)
                    materialListProperty.arraySize = newCount;
                if (newCount != indexListProperty.arraySize)
                    indexListProperty.arraySize = newCount;

                if (_ShowMaterialFoldout.Length != meshListProperty.arraySize)
                {
                    _ShowMaterialFoldout = new bool[meshListProperty.arraySize];
                    for (int i = 0; i < _ShowMaterialFoldout.Length; i++)
                        _ShowMaterialFoldout[i] = true;
                }

                for (int i = 0; i < meshListProperty.arraySize; i++)
                {
                    _ShowMaterialFoldout[i] = EditorGUILayout.Foldout(_ShowMaterialFoldout[i], "Override " + i);
                    if (_ShowMaterialFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty mesh = meshListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty mat = materialListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matIndex = indexListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(mesh, new GUIContent("Mesh Renderer"));
                        EditorGUILayout.PropertyField(mat, new GUIContent("Material"));
                        EditorGUILayout.PropertyField(matIndex, new GUIContent("Material Index"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
#endif

}
