using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
using System;

namespace Texel
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerInspector : Editor
    {
        static bool _showChannelListFoldout = true;
        static bool[] _showChannelFoldout = new bool[0];

        SerializedProperty dataProxyProperty;
        SerializedProperty muteSourcePropertyProperty;

        SerializedProperty enableSyncProperty;
        SerializedProperty syncAudioManagerProperty;

        SerializedProperty inputVolumeProperty;
        SerializedProperty inputMuteProperty;
        SerializedProperty masterVolumeProperty;
        SerializedProperty masterMuteProperty;

        SerializedProperty channelAudioListProperty;
        SerializedProperty channelNameListProperty;
        SerializedProperty channelVolumeListProperty;
        SerializedProperty channelMuteListProperty;
        SerializedProperty channelFadeZoneListProperty;

        private void OnEnable()
        {
            dataProxyProperty = serializedObject.FindProperty(nameof(AudioManager.dataProxy));
            muteSourcePropertyProperty = serializedObject.FindProperty(nameof(AudioManager.muteSourceForInactiveVideo));

            enableSyncProperty = serializedObject.FindProperty(nameof(AudioManager.useSync));
            syncAudioManagerProperty = serializedObject.FindProperty(nameof(AudioManager.syncAudioManager));

            inputVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.inputVolume));
            inputMuteProperty = serializedObject.FindProperty(nameof(AudioManager.inputMute));
            masterVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.masterVolume));
            masterMuteProperty = serializedObject.FindProperty(nameof(AudioManager.masterMute));

            channelAudioListProperty = serializedObject.FindProperty(nameof(AudioManager.channelAudio));
            channelNameListProperty = serializedObject.FindProperty(nameof(AudioManager.channelNames));
            channelVolumeListProperty = serializedObject.FindProperty(nameof(AudioManager.channelVolume));
            channelMuteListProperty = serializedObject.FindProperty(nameof(AudioManager.channelMute));
            channelFadeZoneListProperty = serializedObject.FindProperty(nameof(AudioManager.channelFadeZone));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(dataProxyProperty, new GUIContent("Data Proxy", "The data proxy of the video player acting as input to the audio manager"));

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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Channels", EditorStyles.boldLabel);
            ChannelFoldout();

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
                channelAudioListProperty, channelNameListProperty, channelVolumeListProperty, channelMuteListProperty, channelFadeZoneListProperty);

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
                SerializedProperty fadeZone = channelFadeZoneListProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.PropertyField(audioSource, new GUIContent("Audio Source", "The audio source of the output channel"));
                EditorGUILayout.PropertyField(name, new GUIContent("Name", "The name of the output channel"));
                EditorGUILayout.PropertyField(volume, new GUIContent("Volume", "The default volume of the output channel.  Channel volume is multiplied by master volume and input volume to reach the final volume for the audio source."));
                EditorGUILayout.PropertyField(mute, new GUIContent("Mute", "Whether the output channel is muted by default.  If input or master mute is set, the audio source will be muted regardless of this setting."));
                EditorGUILayout.PropertyField(fadeZone, new GUIContent("Fade Zone", "A fade zone that automatically controls the volume of the output channel"));

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
            channelFadeZoneListProperty.DeleteArrayElementAtIndex(index);
        }
    }
}
