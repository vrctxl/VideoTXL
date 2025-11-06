using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(VideoUrlSource), true)]
    public class VideoUrlSourceInspector : Editor
    {
        protected SerializedProperty sourceNameProperty;
        protected SerializedProperty overrideDisplayProperty;

        protected SerializedProperty errorActionProperty;
        protected SerializedProperty retriesExceededActionProperty;
        protected SerializedProperty maxErrorRetryCountProperty;

        protected virtual void OnEnable()
        {
            sourceNameProperty = serializedObject.FindProperty(nameof(VideoUrlSource.sourceName));
            overrideDisplayProperty = serializedObject.FindProperty(nameof(VideoUrlSource.overrideDisplay));
            errorActionProperty = serializedObject.FindProperty(nameof(VideoUrlSource.errorAction));
            retriesExceededActionProperty = serializedObject.FindProperty(nameof(VideoUrlSource.retriesExceededAction));
            maxErrorRetryCountProperty = serializedObject.FindProperty(nameof(VideoUrlSource.maxErrorRetryCount));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderUrlSourceInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void RenderUrlSourceInspector()
        {
            EditorGUILayout.LabelField("Video URL Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceNameProperty);
            EditorGUILayout.PropertyField(overrideDisplayProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Error Handling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(errorActionProperty);
            if (errorActionProperty.enumValueIndex == (int)VideoErrorAction.Retry)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(maxErrorRetryCountProperty);
                EditorGUILayout.PropertyField(retriesExceededActionProperty);
                EditorGUI.indentLevel--;
            }
        }
    }
}
