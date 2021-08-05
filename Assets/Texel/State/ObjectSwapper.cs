
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
    [AddComponentMenu("Texel/State/Object Swapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ObjectSwapper : UdonSharpBehaviour
    {
        public GameObject[] objectList;
        public GameObject[] replacementList;

        public void _Reset()
        {
            for (int i = 0; i < objectList.Length; i++)
            {
                if (Utilities.IsValid(objectList[i]))
                    objectList[i].SetActive(true);

                if (Utilities.IsValid(replacementList[i]))
                    replacementList[i].SetActive(false);
            }
        }

        public void _Apply()
        {
            for (int i = 0; i < objectList.Length; i++)
            {
                if (Utilities.IsValid(objectList[i]))
                    objectList[i].SetActive(false);

                if (Utilities.IsValid(replacementList[i]))
                    replacementList[i].SetActive(true);
            }
        }
    }


#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(ObjectSwapper))]
    internal class ObjectSwapperInspector : Editor
    {
        static bool _showObjectListFoldout = true;
        static bool[] _ShowObjectFoldout = new bool[0];

        SerializedProperty objectListProperty;
        SerializedProperty replacementListProperty;

        private void OnEnable()
        {
            objectListProperty = serializedObject.FindProperty(nameof(ObjectSwapper.objectList));
            replacementListProperty = serializedObject.FindProperty(nameof(ObjectSwapper.replacementList));
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
            _showObjectListFoldout = EditorGUILayout.Foldout(_showObjectListFoldout, "Object Overrides");
            if (_showObjectListFoldout)
            {
                EditorGUI.indentLevel++;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", objectListProperty.arraySize));
                if (newCount != objectListProperty.arraySize)
                    objectListProperty.arraySize = newCount;
                if (newCount != replacementListProperty.arraySize)
                    replacementListProperty.arraySize = newCount;

                if (_ShowObjectFoldout.Length != objectListProperty.arraySize)
                {
                    _ShowObjectFoldout = new bool[objectListProperty.arraySize];
                    for (int i = 0; i < _ShowObjectFoldout.Length; i++)
                        _ShowObjectFoldout[i] = true;
                }

                for (int i = 0; i < objectListProperty.arraySize; i++)
                {
                    _ShowObjectFoldout[i] = EditorGUILayout.Foldout(_ShowObjectFoldout[i], "Override " + i);
                    if (_ShowObjectFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty obj = objectListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty repl = replacementListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(obj, new GUIContent("Object"));
                        EditorGUILayout.PropertyField(repl, new GUIContent("Replacement"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}