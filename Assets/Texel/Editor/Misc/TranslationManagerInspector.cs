using UnityEngine;

using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(TranslationManager))]
    public class TranslationManagerInspector : Editor
    {
        static bool _showLangFoldout;

        SerializedProperty parentManagerProperty;
        SerializedProperty translationTableProperty;

        SerializedProperty textKeysProperty;
        SerializedProperty textTargetsProperty;
        SerializedProperty pickupUseKeysProperty;
        SerializedProperty pickupInteractKeysProperty;
        SerializedProperty pickupTargetsProperty;
        SerializedProperty behaviorInteractKeysProperty;
        SerializedProperty behaviorTargetsProperty;

        private void OnEnable()
        {
            parentManagerProperty = serializedObject.FindProperty(nameof(TranslationManager.parentManager));
            translationTableProperty = serializedObject.FindProperty(nameof(TranslationManager.translationTable));

            textKeysProperty = serializedObject.FindProperty(nameof(TranslationManager.textKeys));
            textTargetsProperty = serializedObject.FindProperty(nameof(TranslationManager.textTargets));
            pickupInteractKeysProperty = serializedObject.FindProperty(nameof(TranslationManager.pickupInteractKeys));
            pickupUseKeysProperty = serializedObject.FindProperty(nameof(TranslationManager.pickupUseKeys));
            pickupTargetsProperty = serializedObject.FindProperty(nameof(TranslationManager.pickupTargets));
            behaviorInteractKeysProperty = serializedObject.FindProperty(nameof(TranslationManager.behaviorInteractKeys));
            behaviorTargetsProperty = serializedObject.FindProperty(nameof(TranslationManager.behaviorTargets));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(parentManagerProperty);
            EditorGUILayout.PropertyField(translationTableProperty);

            TargetFoldout("Text Entries", textTargetsProperty, textKeysProperty);
            EditorGUILayout.Space();

            TargetFoldout("Pickups", pickupTargetsProperty, pickupInteractKeysProperty, pickupUseKeysProperty);
            EditorGUILayout.Space();

            TargetFoldout("Behaviours", behaviorTargetsProperty, behaviorInteractKeysProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void TargetFoldout(string name, SerializedProperty targetProp, params SerializedProperty[] keyProps)
        {
            bool showFoldout = EditorGUILayout.Foldout(true, name);
            if (showFoldout)
            {
                int oldCount = targetProp.arraySize;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", targetProp.arraySize));
                if (newCount != oldCount)
                {
                    targetProp.arraySize = newCount;
                    for (int i = 0; i < keyProps.Length; i++)
                        keyProps[i].arraySize = newCount;
                }

                for (int i = 0; i < newCount; i++)
                {
                    SerializedProperty targetField = targetProp.GetArrayElementAtIndex(i);
                    if (i >= oldCount)
                        targetField.stringValue = "";

                    EditorGUILayout.PropertyField(targetField);
                    for (int j = 0; j < keyProps.Length; j++)
                    {
                        SerializedProperty keyField = keyProps[j].GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(keyField);
                    }

                    EditorGUILayout.Space();
                }
            }
        }
    }
}
