using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Texel
{
    [CustomEditor(typeof(LocalControlsSlim))]
    public class LocalControlsSlimInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;
        //SerializedProperty audioManagerProperty;
        SerializedProperty colorProfileProperty;

        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        string[] buttonBgImagePaths = new string[]
        {
            "ControlArea/VolumeGroup/MuteButton",
            "ControlArea/VolumeGroup/ResyncButton",
            "ControlArea/VolumeGroup/ResyncButton",
            "ControlArea/VolumeGroup/OptionsButton",
        };

        string[] buttonIconImagePaths = new string[]
        {
            "ControlArea/VolumeGroup/MuteButton/IconMuted",
            "ControlArea/VolumeGroup/MuteButton/IconVolume",
            "ControlArea/VolumeGroup/ResyncButton/IconResync",
            "ControlArea/VolumeGroup/OptionsButton/IconOptions",
        };

        string[] sliderBgImagePaths = new string[] {
            "ControlArea/VolumeGroup/Background",
        };

        string[] volumeFillBgPaths = new string[] {
            "ControlArea/VolumeGroup/Slider/Fill Area/Fill",
        };
        string[] volumeHandleBgPaths = new string[] {
            "ControlArea/VolumeGroup/Slider/Handle Slide Area/Handle",
        };

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.videoPlayer));
            //audioManagerProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.audioManager));
            colorProfileProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.colorProfile));

            muteToggleOnProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.muteToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(LocalControlsSlim.volumeSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            //EditorGUILayout.PropertyField(audioManagerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
            if (GUILayout.Button("Apply Color Profile"))
                UpdateColors();

            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        void UpdateColors()
        {
            LocalControlsSlim pc = (LocalControlsSlim)serializedObject.targetObject;
            if (pc == null)
            {
                Debug.LogWarning("Could not find gameobject");
                return;
            }

            if (pc.colorProfile == null)
            {
                Debug.LogWarning("No control color profile set");
                return;
            }

            GameObject root = pc.gameObject;

            List<Object> pendingUpdate = new List<Object>();
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, buttonBgImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, sliderBgImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, volumeFillBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, volumeHandleBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, buttonIconImagePaths);

            Undo.RecordObjects(pendingUpdate.ToArray(), "Update colors");

            ControlColorProfile colorProfile = pc.colorProfile;

            ControlUtils.UpdateImages(root, buttonBgImagePaths, colorProfile.buttonBackgroundColor);
            ControlUtils.UpdateImages(root, sliderBgImagePaths, colorProfile.sliderBackgroundColor);
            ControlUtils.UpdateImages(root, volumeFillBgPaths, colorProfile.volumeFillColor);
            ControlUtils.UpdateImages(root, volumeHandleBgPaths, colorProfile.volumeHandleColor);
            ControlUtils.UpdateImages(root, buttonIconImagePaths, colorProfile.normalColor);

            foreach (Object obj in pendingUpdate)
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
    }
}
