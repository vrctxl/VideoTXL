using UnityEngine;

using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(ObjectSwapper))]
    public class ObjectSwapperInspector : Editor
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
}
