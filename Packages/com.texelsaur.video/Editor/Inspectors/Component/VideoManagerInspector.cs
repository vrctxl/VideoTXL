
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

        SerializedProperty debugLogProperty;
        SerializedProperty debugLoggingProperty;

        DateTime lastValidate;
        bool audioValid = true;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(VideoManager.videoPlayer));
            sourcesProperty = serializedObject.FindProperty(nameof(VideoManager.sources));
            enableAVProInEditorProperty = serializedObject.FindProperty("enableAVProInEditor");

            debugLogProperty = serializedObject.FindProperty(nameof(VideoManager.debugLog));
            debugLoggingProperty = serializedObject.FindProperty(nameof(VideoManager.debugLogging));

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

            //EditorGUILayout.Space();
            //EditorGUILayout.PropertyField(sourcesProperty, new GUIContent("Sources", "The list of available video sources."));

            //List<VideoSource> unitySources = VideoComponentUpdater.GetVideoSources(videoManager, VideoSource.VIDEO_SOURCE_UNITY);
            //List<VideoSource> avproSources = VideoComponentUpdater.GetVideoSources(videoManager, VideoSource.VIDEO_SOURCE_AVPRO);
            //if (unitySources.Count == 0 && avproSources.Count == 0)
            //    EditorGUILayout.HelpBox("No video sources are defined.  Video playback will not work until at least one video source is added.  Check documentation linked above for information on adding new video sources, or use another version of the video player prefab that includes sources.", MessageType.Warning);

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
            EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
            EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
