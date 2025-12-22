using UnityEngine;

using UnityEditor;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
using System.Collections.Generic;
using System;

#if UNITY_2019
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Texel
{
    [CustomEditor(typeof(SyncPlayer))]
    internal class SyncPlayerInspector : Editor
    {
        SerializedProperty prefabInitializedProperty;

        SerializedProperty sourceManagerProperty;
        SerializedProperty remapperProperty;
        SerializedProperty urlInfoResolverProperty;
        SerializedProperty accessControlProperty;
        SerializedProperty debugLogProperty;
        SerializedProperty debugStageProperty;
        SerializedProperty eventLoggingProperty;
        SerializedProperty traceLoggingProperty;
        SerializedProperty playbackZoneProperty;
        SerializedProperty runBuildHooksProperty;

        SerializedProperty defaultUrlProperty;
        SerializedProperty defaultLockedProperty;
        SerializedProperty debugLoggingProperty;
        SerializedProperty loopProperty;
        SerializedProperty retryOnErrorProperty;
        SerializedProperty autoFailbackAVProProperty;
        SerializedProperty holdLoadedVideosProperty;

        SerializedProperty syncFrequencyProperty;
        SerializedProperty syncThresholdProperty;
        SerializedProperty autoAVSyncProperty;

        SerializedProperty defaultVideoModeProperty;
        SerializedProperty defaultScreenFitProperty;

        static bool expandDebug = false;
        static bool expandAdvanced = false;

        DateTime lastValidate;
        List<VideoManager> cachedVideoManagers;
        List<AudioManager> cachedAudioManagers;

        private void OnEnable()
        {
            prefabInitializedProperty = serializedObject.FindProperty(nameof(SyncPlayer.prefabInitialized));

            sourceManagerProperty = serializedObject.FindProperty(nameof(SyncPlayer.sourceManager));
            remapperProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlRemapper));
            urlInfoResolverProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlInfoResolver));
            accessControlProperty = serializedObject.FindProperty(nameof(SyncPlayer.accessControl));
            debugLogProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLog));
            debugStageProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugState));
            eventLoggingProperty = serializedObject.FindProperty(nameof(SyncPlayer.eventLogging));
            traceLoggingProperty = serializedObject.FindProperty(nameof(SyncPlayer.traceLogging));
            playbackZoneProperty = serializedObject.FindProperty(nameof(SyncPlayer.trackedZoneTrigger));
            runBuildHooksProperty = serializedObject.FindProperty(nameof(SyncPlayer.runBuildHooks));

            defaultUrlProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultUrl));
            defaultLockedProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultLocked));
            debugLoggingProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLogging));
            loopProperty = serializedObject.FindProperty(nameof(SyncPlayer.loop));
            retryOnErrorProperty = serializedObject.FindProperty(nameof(SyncPlayer.retryOnError));
            autoFailbackAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoFailbackToAVPro));
            holdLoadedVideosProperty = serializedObject.FindProperty(nameof(SyncPlayer.holdLoadedVideos));

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

            GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)serializedObject.targetObject;

            TimeSpan time = DateTime.Now.Subtract(lastValidate);
            if (time.TotalMilliseconds > 1000)
            {
                lastValidate = DateTime.Now;
                CheckManagers(videoPlayer);
            }

            CheckIntegrity();

            if (cachedVideoManagers != null && cachedVideoManagers.Count > 0)
            {
                List<VideoSource> unitySources = VideoComponentUpdater.GetVideoSources(cachedVideoManagers[0], VideoSource.VIDEO_SOURCE_UNITY);
                List<VideoSource> avproSources = VideoComponentUpdater.GetVideoSources(cachedVideoManagers[0], VideoSource.VIDEO_SOURCE_AVPRO);

                if (unitySources.Count == 0 && avproSources.Count == 0)
                {
                    EditorGUILayout.HelpBox("No video sources are defined.  Video playback will not work until at least one video source is added.  Check documentation for information on adding new video sources, or use another version of the video player prefab that includes sources.", MessageType.Warning);
                    if (GUILayout.Button("Video Manager Documentation"))
                        Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Video-Manager");
                }
            }

            if (cachedAudioManagers != null && cachedAudioManagers.Count > 0)
            {
                List<AudioChannelGroup> groups = VideoComponentUpdater.GetValidAudioGroups(cachedAudioManagers[0]);
                if (groups.Count == 0)
                {
                    EditorGUILayout.HelpBox("No audio channel groups are defined.  There will be no audio during video playback.  Check documentation for information on adding new audio groups, or use another version of the video player prefab that includes audio groups.", MessageType.Warning);
                    if (GUILayout.Button("Audio Manager Documentation"))
                        Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Audio-Manager");
                }
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("URLs & URL Sources", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(defaultUrlProperty, new GUIContent("Default URL", "Optional default URL to play on world load.  If a separate URL Source is also provided, the default URL will play first."));
            if (TXLGUI.DrawObjectFieldWithAdd(sourceManagerProperty, new GUIContent("URL Source Manager", "Manager for queues, playlists, and other URL sources."), new GUIContent("+", "Create new Source Manager")))
                VideoTxlManager.AddSourceManagerToScene(true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);

            if (TXLGUI.DrawObjectFieldWithAdd(remapperProperty, new GUIContent("URL Remapper", "Set of input URLs to remap to alternate URLs on a per-platform basis."), new GUIContent("+", "Create new URL Remapper")))
                VideoTxlManager.AddUrlRemapperToScene(true);
            if (TXLGUI.DrawObjectFieldWithAdd(urlInfoResolverProperty, new GUIContent("URL Info Resolver", "A resolver and cache for finding additional info about a URL, like title or author."), new GUIContent("+", "Create new URL Info Resolver")))
                VideoTxlManager.AddUrlInfoResolverToScene(true);
            if (TXLGUI.DrawObjectFieldWithAdd(accessControlProperty, new GUIContent("Access Control", "Control access to player controls based on player type or whitelist."), new GUIContent("+", "Create new Access Control")))
                VideoTxlManager.AddAccessControlToScene(true);
            if (TXLGUI.DrawObjectFieldWithAdd(playbackZoneProperty, new GUIContent("Playback Zone", "Optional tracked trigger zone the player must be in to sustain playback.  Disables playing audio on world load."), new GUIContent("+", "Create new Tracked Trigger Zone")))
                VideoTxlManager.AddSyncPlaybackZoneToScene(true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultLockedProperty, new GUIContent("Default Locked", "Whether player controls are locked to master and instance owner by default."));
            EditorGUILayout.PropertyField(loopProperty, new GUIContent("Loop", "Automatically loop track when finished."));
            EditorGUILayout.PropertyField(retryOnErrorProperty, new GUIContent("Retry on Error", "Whether to keep playing the same URL if an error occurs."));
            EditorGUILayout.PropertyField(autoFailbackAVProProperty, new GUIContent("Auto Failover to AVPro", "If AVPro component is available and enabled, automatically fail back to AVPro when auto mode failed under certain conditions to play in video mode."));
            EditorGUILayout.PropertyField(holdLoadedVideosProperty, new GUIContent("Hold Loaded Videos", "Preload videos, but do not start playing them until prompted by an external signal."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            GUIContent desc = new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis.");
            defaultVideoModeProperty.intValue = EditorGUILayout.Popup(desc, defaultVideoModeProperty.intValue, new string[] { "Auto", "AVPro", "Unity Video" });
            EditorGUILayout.PropertyField(defaultScreenFitProperty, new GUIContent("Default Screen Fit", "How content not matching a screen's aspect ratio should be fit by default.  Affects the output CRT and materials with the screen fit property mapped."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            if (GUILayout.Button("Update Connected Components"))
                VideoComponentUpdater.UpdateComponents((TXLVideoPlayer)serializedObject.targetObject);

            EditorGUILayout.Space();
            expandAdvanced = EditorGUILayout.Foldout(expandAdvanced, "Advanced Options", true, boldFoldoutStyle);
            if (expandAdvanced)
            {
                EditorGUILayout.PropertyField(runBuildHooksProperty, new GUIContent("Run Build Hooks", "Checks video player object hierarchy and fixes any component that's internally out of sync at build time."));

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(syncFrequencyProperty, new GUIContent("Sync Frequency", "How often to check if video playback has fallen out of sync."));
                EditorGUILayout.PropertyField(syncThresholdProperty, new GUIContent("Sync Threshold", "How far video playback must have fallen out of sync to perform a correction."));
                EditorGUILayout.PropertyField(autoAVSyncProperty, new GUIContent("Auto Internal AV Sync", "Experimental.  Video playback will periodically resync audio and video.  May cause stuttering or temporary playback failure."));
            }

            EditorGUILayout.Space();
            expandDebug = EditorGUILayout.Foldout(expandDebug, "Debug Options", true, boldFoldoutStyle);
            if (expandDebug)
            {
                EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(debugStageProperty, new GUIContent("Debug State", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(eventLoggingProperty, new GUIContent("Include Events", "Include additional event traffic in debug log"));
                EditorGUILayout.PropertyField(traceLoggingProperty, new GUIContent("Include Trace", "Include significantly more function call statements in debug log"));
                EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void CheckManagers(TXLVideoPlayer videoPlayer)
        {
            cachedVideoManagers = VideoComponentUpdater.GetVideoManagers(videoPlayer);
            cachedAudioManagers = VideoComponentUpdater.GetAudioManagers(videoPlayer);
        }

        bool CheckIntegrity()
        {
            if (cachedVideoManagers != null)
            {
                if (cachedVideoManagers.Count == 0)
                    EditorGUILayout.HelpBox("No video managers found that reference this video player.  The video manager is usually a child object of the video player.", MessageType.Error);
                else if (cachedVideoManagers.Count > 1)
                    EditorGUILayout.HelpBox("More than one video manager found that references this video player.  Only one manager will get used at runtime.", MessageType.Warning);
            }

            if (cachedAudioManagers != null)
            {
                if (cachedAudioManagers.Count == 0)
                    EditorGUILayout.HelpBox("No audio managers found that reference this video player.  The audio manager is usually a child object of the video player.", MessageType.Error);
                else if (cachedAudioManagers.Count > 1)
                    EditorGUILayout.HelpBox("More than one audio manager found that references this video player.  Only one manager will get used at runtime.", MessageType.Warning);
            }

            return true;
        }
    }
}
