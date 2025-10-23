
using UnityEngine;
using VRC.Udon;

using UnityEditor;
using UdonSharpEditor;
using System;
using System.Collections.Generic;

namespace Texel
{
    [CustomEditor(typeof(VideoManager))]
    public class VideoManagerInspector : Editor
    {
        SerializedProperty videoPlayerProperty;
        SerializedProperty sourcesProperty;
        SerializedProperty enableAVProInEditorProperty;
        SerializedProperty defaultReactStreamStopProperty;
        SerializedProperty defaultstreamStopThresholdProperty;
        SerializedProperty youtubeAutoUnityInEditorProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty debugStateProperty;
        SerializedProperty debugLoggingProperty;
        SerializedProperty traceLoggingProperty;
        SerializedProperty eventLoggingProperty;

        DateTime lastValidate;
        bool audioValid = true;

        static bool expandDebug = false;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(VideoManager.videoPlayer));
            sourcesProperty = serializedObject.FindProperty(nameof(VideoManager.sources));
            enableAVProInEditorProperty = serializedObject.FindProperty(nameof(VideoManager.enableAVProInEditor));
            defaultReactStreamStopProperty = serializedObject.FindProperty(nameof(VideoManager.defaultReactStreamStop));
            defaultstreamStopThresholdProperty = serializedObject.FindProperty(nameof(VideoManager.defaultStreamStopThreshold));
            youtubeAutoUnityInEditorProperty = serializedObject.FindProperty(nameof(VideoManager.youtubeAutoUnityInEditor));

            debugLogProperty = serializedObject.FindProperty(nameof(VideoManager.debugLog));
            debugStateProperty = serializedObject.FindProperty(nameof(VideoManager.debugState));
            debugLoggingProperty = serializedObject.FindProperty(nameof(VideoManager.debugLogging));
            traceLoggingProperty = serializedObject.FindProperty(nameof(VideoManager.traceLogging));
            eventLoggingProperty = serializedObject.FindProperty(nameof(VideoManager.eventLogging));

            Revalidate();
        }

        void Revalidate()
        {
            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)videoPlayerProperty.objectReferenceValue;
            if (videoPlayer)
                audioValid = VideoComponentUpdater.ValidateAudioSources(videoPlayer);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            VideoManager videoManager = (VideoManager)serializedObject.targetObject;
            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)videoPlayerProperty.objectReferenceValue;

            TimeSpan time = DateTime.Now.Subtract(lastValidate);
            if (time.TotalMilliseconds > 1000)
            {
                lastValidate = DateTime.Now;
                Revalidate();
            }

            if (GUILayout.Button("Video Manager Documentation"))
                Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Video-Manager");

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent("Video Player", "The video player that this manager serves."));
            EditorGUILayout.PropertyField(enableAVProInEditorProperty, new GUIContent("Enable AVPro In Editor", "Enables loading videos with an AVPro video source active.  This is NOT supported in a default SDK environment.  If you know what you're doing and you've taken the necessary steps to run AVPro in your editor, enable this option."));
            if (enableAVProInEditorProperty.boolValue)
                EditorGUILayout.HelpBox("The above setting does NOT add AVPro playback support to your editor environment.  It will just allow the video player to attempt loading URLs on AVPro video sources.  You must take additional steps on your own to support AVPro playback in the editor.", MessageType.Warning);
            EditorGUILayout.PropertyField(youtubeAutoUnityInEditorProperty, new GUIContent("YouTube Prefer Unity In Editor", "Changes the 'auto' behavior when loading YouTube videos to prefer using a Unity-based video source when available in the editor, instead of AVPro.  YouTube videos are only supported at 360p resolution on Unity Video, but they play natively in the editor."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultReactStreamStopProperty, new GUIContent("Handle Stream End Event", "Enables reacting to an end event from a livestream, causing the video player to stop.\n\nEnd events should indicate the stream has ended, but sometimes they are sent erroneously mid-stream or may be sent when the stream is being restarted."));
            if (defaultReactStreamStopProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(defaultstreamStopThresholdProperty, new GUIContent("Ignore Threshold", "The number of seconds from connecting to a livestream during which any stop events should be ignored.  AVPro may raise a stop event shortly after connecting to a livestream, even though the stream is still running."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            List<VideoSource> sources = VideoComponentUpdater.GetVideoSources((VideoManager)serializedObject.targetObject, -1);
            if (sources.Count == 0)
                EditorGUILayout.HelpBox("No video sources are defined.  Video playback will not work until at least one video source is added.  Check documentation linked above for information on adding new video sources, or use another version of the video player prefab that includes sources.", MessageType.Warning);
            else
            {
                EditorGUILayout.LabelField(new GUIContent("Detected Video Sources", "Add or remove video source prefabs under the Video Manager node in the hierarchy.  Video sources can be temporarily removed by disabling them in the hierarchy.  After making any changes, run Update Audio Components."));
                EditorGUI.indentLevel++;

                foreach (VideoSource source in sources)
                    EditorGUILayout.LabelField("• " + source.name);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            if (!audioValid)
                EditorGUILayout.HelpBox("Video player audio is out of sync with the audio groups defined in the Audio Manager.  Use the button below to resync them.", MessageType.Warning, true);

            if (GUILayout.Button("Update Audio Components"))
            {
                VideoComponentUpdater.UpdateAudioComponents(videoPlayer);
                VideoComponentUpdater.UpdateAudioUI(videoPlayer);
                audioValid = VideoComponentUpdater.ValidateUnityAudioSources(videoPlayer);
            }

            EditorGUILayout.Space();
            expandDebug = EditorGUILayout.Foldout(expandDebug, "Debug Options", true, boldFoldoutStyle);
            if (expandDebug)
            {
                EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(debugStateProperty, new GUIContent("Debug State", "Track periodically refreshed internal state in a world object"));
                EditorGUILayout.PropertyField(eventLoggingProperty, new GUIContent("Include Events", "Include additional event traffic in debug log"));
                EditorGUILayout.PropertyField(traceLoggingProperty, new GUIContent("Include Trace", "Include low-level tracing of video component calls"));
                EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
