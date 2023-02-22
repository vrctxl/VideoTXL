
using UnityEngine;
using VRC.Udon;

using UnityEditor;
using UdonSharpEditor;
using System;
using System.Collections.Generic;

namespace Texel
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerInspector : Editor
    {
        SerializedProperty videoPlayerProperty;
        SerializedProperty muteSourcePropertyProperty;

        //SerializedProperty enableSyncProperty;
        //SerializedProperty syncAudioManagerProperty;

        SerializedProperty inputVolumeProperty;
        SerializedProperty inputMuteProperty;
        SerializedProperty masterVolumeProperty;
        SerializedProperty masterMuteProperty;
        //SerializedProperty master2DProperty;

        SerializedProperty channelGroupsProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty debugStateProperty;
        SerializedProperty debugLoggingProperty;
        SerializedProperty debugEventsProperty;

        //SerializedProperty channelAudioListProperty;
        //SerializedProperty channelNameListProperty;
        //SerializedProperty channelVolumeListProperty;
        //SerializedProperty channelMuteListProperty;
        //SerializedProperty channel2DListProperty;
        //SerializedProperty channelFadeZoneListProperty;

        //SerializedProperty channel2DMuteListProperty;
        //SerializedProperty channel2DVolumeListProperty;
        //SerializedProperty channel2DSeparateVolumeListProperty;
        //SerializedProperty channel2DFadeDisableListProperty;

        SerializedProperty audioLinkProperty;
        //SerializedProperty audioLinkChannelProperty;
        
        bool audioValid = true;
        bool audioLinkOutsideLinked = false;
        UdonBehaviour audioLinkCache;
        
        bool expandDebug = false;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(AudioManager.videoPlayer));
            muteSourcePropertyProperty = serializedObject.FindProperty(nameof(AudioManager.muteSourceForInactiveVideo));

            //enableSyncProperty = serializedObject.FindProperty(nameof(AudioManager.useSync));
            //syncAudioManagerProperty = serializedObject.FindProperty(nameof(AudioManager.syncAudioManager));

            inputVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.inputVolume));
            inputMuteProperty = serializedObject.FindProperty(nameof(AudioManager.inputMute));
            masterVolumeProperty = serializedObject.FindProperty(nameof(AudioManager.masterVolume));
            masterMuteProperty = serializedObject.FindProperty(nameof(AudioManager.masterMute));
            //master2DProperty = serializedObject.FindProperty(nameof(AudioManager.master2D));

            channelGroupsProperty = serializedObject.FindProperty(nameof(AudioManager.channelGroups));

            //channelAudioListProperty = serializedObject.FindProperty(nameof(AudioManager.channelAudio));
            //channelNameListProperty = serializedObject.FindProperty(nameof(AudioManager.channelNames));
            //channelVolumeListProperty = serializedObject.FindProperty(nameof(AudioManager.channelVolume));
            //channelMuteListProperty = serializedObject.FindProperty(nameof(AudioManager.channelMute));
            //channelFadeZoneListProperty = serializedObject.FindProperty(nameof(AudioManager.channelFadeZone));

            audioLinkProperty = serializedObject.FindProperty(nameof(AudioManager.audioLinkSystem));
            //audioLinkChannelProperty = serializedObject.FindProperty(nameof(AudioManager.audioLinkChannel));

            debugLogProperty = serializedObject.FindProperty(nameof(AudioManager.debugLog));
            debugStateProperty = serializedObject.FindProperty(nameof(AudioManager.debugState));
            debugLoggingProperty = serializedObject.FindProperty(nameof(AudioManager.debugLogging));
            debugEventsProperty = serializedObject.FindProperty(nameof(AudioManager.debugEvents));

            Revalidate();
        }

        void Revalidate()
        {
            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)videoPlayerProperty.objectReferenceValue;
            if (videoPlayer)
                audioValid = VideoComponentUpdater.ValidateAudioSources(videoPlayer);

            audioLinkOutsideLinked = IsAudioLinkOnAnotherManager();
            FindAudioLink();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)videoPlayerProperty.objectReferenceValue;

            GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Audio Manager Documentation"))
                Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Audio-Manager");

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent("Video Player", "Optional reference to video player acting as input to the audio manager"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            //EditorGUILayout.PropertyField(enableSyncProperty, new GUIContent("Enable Sync", "Enable syncing of audio manager settings across all players, excluding local overrides like local volume controls or fade zones.  Sync is not needed for most video player setups."));
            //if (enableSyncProperty.boolValue)
            //    EditorGUILayout.PropertyField(syncAudioManagerProperty, new GUIContent("Sync Audio Manager", "Separate sync component attached to the Audio Manager"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inputVolumeProperty, new GUIContent("Input Volume", "The default volume of the input source.  Allows input to be controlled without user override.  Normally left at 1."));
            EditorGUILayout.PropertyField(inputMuteProperty, new GUIContent("Input Mute", "Whether the input source is muted by default"));
            EditorGUILayout.PropertyField(masterVolumeProperty, new GUIContent("Master Volume", "The default master volume. Can be overridden locally by users."));
            EditorGUILayout.PropertyField(masterMuteProperty, new GUIContent("Master Mute", "Whether all audio is muted by default"));
            //EditorGUILayout.PropertyField(master2DProperty, new GUIContent("Master 2D", "Whether the default spatial audio mode is 2D"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Channel Groups", EditorStyles.boldLabel);
            
            if (!audioValid)
                EditorGUILayout.HelpBox("Video player audio is out of sync with the audio groups defined below.  Audio channel groups are a template, and the video player's audio components must be refreshed when the templates are changed.", MessageType.Warning, true);

            if (GUILayout.Button("Update Audio Components"))
            {
                VideoComponentUpdater.UpdateAudioComponents(videoPlayer);
                VideoComponentUpdater.UpdateAudioUI(videoPlayer);
                audioValid = VideoComponentUpdater.ValidateUnityAudioSources(videoPlayer);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(channelGroupsProperty, new GUIContent("Groups", "Channel groups recognized by the video player"));
            if (EditorGUI.EndChangeCheck())
                Revalidate();

            List<AudioChannelGroup> groups = VideoComponentUpdater.GetValidAudioGroups((AudioManager)serializedObject.targetObject);
            if (groups.Count == 0)
                EditorGUILayout.HelpBox("No audio channel groups are defined.  There will be no audio during video playback.  Check documentation linked above for information on adding new audio groups, or use another version of the video player prefab that includes audio groups.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioLink", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(audioLinkProperty, new GUIContent("AudioLink", "Set the main AudioLink script to have the AudioManager update it as audio sources are changed out."));
            if (audioLinkCache)
            {
                if (audioLinkOutsideLinked && audioLinkProperty.objectReferenceValue)
                    EditorGUILayout.HelpBox("AudioLink detected in scene.  AudioLink is already linked to another TXL AudioManager.  This can cause a conflict.", MessageType.Warning, true);
                else if (audioLinkOutsideLinked && !audioLinkProperty.objectReferenceValue)
                    EditorGUILayout.HelpBox("AudioLink detected in scene.  AudioLink is already linked to another TXL AudioManager.", MessageType.Info, true);
                else
                    EditorGUILayout.HelpBox("AudioLink detected in scene.  Link AudioLink to have this manager keep AudioLink's audio source updated when video sources change.", MessageType.Info, true);
            }

            if (GUILayout.Button(new GUIContent("Link AudioLink to this manager", "Finds first AudioLink UdonBehaviour and sets its reference automatically.")))
                LinkAudioLink();

            EditorGUILayout.Space();
            expandDebug = EditorGUILayout.Foldout(expandDebug, "Debug Options", true, boldFoldoutStyle);
            if (expandDebug)
            {
                //EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(debugStateProperty, new GUIContent("Debug State", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(debugEventsProperty, new GUIContent("Include Events", "Include additional event traffic in debug log"));
                EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private UdonBehaviour FindAudioLink()
        {
            if (audioLinkCache)
            {
                UdonBehaviour[] components = audioLinkCache.transform.GetComponents<UdonBehaviour>();
                audioLinkCache = FindAudioLink(components);
                if (audioLinkCache)
                    return audioLinkCache;
            }

            UdonBehaviour[] allBehaviours = FindObjectsOfType<UdonBehaviour>();
            audioLinkCache = FindAudioLink(allBehaviours);
            return audioLinkCache;
        }

        private UdonBehaviour FindAudioLink(UdonBehaviour[] behaviors)
        {
            foreach (UdonBehaviour behaviour in behaviors)
            {
                if (!behaviour.programSource)
                    continue;

                if (behaviour.programSource.name != "AudioLink")
                    continue;

                return behaviour;
            }

            return null;
        }

        private void LinkAudioLink()
        {
            UdonBehaviour audioLink = FindAudioLink();
            if (audioLink)
                audioLinkProperty.objectReferenceValue = audioLink;
            else
                audioLinkProperty.objectReferenceValue = null;
        }

        private bool IsAudioLinkOnAnotherManager()
        {
            AudioManager[] managers = FindObjectsOfType<AudioManager>();
            foreach (AudioManager manager in managers)
            {
                if (manager == serializedObject.targetObject)
                    continue;

                if (manager.audioLinkSystem)
                    return true;
            }

            return false;
        }
    }
}
