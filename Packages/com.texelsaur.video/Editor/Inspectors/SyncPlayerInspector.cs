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
        SerializedProperty playbackZoneProperty;
        SerializedProperty exclusionZonesProperty;
        SerializedProperty defaultLocalPlaybackEnabledProperty;
        SerializedProperty runBuildHooksProperty;
        SerializedProperty debugStateProperty;

        SerializedProperty defaultUrlProperty;
        SerializedProperty defaultUrlInterruptibleProperty;
        SerializedProperty defaultLockedProperty;
        SerializedProperty loopProperty;
        SerializedProperty retryOnErrorProperty;
        SerializedProperty autoFailbackAVProProperty;
        SerializedProperty holdLoadedVideosProperty;

        SerializedProperty syncFrequencyProperty;
        SerializedProperty syncThresholdProperty;
        SerializedProperty autoAVSyncProperty;

        SerializedProperty defaultVideoModeProperty;
        SerializedProperty defaultScreenFitProperty;

        AccessInspectorBlock accessBlock;
        DebugInspectorBlock debugBlock;

        //static bool expandDebug = false;
        static bool expandAdvanced = false;

        static readonly GUIContent labelDefaultUrl = new GUIContent("Default URL", "Optional default URL to play on world load.  If a separate URL Source is also provided, the default URL will play first.");
        static readonly GUIContent labelInterruptible = new GUIContent("Interruptible", "Whether the default URL playback can be interrupted by other interrupting sources, like queues.");
        static readonly GUIContent labelSourceManager = new GUIContent("URL Source Manager", "Manager for queues, playlists, and other URL sources.");
        static readonly GUIContent labelSourceManagerAdd = new GUIContent("+", "Create new Source Manager");
        static readonly GUIContent labelRemapper = new GUIContent("URL Remapper", "Set of input URLs to remap to alternate URLs on a per-platform basis.");
        static readonly GUIContent labelRemapperAdd = new GUIContent("+", "Create new URL Remapper");
        static readonly GUIContent labelResolver = new GUIContent("URL Info Resolver", "A resolver and cache for finding additional info about a URL, like title or author.");
        static readonly GUIContent labelResolverAdd = new GUIContent("+", "Create new URL Info Resolver");
        static readonly GUIContent labelPlaybackEnabled = new GUIContent("Playback Enabled", "Whether or not playback is enabled for each player locally on world load.\n\nLocal playback is controllable by API and independent of playback zones.");
        static readonly GUIContent labelPlaybackZone = new GUIContent("Playback Zone", "Optional tracked trigger zone the player must be in to sustain playback.  Disables playing video on world load if player does not start in zone.");
        static readonly GUIContent labelPlaybackZoneAdd = new GUIContent("+", "Create new Tracked Trigger Zone");
        static readonly GUIContent labelExclusionZones = new GUIContent("Exclusion Zones", "Optional one or more tracked tricker zones that will locally halt playback when the player enters them.");
        static readonly GUIContent labelDefaultLocked = new GUIContent("Default Locked", "Whether player controls are locked to master and instance owner by default.");
        static readonly GUIContent labelLoop = new GUIContent("Loop", "Automatically loop track when finished.");
        static readonly GUIContent labelRetry = new GUIContent("Retry on Error", "Whether to keep playing the same URL if an error occurs.");
        static readonly GUIContent labelFailover = new GUIContent("Auto Failover to AVPro", "If AVPro component is available and enabled, automatically fail back to AVPro when auto mode failed under certain conditions to play in video mode.");
        static readonly GUIContent labelHoldVideos = new GUIContent("Hold Loaded Videos", "Preload videos, but do not start playing them until prompted by an external signal.");
        static readonly GUIContent labelDefaultSource = new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis.");
        static readonly GUIContent labelDefaultFit = new GUIContent("Default Screen Fit", "How content not matching a screen's aspect ratio should be fit by default.  Affects the output CRT and materials with the screen fit property mapped.");
        static readonly GUIContent labelBuildHooks = new GUIContent("Run Build Hooks", "Checks video player object hierarchy and fixes any component that's internally out of sync at build time.");
        static readonly GUIContent labelSyncFrequency = new GUIContent("Sync Frequency", "How often to check if video playback has fallen out of sync.");
        static readonly GUIContent labelSyncThreshold = new GUIContent("Sync Threshold", "How far video playback must have fallen out of sync to perform a correction.");
        static readonly GUIContent labelAutoAVSync = new GUIContent("Auto Internal AV Sync", "Experimental.  Video playback will periodically resync audio and video.  May cause stuttering or temporary playback failure.");
        static readonly GUIContent labelAdvanced = new GUIContent("Advanced Options");

        static readonly string[] videoSourceNames = new string[] { "Auto", "AVPro", "Unity Video" };

        DateTime lastValidate;
        List<VideoManager> cachedVideoManagers;
        List<AudioManager> cachedAudioManagers;

        private void OnEnable()
        {
            prefabInitializedProperty = serializedObject.FindProperty(nameof(SyncPlayer.prefabInitialized));

            sourceManagerProperty = serializedObject.FindProperty(nameof(SyncPlayer.sourceManager));
            remapperProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlRemapper));
            urlInfoResolverProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlInfoResolver));
            playbackZoneProperty = serializedObject.FindProperty(nameof(SyncPlayer.trackedZoneTrigger));
            exclusionZonesProperty = serializedObject.FindProperty(nameof(SyncPlayer.exclusionZones));
            defaultLocalPlaybackEnabledProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultLocalPlaybackEnabled));
            runBuildHooksProperty = serializedObject.FindProperty(nameof(SyncPlayer.runBuildHooks));
            debugStateProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugState));

            defaultUrlProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultUrl));
            defaultUrlInterruptibleProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultUrlInterruptible));
            defaultLockedProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultLocked));
            loopProperty = serializedObject.FindProperty(nameof(SyncPlayer.loop));
            retryOnErrorProperty = serializedObject.FindProperty(nameof(SyncPlayer.retryOnError));
            autoFailbackAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoFailbackToAVPro));
            holdLoadedVideosProperty = serializedObject.FindProperty(nameof(SyncPlayer.holdLoadedVideos));

            syncFrequencyProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncFrequency));
            syncThresholdProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncThreshold));
            autoAVSyncProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoInternalAVSync));

            defaultVideoModeProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultVideoSource));
            defaultScreenFitProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultScreenFit));

            accessBlock = new AccessInspectorBlock(serializedObject, AccessBlockOptions.Synced);
            debugBlock = new DebugInspectorBlock(serializedObject);

            accessBlock.ContributeDebugRows(debugBlock);

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

            SyncPlayer videoPlayer = (SyncPlayer)serializedObject.targetObject;

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

            EditorGUILayout.PropertyField(defaultUrlProperty, labelDefaultUrl);
            if (videoPlayer.defaultUrl != null && videoPlayer.defaultUrl.Get() != "")
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(defaultUrlInterruptibleProperty, labelInterruptible);
                EditorGUI.indentLevel--;
            }

            if (TXLGUI.DrawObjectFieldWithAdd(sourceManagerProperty, labelSourceManager, labelSourceManagerAdd))
                VideoTxlManager.AddSourceManagerToScene(true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);

            if (TXLGUI.DrawObjectFieldWithAdd(remapperProperty, labelRemapper, labelRemapperAdd))
                VideoTxlManager.AddUrlRemapperToScene(true);
            if (TXLGUI.DrawObjectFieldWithAdd(urlInfoResolverProperty, labelResolver, labelResolverAdd))
                VideoTxlManager.AddUrlInfoResolverToScene(true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Playback Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultLocalPlaybackEnabledProperty, labelPlaybackEnabled);
            if (TXLGUI.DrawObjectFieldWithAdd(playbackZoneProperty, labelPlaybackZone, labelPlaybackZoneAdd))
                VideoTxlManager.AddSyncPlaybackZoneToScene(true);
            EditorGUILayout.PropertyField(exclusionZonesProperty, labelExclusionZones);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultLockedProperty, labelDefaultLocked);
            EditorGUILayout.PropertyField(loopProperty, labelLoop);
            EditorGUILayout.PropertyField(retryOnErrorProperty, labelRetry);
            EditorGUILayout.PropertyField(autoFailbackAVProProperty, labelFailover);
            EditorGUILayout.PropertyField(holdLoadedVideosProperty, labelHoldVideos);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            defaultVideoModeProperty.intValue = EditorGUILayout.Popup(labelDefaultSource, defaultVideoModeProperty.intValue, videoSourceNames);
            EditorGUILayout.PropertyField(defaultScreenFitProperty, labelDefaultFit);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            if (GUILayout.Button("Update Connected Components"))
                VideoComponentUpdater.UpdateComponents((TXLVideoPlayer)serializedObject.targetObject);

            EditorGUILayout.Space();
            expandAdvanced = EditorGUILayout.Foldout(expandAdvanced, labelAdvanced, true, TXLGUI.Styles.BoldFoldout);
            if (expandAdvanced)
            {
                EditorGUILayout.PropertyField(runBuildHooksProperty, labelBuildHooks);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(syncFrequencyProperty, labelSyncFrequency);
                EditorGUILayout.PropertyField(syncThresholdProperty, labelSyncThreshold);
                EditorGUILayout.PropertyField(autoAVSyncProperty, labelAutoAVSync);
            }

            EditorGUILayout.Space();
            accessBlock.Draw(TXLGUI.Styles.BoldFoldout);

            EditorGUILayout.Space();
            debugBlock.Draw(TXLGUI.Styles.BoldFoldout);

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
