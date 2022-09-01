using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(BasicPlayerControls))]
    public class BasicPlayerControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;

        SerializedProperty urlInputProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;

        SerializedProperty stopIconProperty;
        SerializedProperty lockedIconProperty;
        SerializedProperty unlockedIconProperty;
        SerializedProperty loadIconProperty;
        SerializedProperty syncIconProperty;

        SerializedProperty progressSliderProperty;
        SerializedProperty statusTextProperty;
        SerializedProperty urlTextProperty;
        SerializedProperty placeholderTextProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.videoPlayer));
            urlInputProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlInput));

            progressSliderControlProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.progressSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlInputControl));

            stopIconProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.stopIcon));
            lockedIconProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.lockedIcon));
            unlockedIconProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.unlockedIcon));
            loadIconProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.loadIcon));
            syncIconProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.syncIcon));

            statusTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.statusText));
            placeholderTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.placeholderText));
            urlTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlText));
            progressSliderProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.progressSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(stopIconProperty);
                EditorGUILayout.PropertyField(lockedIconProperty);
                EditorGUILayout.PropertyField(unlockedIconProperty);
                EditorGUILayout.PropertyField(loadIconProperty);
                EditorGUILayout.PropertyField(syncIconProperty);
                EditorGUILayout.PropertyField(progressSliderProperty);
                EditorGUILayout.PropertyField(statusTextProperty);
                EditorGUILayout.PropertyField(urlTextProperty);
                EditorGUILayout.PropertyField(placeholderTextProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
