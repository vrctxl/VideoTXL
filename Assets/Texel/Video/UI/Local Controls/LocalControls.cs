
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
    [AddComponentMenu("VideoTXL/UI/Local Controls")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class LocalControls : UdonSharpBehaviour
    {
        public UdonBehaviour videoPlayer;
        public StaticUrlSource staticUrlSource;
        public AudioManager AudioManager;
        //public ControlColorProfile colorProfile;

        public bool autoLayout = true;
        public bool enableResync = true;
        public bool enableQualitySelect = false;
        public bool enableVolume = true;
        public bool enable2DAudioToggle = false;
        public bool enableMessageBar = true;

        public GameObject volumeGroup;
        public GameObject resyncGroup;
        public GameObject toggleGroup;
        public GameObject messageBarGroup;

        public GameObject volumeSliderControl;
        public GameObject audio2DControl;

        public GameObject toggle720On;
        public GameObject toggle720Off;
        public GameObject toggle1080On;
        public GameObject toggle1080Off;
        public GameObject toggleAudioOn;
        public GameObject toggleAudioOff;
        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public Slider volumeSlider;

        void Start()
        {
            SendCustomEventDelayedFrames("_UpdateLayout", 1);

            if (Utilities.IsValid(AudioManager))
                AudioManager._RegisterControls(this);
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._RegisterControls(this);
        }

        public void _UpdateLayout()
        {
            if (!autoLayout)
                return;

            bool volumePresent = Utilities.IsValid(volumeGroup) && Utilities.IsValid(AudioManager);

            if (enableVolume && volumePresent)
            {
                if (Utilities.IsValid(audio2DControl))
                    audio2DControl.SetActive(enable2DAudioToggle);

                RectTransform volumeRT = volumeSlider.GetComponent<RectTransform>();
                if (Utilities.IsValid(audio2DControl) && enable2DAudioToggle)
                    volumeRT.offsetMax = new Vector2(-25, volumeRT.offsetMax.y);
                else
                    volumeRT.offsetMax = new Vector2(0, volumeRT.offsetMax.y);
            }

            if (Utilities.IsValid(volumeGroup))
                volumeGroup.SetActive(enableVolume);

            bool qualityDepsMet = Utilities.IsValid(staticUrlSource) && staticUrlSource.multipleResolutions;

            if (Utilities.IsValid(resyncGroup))
                resyncGroup.SetActive(enableResync);
            if (Utilities.IsValid(toggleGroup))
                toggleGroup.SetActive(enableQualitySelect && qualityDepsMet);
            if (Utilities.IsValid(messageBarGroup))
                messageBarGroup.SetActive(enableMessageBar);

            UpdateToggleVisual();
        }

        bool inVolumeControllerUpdate = false;

        //public void _VolumeControllerUpdate()
        public void _AudioManagerUpdate()
        {
            if (!Utilities.IsValid(AudioManager))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = AudioManager.masterVolume;
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

        public void _UrlChanged()
        {
            UpdateToggleVisual();
        }

        public void _SetQuality720()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQuality720();
            UpdateToggleVisual();
        }

        public void _SetQuality1080()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQuality1080();
            UpdateToggleVisual();
        }

        public void _SetQualityAudio()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQualityAudio();
            UpdateToggleVisual();
        }

        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(AudioManager))
                AudioManager._SetMasterMute(!AudioManager.masterMute);
        }

        public void _ToggleAudio2D()
        {
            if (inVolumeControllerUpdate)
                return;

            //if (Utilities.IsValid(AudioManager))
            //    AudioManager._ToggleAudio2D();
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(AudioManager) && Utilities.IsValid(volumeSlider))
                AudioManager._SetMasterVolume(volumeSlider.value);
        }

        void UpdateToggleVisual()
        {
            if (Utilities.IsValid(staticUrlSource))
            {
                bool is720 = staticUrlSource._IsQuality720();
                bool is1080 = staticUrlSource._IsQuality1080();
                bool isAudio = staticUrlSource._IsQualityAudio();
                if (Utilities.IsValid(toggle720On))
                    toggle720On.SetActive(is720);
                if (Utilities.IsValid(toggle720Off))
                    toggle720Off.SetActive(!is720);
                if (Utilities.IsValid(toggle1080On))
                    toggle1080On.SetActive(is1080);
                if (Utilities.IsValid(toggle1080Off))
                    toggle1080Off.SetActive(!is1080);
                if (Utilities.IsValid(toggleAudioOn))
                    toggleAudioOn.SetActive(isAudio);
                if (Utilities.IsValid(toggleAudioOff))
                    toggleAudioOff.SetActive(!isAudio);
            }
            
            if (Utilities.IsValid(AudioManager))
            {
                if (Utilities.IsValid(muteToggleOn) && Utilities.IsValid(muteToggleOff))
                {
                    muteToggleOn.SetActive(AudioManager.masterMute);
                    muteToggleOff.SetActive(!AudioManager.masterMute);
                }
                if (Utilities.IsValid(audio2DToggleOn) && Utilities.IsValid(audio2DToggleOff))
                {
                    //audio2DToggleOn.SetActive(AudioManager.audio2D);
                    //audio2DToggleOff.SetActive(!AudioManager.audio2D);
                }
            }
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(LocalControls))]
    internal class LocalControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;
        SerializedProperty staticUrlSourceProperty;
        SerializedProperty volumeControllerProperty;
        //SerializedProperty colorProfileProperty;

        SerializedProperty autoLayoutProperty;
        SerializedProperty enableResyncProprety;
        SerializedProperty enableQualitySelectProperty;
        SerializedProperty enableVolumeProperty;
        SerializedProperty enable2DAudioProperty;
        SerializedProperty enableMessageBarProperty;

        SerializedProperty volumeGroupProperty;
        SerializedProperty resyncGroupProperty;
        SerializedProperty qualityGroupproperty;
        SerializedProperty messageBarGroupProperty;

        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty toggle720OnProperty;
        SerializedProperty toggle720OffProperty;
        SerializedProperty toggle1080OnProperty;
        SerializedProperty toggle1080OffProperty;
        SerializedProperty toggleAudioOnProperty;
        SerializedProperty toggleAudioOffProperty;
        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(LocalControls.videoPlayer));
            staticUrlSourceProperty = serializedObject.FindProperty(nameof(LocalControls.staticUrlSource));
            volumeControllerProperty = serializedObject.FindProperty(nameof(LocalControls.AudioManager));
            //colorProfileProperty = serializedObject.FindProperty(nameof(LocalControls.colorProfile));

            autoLayoutProperty = serializedObject.FindProperty(nameof(LocalControls.autoLayout));
            enableResyncProprety = serializedObject.FindProperty(nameof(LocalControls.enableResync));
            enableQualitySelectProperty = serializedObject.FindProperty(nameof(LocalControls.enableQualitySelect));
            enableVolumeProperty = serializedObject.FindProperty(nameof(LocalControls.enableVolume));
            enable2DAudioProperty = serializedObject.FindProperty(nameof(LocalControls.enable2DAudioToggle));
            enableMessageBarProperty = serializedObject.FindProperty(nameof(LocalControls.enableMessageBar));

            volumeGroupProperty = serializedObject.FindProperty(nameof(LocalControls.volumeGroup));
            resyncGroupProperty = serializedObject.FindProperty(nameof(LocalControls.resyncGroup));
            qualityGroupproperty = serializedObject.FindProperty(nameof(LocalControls.toggleGroup));
            messageBarGroupProperty = serializedObject.FindProperty(nameof(LocalControls.messageBarGroup));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(LocalControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DControl));
            toggle720OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720On));
            toggle720OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720Off));
            toggle1080OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080On));
            toggle1080OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080Off));
            toggleAudioOnProperty = serializedObject.FindProperty(nameof(LocalControls.toggleAudioOn));
            toggleAudioOffProperty = serializedObject.FindProperty(nameof(LocalControls.toggleAudioOff));
            muteToggleOnProperty = serializedObject.FindProperty(nameof(LocalControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(LocalControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(LocalControls.audio2DToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(LocalControls.volumeSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(staticUrlSourceProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            //EditorGUILayout.PropertyField(colorProfileProperty);
            EditorGUILayout.PropertyField(autoLayoutProperty);
            if (autoLayoutProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = videoPlayerProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableResyncProprety);
                GUI.enabled = staticUrlSourceProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableQualitySelectProperty);
                GUI.enabled = volumeControllerProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableVolumeProperty);
                EditorGUILayout.PropertyField(enable2DAudioProperty);
                GUI.enabled = messageBarGroupProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableMessageBarProperty);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(volumeGroupProperty);
                EditorGUILayout.PropertyField(resyncGroupProperty);
                EditorGUILayout.PropertyField(qualityGroupproperty);
                EditorGUILayout.PropertyField(messageBarGroupProperty);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(toggle720OnProperty);
                EditorGUILayout.PropertyField(toggle720OffProperty);
                EditorGUILayout.PropertyField(toggle1080OnProperty);
                EditorGUILayout.PropertyField(toggle1080OffProperty);
                EditorGUILayout.PropertyField(toggleAudioOnProperty);
                EditorGUILayout.PropertyField(toggleAudioOffProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                LocalControls lc = (LocalControls)target;
                lc._UpdateLayout();
            }
        }
    }
#endif
}
