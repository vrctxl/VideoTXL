using UnityEngine;

using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(LocalControls))]
    public class LocalControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;
        SerializedProperty staticUrlSourceProperty;
        SerializedProperty volumeControllerProperty;
        //SerializedProperty colorProfileProperty;

        SerializedProperty autoLayoutProperty;
        SerializedProperty enableResyncProprety;
        SerializedProperty enableQualitySelectProperty;
        SerializedProperty enableVolumeProperty;
        SerializedProperty enable2DAudioProperty;
        SerializedProperty enableMessageBarProperty;

        SerializedProperty volumeGroupProperty;
        SerializedProperty resyncGroupProperty;
        SerializedProperty qualityGroupproperty;
        SerializedProperty messageBarGroupProperty;

        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty toggle720OnProperty;
        SerializedProperty toggle720OffProperty;
        SerializedProperty toggle1080OnProperty;
        SerializedProperty toggle1080OffProperty;
        SerializedProperty toggleAudioOnProperty;
        SerializedProperty toggleAudioOffProperty;
        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(LocalControls.videoPlayer));
            staticUrlSourceProperty = serializedObject.FindProperty(nameof(LocalControls.staticUrlSource));
            volumeControllerProperty = serializedObject.FindProperty(nameof(LocalControls.AudioManager));
            //colorProfileProperty = serializedObject.FindProperty(nameof(LocalControls.colorProfile));

            autoLayoutProperty = serializedObject.FindProperty(nameof(LocalControls.autoLayout));
            enableResyncProprety = serializedObject.FindProperty(nameof(LocalControls.enableResync));
            enableQualitySelectProperty = serializedObject.FindProperty(nameof(LocalControls.enableQualitySelect));
            enableVolumeProperty = serializedObject.FindProperty(nameof(LocalControls.enableVolume));
            enable2DAudioProperty = serializedObject.FindProperty(nameof(LocalControls.enable2DAudioToggle));
            enableMessageBarProperty = serializedObject.FindProperty(nameof(LocalControls.enableMessageBar));

            volumeGroupProperty = serializedObject.FindProperty(nameof(LocalControls.volumeGroup));
            resyncGroupProperty = serializedObject.FindProperty(nameof(LocalControls.resyncGroup));
            qualityGroupproperty = serializedObject.FindProperty(nameof(LocalControls.toggleGroup));
            messageBarGroupProperty = serializedObject.FindProperty(nameof(LocalControls.messageBarGroup));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(LocalControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DControl));
            toggle720OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720On));
            toggle720OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720Off));
            toggle1080OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080On));
            toggle1080OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080Off));
            toggleAudioOnProperty = serializedObject.FindProperty(nameof(LocalControls.toggleAudioOn));
            toggleAudioOffProperty = serializedObject.FindProperty(nameof(LocalControls.toggleAudioOff));
            muteToggleOnProperty = serializedObject.FindProperty(nameof(LocalControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(LocalControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(LocalControls.volumeSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(staticUrlSourceProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            //EditorGUILayout.PropertyField(colorProfileProperty);
            EditorGUILayout.PropertyField(autoLayoutProperty);
            if (autoLayoutProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = videoPlayerProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableResyncProprety);
                GUI.enabled = staticUrlSourceProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableQualitySelectProperty);
                GUI.enabled = volumeControllerProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableVolumeProperty);
                EditorGUILayout.PropertyField(enable2DAudioProperty);
                GUI.enabled = messageBarGroupProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableMessageBarProperty);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(volumeGroupProperty);
                EditorGUILayout.PropertyField(resyncGroupProperty);
                EditorGUILayout.PropertyField(qualityGroupproperty);
                EditorGUILayout.PropertyField(messageBarGroupProperty);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(toggle720OnProperty);
                EditorGUILayout.PropertyField(toggle720OffProperty);
                EditorGUILayout.PropertyField(toggle1080OnProperty);
                EditorGUILayout.PropertyField(toggle1080OffProperty);
                EditorGUILayout.PropertyField(toggleAudioOnProperty);
                EditorGUILayout.PropertyField(toggleAudioOffProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                LocalControls lc = (LocalControls)target;
                lc._UpdateLayout();
            }
        }
    }
}
