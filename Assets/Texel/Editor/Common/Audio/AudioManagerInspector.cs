
using UnityEngine;
using VRC.Udon;

using UnityEditor;
using UdonSharpEditor;
using System;

namespace Texel
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerInspector : Editor
    {
        static bool _showChannelListFoldout = true;
        static bool[] _showChannelFoldout = new bool[0];

        SerializedProperty videoPlayerProperty;
        SerializedProperty muteSourcePropertyProperty;

        SerializedProperty enableSyncProperty;
        SerializedProperty syncAudioManagerProperty;

        SerializedProperty inputVolumeProperty;
        SerializedProperty inputMuteProperty;
        SerializedProperty masterVolumeProperty;
        SerializedProperty masterMuteProperty;
        SerializedProperty master2DProperty;

        SerializedProperty channelAudioListProperty;
        SerializedProperty channelNameListProperty;
        SerializedProperty channelVolumeListProperty;
        SerializedProperty channelMuteListProperty;
        SerializedProperty channel2DListProperty;
        SerializedProperty channelFadeZoneListProperty;

        SerializedProperty channel2DMuteListProperty;
        SerializedProperty channel2DVolumeListProperty;
        SerializedProperty channel2DSeparateVolumeListProperty;
        SerializedProperty channel2DFadeDisableListProperty;

        SerializedProperty audioLinkProperty;
        SerializedProperty audioLinkChannelProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(AudioManager.videoPlayer));
            muteSourcePropertyProperty = serializedObject.FindProperty(nameof(AudioManager.muteSourceForInactiveVideo));

            enableSyncProperty = serializedObject.FindProperty(nameof(AudioManager.useSync));
            syncAudioManagerProperty = serializedObject.FindProperty(nameof(AudioManager.syncAudioManager));

            inputVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.inputVolume));
            inputMuteProperty = serializedObject.FindProperty(nameof(AudioManager.inputMute));
            masterVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.masterVolume));
            masterMuteProperty = serializedObject.FindProperty(nameof(AudioManager.masterMute));
            master2DProperty = serializedObject.FindProperty(nameof(AudioManager.master2D));

            channelAudioListProperty = serializedObject.FindProperty(nameof(AudioManager.channelAudio));
            channelNameListProperty = serializedObject.FindProperty(nameof(AudioManager.channelNames));
            channelVolumeListProperty = serializedObject.FindProperty(nameof(AudioManager.channelVolume));
            channelMuteListProperty = serializedObject.FindProperty(nameof(AudioManager.channelMute));
            channel2DListProperty = serializedObject.FindProperty(nameof(AudioManager.channel2D));
            channelFadeZoneListProperty = serializedObject.FindProperty(nameof(AudioManager.channelFadeZone));

            channel2DMuteListProperty = serializedObject.FindProperty(nameof(AudioManager.channelMute2D));
            channel2DVolumeListProperty = serializedObject.FindProperty(nameof(AudioManager.channelVolume2D));
            channel2DSeparateVolumeListProperty = serializedObject.FindProperty(nameof(AudioManager.channelSeparateVolume2D));
            channel2DFadeDisableListProperty = serializedObject.FindProperty(nameof(AudioManager.channelDisableFade2D));

            audioLinkProperty = serializedObject.FindProperty(nameof(AudioManager.audioLinkSystem));
            audioLinkChannelProperty = serializedObject.FindProperty(nameof(AudioManager.audioLinkChannel));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent("Video Player", "Optional reference to video player acting as input to the audio manager"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSyncProperty, new GUIContent("Enable Sync", "Enable syncing of audio manager settings across all players, excluding local overrides like local volume controls or fade zones.  Sync is not needed for most video player setups."));
            if (enableSyncProperty.boolValue)
                EditorGUILayout.PropertyField(syncAudioManagerProperty, new GUIContent("Sync Audio Manager", "Separate sync component attached to the Audio Manager"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inputVolumeProperty, new GUIContent("Input Volume", "The default volume of the input source.  Allows input to be controlled without user override.  Normally left at 1."));
            EditorGUILayout.PropertyField(inputMuteProperty, new GUIContent("Input Mute", "Whether the input source is muted by default"));
            EditorGUILayout.PropertyField(masterVolumeProperty, new GUIContent("Master Volume", "The default master volume. Can be overridden locally by users."));
            EditorGUILayout.PropertyField(masterMuteProperty, new GUIContent("Master Mute", "Whethre all audio is muted by default"));
            EditorGUILayout.PropertyField(master2DProperty, new GUIContent("Master 2D", "Whether all audio is forced to 2D spatial blend by default.  Master 2D also invokes several separate per-channel settings."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Channels", EditorStyles.boldLabel);
            ChannelFoldout();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioLink", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(audioLinkProperty, new GUIContent("AudioLink", "Set the main AudioLink script to have the AudioManager update it as audio sources are changed out."));
            EditorGUILayout.PropertyField(audioLinkChannelProperty, new GUIContent("AudioLink Channel", "The name of the audio channel that should be assigned to AudioLink.  If not channel is specified, or the channel is not used by a given source, the first available channel will be used instead."));
            if (GUILayout.Button(new GUIContent("Link AudioLink to this manager", "Finds first AudioLink UdonBehaviour and sets its reference automatically.")))
                LinkAudioLink();

            serializedObject.ApplyModifiedProperties();
        }

        private void ChannelFoldout()
        {
            int count = channelAudioListProperty.arraySize;

            _showChannelListFoldout = EditorGUILayout.Foldout(_showChannelListFoldout, $"Channels ({count})");
            if (!_showChannelListFoldout)
                return;

            EditorGUI.indentLevel++;
            _showChannelFoldout = EditorTools.MultiArraySize(serializedObject, _showChannelFoldout,
                channelAudioListProperty, channelNameListProperty, channelVolumeListProperty, channelMuteListProperty, channel2DListProperty, channelFadeZoneListProperty,
                channel2DMuteListProperty, channel2DVolumeListProperty, channel2DSeparateVolumeListProperty, channel2DFadeDisableListProperty);

            for (int i = 0; i < channelAudioListProperty.arraySize; i++)
            {
                SerializedProperty name = channelNameListProperty.GetArrayElementAtIndex(i);

                _showChannelFoldout[i] = EditorGUILayout.Foldout(_showChannelFoldout[i], $"Channel {i} ({name.stringValue})");
                if (!_showChannelFoldout[i])
                    continue;

                EditorGUI.indentLevel++;

                SerializedProperty audioSource = channelAudioListProperty.GetArrayElementAtIndex(i);
                SerializedProperty volume = channelVolumeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty mute = channelMuteListProperty.GetArrayElementAtIndex(i);
                SerializedProperty audio2D = channel2DListProperty.GetArrayElementAtIndex(i);
                SerializedProperty fadeZone = channelFadeZoneListProperty.GetArrayElementAtIndex(i);
                SerializedProperty audio2DMute = channel2DMuteListProperty.GetArrayElementAtIndex(i);
                SerializedProperty audio2DVolume = channel2DVolumeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty audio2DSepVolume = channel2DSeparateVolumeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty audio2DFade = channel2DFadeDisableListProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.PropertyField(audioSource, new GUIContent("Audio Source", "The audio source of the output channel"));
                EditorGUILayout.PropertyField(name, new GUIContent("Name", "The name of the output channel"));
                EditorGUILayout.PropertyField(volume, new GUIContent("Volume", "The default volume of the output channel.  Channel volume is multiplied by master volume and input volume to reach the final volume for the audio source."));
                EditorGUILayout.PropertyField(mute, new GUIContent("Mute", "Whether the output channel is muted by default.  If input or master mute is set, the audio source will be muted regardless of this setting."));
                EditorGUILayout.PropertyField(fadeZone, new GUIContent("Fade Zone", "A fade zone that automatically controls the volume of the output channel"));
                EditorGUILayout.PropertyField(audio2D, new GUIContent("2D", "Whether the spatial blend of the channel is forced 2D by default.  Channel can have separate mute and volume characteristics when 2D is set."));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Master 2D Profile", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(audio2DMute, new GUIContent("Mute", "Whether the output channel is muted when master 2D audio is enabled.  In some multi-channel setups, only a single channel may be desired in a 2D spatial blend."));
                EditorGUILayout.PropertyField(audio2DSepVolume, new GUIContent("Override Volume", "Whether to override the channel volume with a separate value when master 2D audio is enabled."));
                if (audio2DSepVolume.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(audio2DVolume, new GUIContent("Volume", "The default volume of the output channel when master 2D audio is enabled."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(audio2DFade, new GUIContent("Disable Fade Zone", "Whether the fade zone should be ignored when the channel is set to 2D.  Note that fade zones are often used with 2D audio sources to limit them like 3D spatial sources, but that is separate from the concept of a master 2D audio toggle."));

                if (GUILayout.Button("Remove", GUILayout.Width(EditorGUIUtility.labelWidth)))
                {
                    RemoveIndex(i);
                    i--;
                }

                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        private void RemoveIndex(int index)
        {
            if (index < 0 || index >= channelAudioListProperty.arraySize)
                return;

            bool[] foldout = new bool[channelAudioListProperty.arraySize - 1];
            Array.Copy(_showChannelFoldout, 0, foldout, 0, index);
            Array.Copy(_showChannelFoldout, index, foldout, index - 1, channelAudioListProperty.arraySize - index - 1);
            _showChannelFoldout = foldout;

            channelAudioListProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            channelFadeZoneListProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;

            channelAudioListProperty.DeleteArrayElementAtIndex(index);
            channelNameListProperty.DeleteArrayElementAtIndex(index);
            channelVolumeListProperty.DeleteArrayElementAtIndex(index);
            channelMuteListProperty.DeleteArrayElementAtIndex(index);
            channel2DListProperty.DeleteArrayElementAtIndex(index);
            channelFadeZoneListProperty.DeleteArrayElementAtIndex(index);
            channel2DMuteListProperty.DeleteArrayElementAtIndex(index);
            channel2DVolumeListProperty.DeleteArrayElementAtIndex(index);
            channel2DSeparateVolumeListProperty.DeleteArrayElementAtIndex(index);
            channel2DFadeDisableListProperty.DeleteArrayElementAtIndex(index);
        }

        private void LinkAudioLink()
        {
            UdonBehaviour[] allBehaviours = FindObjectsOfType<UdonBehaviour>();
            foreach (UdonBehaviour behaviour in allBehaviours)
            {
                if (!behaviour.programSource)
                    continue;

                var program = behaviour.programSource.SerializedProgramAsset.RetrieveProgram();
                if (behaviour.programSource.name != "AudioLink")
                    continue;

                audioLinkProperty.objectReferenceValue = behaviour;
                break;
            }

            string channel = audioLinkChannelProperty.stringValue;
            if (channel == null || channel == "")
            {
                for (int i = 0; i < channelNameListProperty.arraySize; i++)
                {
                    string name = channelNameListProperty.GetArrayElementAtIndex(i).stringValue;
                    if (name == "audioLink" || name == "AudioLink")
                        audioLinkChannelProperty.stringValue = name;
                }
            }
        }
    }
}
