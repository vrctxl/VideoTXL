using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using UnityEditor.Experimental.SceneManagement;
using System.Collections.Generic;

namespace Texel
{
    [CustomEditor(typeof(SyncPlayer))]
    internal class SyncPlayerInspector : Editor
    {
        SerializedProperty videoMuxProperty;
        SerializedProperty audioManagerProperty;

        SerializedProperty prefabInitializedProperty;

        SerializedProperty playlistPoperty;
        SerializedProperty remapperProperty;
        SerializedProperty accessControlProperty;
        SerializedProperty debugLogProperty;
        SerializedProperty debugStageProperty;
        SerializedProperty playbackZoneProperty;

        SerializedProperty defaultUrlProperty;
        SerializedProperty defaultLockedProperty;
        SerializedProperty debugLoggingProperty;
        SerializedProperty loopProperty;
        SerializedProperty retryOnErrorProperty;
        SerializedProperty autoFailbackAVProProperty;

        SerializedProperty syncFrequencyProperty;
        SerializedProperty syncThresholdProperty;
        SerializedProperty autoAVSyncProperty;

        SerializedProperty defaultVideoModeProperty;
        SerializedProperty defaultScreenFitProperty;

        private void OnEnable()
        {
            videoMuxProperty = serializedObject.FindProperty(nameof(SyncPlayer.videoMux));
            audioManagerProperty = serializedObject.FindProperty(nameof(SyncPlayer.audioManager));

            prefabInitializedProperty = serializedObject.FindProperty(nameof(SyncPlayer.prefabInitialized));

            playlistPoperty = serializedObject.FindProperty(nameof(SyncPlayer.playlist));
            remapperProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlRemapper));
            accessControlProperty = serializedObject.FindProperty(nameof(SyncPlayer.accessControl));
            debugLogProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLog));
            debugStageProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugState));
            playbackZoneProperty = serializedObject.FindProperty(nameof(SyncPlayer.playbackZoneMembership));

            defaultUrlProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultUrl));
            defaultLockedProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultLocked));
            debugLoggingProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLogging));
            loopProperty = serializedObject.FindProperty(nameof(SyncPlayer.loop));
            retryOnErrorProperty = serializedObject.FindProperty(nameof(SyncPlayer.retryOnError));
            autoFailbackAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoFailbackToAVPro));

            syncFrequencyProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncFrequency));
            syncThresholdProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncThreshold));
            autoAVSyncProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoInternalAVSync));

            defaultVideoModeProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultVideoSource));
            defaultScreenFitProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultScreenFit));

            // Automatically generate resources and update components when prefab is dropped into the scene
            // The hidden prefabInitizlied property is set false on the shipped video player variants
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null && !prefabInitializedProperty.boolValue)
            {
                serializedObject.Update();
                prefabInitializedProperty.boolValue = true;
                serializedObject.ApplyModifiedProperties();

                VideoComponentUpdater.UpdateComponents((TXLVideoPlayer)serializedObject.targetObject);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)serializedObject.targetObject;

            List<VideoSource> unitySources = VideoComponentUpdater.GetVideoSources(videoPlayer.videoMux, VideoSource.VIDEO_SOURCE_UNITY);
            List<VideoSource> avproSources = VideoComponentUpdater.GetVideoSources(videoPlayer.videoMux, VideoSource.VIDEO_SOURCE_AVPRO);
            if (unitySources.Count == 0 && avproSources.Count == 0)
            {
                EditorGUILayout.HelpBox("No video sources are defined.  Video playback will not work until at least one video source is added.  Check documentation for information on adding new video sources, or use another version of the video player prefab that includes sources.", MessageType.Warning);
                if (GUILayout.Button("Video Manager Documentation"))
                    Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Video-Manager");
            }

            List<AudioChannelGroup> groups = VideoComponentUpdater.GetValidAudioGroups(videoPlayer.audioManager);
            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox("No audio channel groups are defined.  There will be no audio during video playback.  Check documentation for information on adding new audio groups, or use another version of the video player prefab that includes audio groups.", MessageType.Warning);
                if (GUILayout.Button("Audio Manager Documentation"))
                    Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Audio-Manager");
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(videoMuxProperty, new GUIContent("Video Source Manager", "Internal object for multiplexing multiple video sources."));
            EditorGUILayout.PropertyField(audioManagerProperty, new GUIContent("Audio Source Manager", "Internal object for managing multiple audio source sets against video sources."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playlistPoperty, new GUIContent("Playlist", "Pre-populated playlist to iterate through.  If default URL is set, the playlist will be disabled by default, otherwise it will auto-play."));
            EditorGUILayout.PropertyField(remapperProperty, new GUIContent("URL Remapper", "Set of input URLs to remap to alternate URLs on a per-platform basis."));
            EditorGUILayout.PropertyField(accessControlProperty, new GUIContent("Access Control", "Control access to player controls based on player type or whitelist."));

            EditorGUILayout.PropertyField(playbackZoneProperty, new GUIContent("Playback Zone Membership", "Optional zone membership object tied to a trigger zone the player must be in to sustain playback.  Disables playing audio on world load."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultUrlProperty, new GUIContent("Default URL", "Optional default URL to play on world load."));
            EditorGUILayout.PropertyField(defaultLockedProperty, new GUIContent("Default Locked", "Whether player controls are locked to master and instance owner by default."));
            EditorGUILayout.PropertyField(loopProperty, new GUIContent("Loop", "Automatically loop track when finished."));
            EditorGUILayout.PropertyField(retryOnErrorProperty, new GUIContent("Retry on Error", "Whether to keep playing the same URL if an error occurs."));
            EditorGUILayout.PropertyField(autoFailbackAVProProperty, new GUIContent("Auto Failover to AVPro", "If AVPro component is available and enabled, automatically fail back to AVPro when auto mode failed under certain conditions to play in video mode."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(syncFrequencyProperty, new GUIContent("Sync Frequency", "How often to check if video playback has fallen out of sync."));
            EditorGUILayout.PropertyField(syncThresholdProperty, new GUIContent("Sync Threshold", "How far video playback must have fallen out of sync to perform a correction."));
            EditorGUILayout.PropertyField(autoAVSyncProperty, new GUIContent("Auto Internal AV Sync", "Experimental.  Video playback will periodically resync audio and video.  May cause stuttering or temporary playback failure."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            GUIContent desc = new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis.");
            defaultVideoModeProperty.intValue = EditorGUILayout.Popup(desc, defaultVideoModeProperty.intValue, new string[] { "Auto", "AVPro", "Unity Video" });
            EditorGUILayout.PropertyField(defaultScreenFitProperty, new GUIContent("Default Screen Fit", "How content not matching a screen's aspect ratio should be fit by default."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
            EditorGUILayout.PropertyField(debugStageProperty, new GUIContent("Debug State", "Periodically refresh internal object state on a world object"));
            EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            if (GUILayout.Button("Update Connected Components", GUILayout.Width(EditorGUIUtility.labelWidth)))
                VideoComponentUpdater.UpdateComponents((TXLVideoPlayer)serializedObject.targetObject);
        }
    }
}
