
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("Texel/State/Material Swapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MaterialSwapper : UdonSharpBehaviour
    {
        public MeshRenderer[] meshList;
        public Material[] materialList;
        public int[] indexList;

        Material[] originalMaterialList;

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
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(MaterialSwapper))]
    internal class MaterialSwapperInspector : Editor
    {
        static bool _showMaterialListFoldout = true;
        static bool[] _ShowMaterialFoldout = new bool[0];

        SerializedProperty meshListProperty;
        SerializedProperty materialListProperty;
        SerializedProperty indexListProperty;

        private void OnEnable()
        {
            meshListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.meshList));
            materialListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.materialList));
            indexListProperty = serializedObject.FindProperty(nameof(MaterialSwapper.indexList));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

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
