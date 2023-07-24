using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;

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
    }
}
