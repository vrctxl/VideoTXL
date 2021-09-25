
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
    [AddComponentMenu("Texel/Audio/Audio Override Settings")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideSettings : UdonSharpBehaviour
    {
        public bool applyVoice = true;
        public float voiceGain = 15;
        public float voiceNear = 0;
        public float voiceFar = 25;
        public bool voiceLowpass = true;

        public bool applyAvatar = true;
        public float avatarGain = 10;
        public float avatarNear = 0;
        public float avatarFar = 40;

        public DebugLog debugLog;
        public bool vrcLogging = false;

        public void _Apply(VRCPlayerApi player)
        {
            if (applyVoice)
            {
                DebugLog($"Setting voice override for {player.displayName} ({player.playerId}): {voiceGain}, {voiceNear}, {voiceFar}, {voiceLowpass}");

                player.SetVoiceGain(voiceGain);
                player.SetVoiceDistanceNear(voiceNear);
                player.SetVoiceDistanceFar(voiceFar);
                player.SetVoiceLowpass(voiceLowpass);
            }

            if (applyAvatar)
            {
                DebugLog($"Setting avatar override for {player.displayName} ({player.playerId}): {avatarGain}, {avatarNear}, {avatarFar}");
                player.SetAvatarAudioGain(avatarGain);
                player.SetAvatarAudioNearRadius(avatarNear);
                player.SetAvatarAudioFarRadius(avatarFar);
            }
        }

        void DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[Texel:AudioOverride] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioOverride", message);
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(AudioOverrideSettings))]
    internal class AudioOverrideSettingsInspector : Editor
    {
        SerializedProperty applyVoiceProperty;
        SerializedProperty voiceGainProperty;
        SerializedProperty voiceNearProperty;
        SerializedProperty voiceFarProperty;
        SerializedProperty voiceLowpassProperty;

        SerializedProperty applyAvatarProperty;
        SerializedProperty avatarGainProperty;
        SerializedProperty avatarNearProperty;
        SerializedProperty avatarFarProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty vrcLoggingProperty;


        private void OnEnable()
        {
            applyVoiceProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.applyVoice));
            voiceGainProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceGain));
            voiceNearProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceNear));
            voiceFarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceFar));
            voiceLowpassProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.voiceLowpass));
            applyAvatarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.applyAvatar));
            avatarGainProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarGain));
            avatarNearProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarNear));
            avatarFarProperty = serializedObject.FindProperty(nameof(AudioOverrideSettings.avatarFar));
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
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);
            EditorGUILayout.PropertyField(vrcLoggingProperty);

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
