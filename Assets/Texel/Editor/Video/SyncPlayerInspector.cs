using UnityEngine;

using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(SyncPlayer))]
    internal class SyncPlayerInspector : Editor
    {
        SerializedProperty dataProxyProperty;

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
        SerializedProperty useUnityVideoProperty;
        SerializedProperty useAVProProperty;
        SerializedProperty unityVideoProperty;
        SerializedProperty avProVideoProperty;

        private void OnEnable()
        {
            dataProxyProperty = serializedObject.FindProperty(nameof(SyncPlayer.dataProxy));

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
            useUnityVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.useUnityVideo));
            useAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.useAVPro));
            unityVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.unityVideo));
            avProVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.avProVideo));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(dataProxyProperty, new GUIContent("Data Proxy", "A proxy for dispatching video-related events to other listening behaviors, such as a screen manager."));

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

            EditorGUILayout.PropertyField(useAVProProperty, new GUIContent("Use AVPro", "Whether AVPro is a supported video source for this player.  Disabling this component also disables source selection."));
            if (useAVProProperty.boolValue)
                EditorGUILayout.PropertyField(avProVideoProperty, new GUIContent("AVPro Video Source", "AVPro video player component"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useUnityVideoProperty, new GUIContent("Use Unity Video", "Whether Unity video is a supported video source for this player.  Disabling this component also disables source selection."));
            if (useUnityVideoProperty.boolValue)
                EditorGUILayout.PropertyField(unityVideoProperty, new GUIContent("Unity Video Source", "Unity video player component"));

            if (useAVProProperty.boolValue && useUnityVideoProperty.boolValue)
            {
                EditorGUILayout.Space();
                GUIContent desc = new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis.");
                defaultVideoModeProperty.intValue = EditorGUILayout.Popup(desc, defaultVideoModeProperty.intValue, new string[] { "Auto", "AVPro", "Unity Video" });
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
            EditorGUILayout.PropertyField(debugStageProperty, new GUIContent("Debug State", "Periodically refresh internal object state on a world object"));
            EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
