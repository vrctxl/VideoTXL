using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using UnityEngine.UI;

namespace Texel
{
    [CustomEditor(typeof(LocalControlsSlim))]
    public class LocalControlsSlimInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;
        SerializedProperty audioManagerProperty;
        SerializedProperty colorProfileProperty;

        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        string[] buttonBgImagePaths = new string[]
        {
            "ControlArea/VolumeGroup/MuteButton",
            "ControlArea/VolumeGroup/ResyncButton",
        };

        string[] buttonIconImagePaths = new string[]
        {
            "ControlArea/VolumeGroup/MuteButton/IconMuted",
            "ControlArea/VolumeGroup/MuteButton/IconVolume",
            "ControlArea/VolumeGroup/ResyncButton/IconResync",
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
            videoPlayerProperty = serializedObject.FindProperty(nameof(PlayerControls.videoPlayer));
            audioManagerProperty = serializedObject.FindProperty(nameof(PlayerControls.audioManager));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            muteToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(audioManagerProperty);
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
            UpdateImages(root, buttonBgImagePaths, pc.colorProfile.buttonBackgroundColor);
            UpdateImages(root, sliderBgImagePaths, pc.colorProfile.sliderBackgroundColor);
            UpdateImages(root, volumeFillBgPaths, pc.colorProfile.volumeFillColor);
            UpdateImages(root, volumeHandleBgPaths, pc.colorProfile.volumeHandleColor);
            UpdateImages(root, buttonIconImagePaths, pc.colorProfile.normalColor);
        }

        void UpdateImages(GameObject root, string[] paths, Color color)
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                Image image = t.GetComponent<Image>();
                if (image == null)
                    continue;

                image.color = color;
            }
        }
    }
}
