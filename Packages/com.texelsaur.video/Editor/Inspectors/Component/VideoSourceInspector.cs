using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace Texel
{
    [CustomEditor(typeof(VideoSource))]
    public class VideoSourceInspector : Editor
    {
        SerializedProperty captureRendererProperty;
        SerializedProperty audioGroupsListProperty;
        SerializedProperty avproReservedChannelProperty;

        SerializedProperty maxResolutionProperty;
        SerializedProperty lowLatencyProperty;

        DateTime lastValidate;

        private void OnEnable()
        {
            captureRendererProperty = serializedObject.FindProperty(nameof(VideoSource.captureRenderer));
            audioGroupsListProperty = serializedObject.FindProperty(nameof(VideoSource.audioGroups));
            avproReservedChannelProperty = serializedObject.FindProperty(nameof(VideoSource.avproReservedChannel));

            maxResolutionProperty = serializedObject.FindProperty(nameof(VideoSource.maxResolution));
            lowLatencyProperty = serializedObject.FindProperty(nameof(VideoSource.lowLatency));

            Revalidate();
        }

        void Revalidate()
        {
            VideoSource source = (VideoSource)target;
            VRCAVProVideoPlayer avp = GetAVPro(source);
            if (avp)
            {
                maxResolutionProperty.intValue = avp.MaximumResolution;
                lowLatencyProperty.boolValue = avp.UseLowLatency;
            }

            VRCUnityVideoPlayer unity = GetUnity(source);
            if (unity)
            {
                try
                {
                    int maxRes = (int)typeof(VRCUnityVideoPlayer).GetField("maximumResolution", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(unity);
                    maxResolutionProperty.intValue = maxRes;
                }
                catch { }
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            TimeSpan time = DateTime.Now.Subtract(lastValidate);
            if (time.TotalMilliseconds > 1000)
            {
                lastValidate = DateTime.Now;
                Revalidate();
            }

            EditorGUILayout.PropertyField(captureRendererProperty);
            EditorGUILayout.PropertyField(audioGroupsListProperty);

            VideoSource source = (VideoSource)target;
            VRCAVProVideoPlayer avp = GetAVPro(source);

            if (avp)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("AVPro Options", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(avproReservedChannelProperty);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detected Settings", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Maximum Resolution: {source.maxResolution}");
            if (avp)
                EditorGUILayout.LabelField($"Use Low Latency: {source.lowLatency}");

            if (!avp)
                _CheckUnityComponent(source);

            serializedObject.ApplyModifiedProperties();
        }

        private VRCAVProVideoPlayer GetAVPro(VideoSource source)
        {
            return (VRCAVProVideoPlayer)source.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
        }

        private VRCUnityVideoPlayer GetUnity(VideoSource source)
        {
            return (VRCUnityVideoPlayer)source.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
        }

        private void _CheckUnityComponent(VideoSource source)
        {
            VRCUnityVideoPlayer com = GetUnity(source);
            if (!com)
                return;

            VRCUrl url = (VRCUrl)typeof(VRCUnityVideoPlayer).GetField("videoURL", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            bool autoPlay = (bool)typeof(VRCUnityVideoPlayer).GetField("autoPlay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            bool loop = (bool)typeof(VRCUnityVideoPlayer).GetField("loop", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            int renderMode = (int)typeof(VRCUnityVideoPlayer).GetField("renderMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            RenderTexture texture = (RenderTexture)typeof(VRCUnityVideoPlayer).GetField("targetTexture", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            Renderer renderer = (Renderer)typeof(VRCUnityVideoPlayer).GetField("targetMaterialRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            string property = (string)typeof(VRCUnityVideoPlayer).GetField("targetMaterialProperty", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
            VideoAspectRatio aspect = (VideoAspectRatio)typeof(VRCUnityVideoPlayer).GetField("aspectRatio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);

            if (renderMode != 1)
                EditorGUILayout.HelpBox("The 'Render Mode' property must be set to 'Material Override' on the Unity Video Player component.  To capture video data in a render texture, use the video player's 'Screen Manager' object.", MessageType.Error);
            else if (texture != null)
                EditorGUILayout.HelpBox("The 'Target Texture' property is set on the Unity Video Player component, but will not be used.  To capture video data in a render texture, use the video player's 'Screen Manager' object.", MessageType.Error);
            else if (renderer != captureRendererProperty.objectReferenceValue)
                EditorGUILayout.HelpBox("The 'Target Material Renderer' property on the Video Player component does not match the 'Capture Renderer' on the Video Source component.", MessageType.Error);
            else if (property != "_MainTex")
                EditorGUILayout.HelpBox("The 'Target Material Property' property on the Video Player component should be set to '_MainTex'.", MessageType.Error);
            else if (aspect != VideoAspectRatio.NoScaling)
                EditorGUILayout.HelpBox("The 'Aspect Ratio' property on the Video Player component should be set to 'No Scaling'.  Aspect Ratio should be controlled in the video player's 'Screen Manager' object.", MessageType.Error);
            else if (url != null && url.Get() != "")
                EditorGUILayout.HelpBox("The 'Video URL' property on the Video Player component should be empty.  The default URL can be set on the main video player object.", MessageType.Error);
            else if (autoPlay)
                EditorGUILayout.HelpBox("The 'Auto Play' property on the Video Player component should be off.", MessageType.Error);
            else if (loop)
                EditorGUILayout.HelpBox("The 'Loop' property on the Video Player component should be off.  Looping can be set on the main video player object.", MessageType.Error);
        }
    }
}
