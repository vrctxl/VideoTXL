
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("VideoTXL/UI/Local Controls Slim")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class LocalControlsSlim : UdonSharpBehaviour
    {
        public UdonBehaviour videoPlayer;
        public AudioManager audioManager;
        public ControlColorProfile colorProfile;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public Slider volumeSlider;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        void Start()
        {
            _PopulateMissingReferences();

            if (Utilities.IsValid(colorProfile))
            {
                normalColor = colorProfile.normalColor;
                disabledColor = colorProfile.disabledColor;
                activeColor = colorProfile.activeColor;
                attentionColor = colorProfile.attentionColor;
            }

            if (Utilities.IsValid(audioManager))
                audioManager._RegisterControls(this);
        }

        bool inVolumeControllerUpdate = false;

        public void _AudioManagerUpdate()
        {
            if (!Utilities.IsValid(audioManager))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = audioManager.masterVolume;
                if (volume != volumeSlider.value)
                    volumeSlider.value = volume;
            }

            UpdateToggleVisual();

            inVolumeControllerUpdate = false;
        }

        public void _Resync()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer.SendCustomEvent("_Resync");
        }

        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager))
                audioManager._SetMasterMute(!audioManager.masterMute);
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager) && Utilities.IsValid(volumeSlider))
                audioManager._SetMasterVolume(volumeSlider.value);
        }

        void UpdateToggleVisual()
        {
            if (Utilities.IsValid(audioManager))
            {
                if (Utilities.IsValid(muteToggleOn) && Utilities.IsValid(muteToggleOff))
                {
                    muteToggleOn.SetActive(audioManager.masterMute);
                    muteToggleOff.SetActive(!audioManager.masterMute);
                }
            }
        }

        void _PopulateMissingReferences()
        {
            if (!Utilities.IsValid(volumeSlider))
                volumeSlider = (Slider)_FindComponent("ControlArea/VolumeGroup/Slider", typeof(Slider));
            if (!Utilities.IsValid(muteToggleOn))
                muteToggleOn = _FindGameObject("ControlArea/VolumeGroup/MuteButton/IconMuted");
            if (!Utilities.IsValid(muteToggleOff))
                muteToggleOff = _FindGameObject("ControlArea/VolumeGroup/MuteButton/IconVolume");
        }

        GameObject _FindGameObject(string path)
        {
            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.gameObject;
        }

        Component _FindComponent(string path, System.Type type)
        {
            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.GetComponent(type);
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(LocalControlsSlim))]
    internal class LocalControlsSlimInspector : Editor
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
#endif
}
