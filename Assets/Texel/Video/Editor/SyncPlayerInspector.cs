using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
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

            EditorGUILayout.PropertyField(dataProxyProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playlistPoperty);
            EditorGUILayout.PropertyField(remapperProperty);
            EditorGUILayout.PropertyField(accessControlProperty);
            
            EditorGUILayout.PropertyField(playbackZoneProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultUrlProperty);
            EditorGUILayout.PropertyField(defaultLockedProperty);
            EditorGUILayout.PropertyField(loopProperty);
            EditorGUILayout.PropertyField(retryOnErrorProperty);
            EditorGUILayout.PropertyField(autoFailbackAVProProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(syncFrequencyProperty);
            EditorGUILayout.PropertyField(syncThresholdProperty);
            EditorGUILayout.PropertyField(autoAVSyncProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useAVProProperty);
            if (useAVProProperty.boolValue)
                EditorGUILayout.PropertyField(avProVideoProperty);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useUnityVideoProperty);
            if (useUnityVideoProperty.boolValue)
                EditorGUILayout.PropertyField(unityVideoProperty);

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
