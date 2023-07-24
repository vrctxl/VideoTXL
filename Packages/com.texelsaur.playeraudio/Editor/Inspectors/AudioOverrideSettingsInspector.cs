using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(AudioOverrideSettings))]
    internal class AudioOverrideSettingsInspector : Editor
    {
        SerializedProperty applyVoiceProperty;
        SerializedProperty voiceGainProperty;
        SerializedProperty voiceNearProperty;
        SerializedProperty voiceFarProperty;
        SerializedProperty voiceVolumetricProperty;
        SerializedProperty voiceLowpassProperty;

        SerializedProperty applyAvatarProperty;
        SerializedProperty avatarGainProperty;
        SerializedProperty avatarNearProperty;
        SerializedProperty avatarFarProperty;
        SerializedProperty avatarVolumetricProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty vrcLoggingProperty;


        private void OnEnable()
        {
            applyVoiceProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.applyVoice));
            voiceGainProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceGain));
            voiceNearProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceNear));
            voiceFarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceFar));
            voiceVolumetricProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceVolumetric));
            voiceLowpassProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceLowpass));
            applyAvatarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.applyAvatar));
            avatarGainProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarGain));
            avatarNearProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarNear));
            avatarFarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarFar));
            avatarVolumetricProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarVolumetric));
            debugLogProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.debugLog));
            vrcLoggingProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.vrcLogging));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.LabelField("Voice Override", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(applyVoiceProperty);
            if (applyVoiceProperty.boolValue)
            {
                EditorGUILayout.PropertyField(voiceGainProperty);
                EditorGUILayout.PropertyField(voiceNearProperty);
                EditorGUILayout.PropertyField(voiceFarProperty);
                EditorGUILayout.PropertyField(voiceVolumetricProperty);
                EditorGUILayout.PropertyField(voiceLowpassProperty);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Avatar Sound Override", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(applyAvatarProperty);
            if (applyAvatarProperty.boolValue)
            {
                EditorGUILayout.PropertyField(avatarGainProperty);
                EditorGUILayout.PropertyField(avatarNearProperty);
                EditorGUILayout.PropertyField(avatarFarProperty);
                EditorGUILayout.PropertyField(avatarVolumetricProperty);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);
            EditorGUILayout.PropertyField(vrcLoggingProperty);

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
