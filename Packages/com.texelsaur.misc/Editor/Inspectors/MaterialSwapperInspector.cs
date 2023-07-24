using UnityEngine;

using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(MaterialSwapper))]
    public class MaterialSwapperInspector : Editor
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
}
