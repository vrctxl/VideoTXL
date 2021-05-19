
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

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/UI/Local Controls")]
    public class LocalControls : UdonSharpBehaviour
    {
        public UdonBehaviour videoPlayer;
        public StaticUrlSource staticUrlSource;
        public VolumeController volumeController;
        public ControlColorProfile colorProfile;

        public bool autoLayout = true;
        public bool enableResync = true;
        public bool enableQualitySelect = false;
        public bool enableVolume = true;
        public bool enable2DAudioToggle = true;
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

        public Image[] backgroundImgs;
        public Image[] backgroundMsgBarImgs;
        public Image[] buttonImgs;
        public Image[] buttonSelectedImgs;
        public Text[] brightTexts;
        public Text[] dimTexts;
        public Image[] brightImgs;
        public Image[] dimImgs;
        public Image[] redImgs;
        public Image[] sliderBrightImgs;
        public Image[] sliderDimImgs;
        public Image[] sliderGrabImgs;

        void Start()
        {
            SendCustomEventDelayedFrames("_UpdateLayout", 1);

            if (Utilities.IsValid(volumeController))
                volumeController._RegisterControls(gameObject);
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._RegisterControls(gameObject);

            _UpdateColor();
        }

        public void _UpdateColor()
        {
            if (!Utilities.IsValid(colorProfile))
                return;

            foreach (Image image in backgroundImgs)
                if (image != null) image.color = colorProfile.backgroundColor;
            foreach (Image image in backgroundMsgBarImgs)
                if (image != null) image.color = colorProfile.backgroundMsgBarColor;
            foreach (Image image in buttonImgs)
                if (image != null) image.color = colorProfile.buttonColor;
            foreach (Image image in buttonSelectedImgs)
                if (image != null) image.color = colorProfile.buttonSelectedColor;
            foreach (Text text in brightTexts)
                if (text != null) text.color = colorProfile.brightLabelColor;
            foreach (Text text in dimTexts)
                if (text != null) text.color = colorProfile.dimLabelColor;
            foreach (Image image in brightImgs)
                if (image != null) image.color = colorProfile.brightLabelColor;
            foreach (Image image in dimImgs)
                if (image != null) image.color = colorProfile.dimLabelColor;
            foreach (Image image in redImgs)
                if (image != null) image.color = colorProfile.redLabelColor;
            foreach (Image image in sliderBrightImgs)
                if (image != null) image.color = colorProfile.brightSliderColor;
            foreach (Image image in sliderDimImgs)
                if (image != null) image.color = colorProfile.dimSliderColor;
            foreach (Image image in sliderGrabImgs)
                if (image != null) image.color = colorProfile.sliderGrabColor;
        }

        public void _UpdateLayout()
        {
            if (!autoLayout)
                return;

            bool volumePresent = Utilities.IsValid(volumeGroup) && Utilities.IsValid(volumeController);

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

        public void _VolumeControllerUpdate()
        {
            if (!Utilities.IsValid(volumeController))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = volumeController.volume;
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

            if (Utilities.IsValid(volumeController))
                volumeController._ToggleMute();
        }

        public void _ToggleAudio2D()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(volumeController))
                volumeController._ToggleAudio2D();
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(volumeController) && Utilities.IsValid(volumeSlider))
                volumeController._ApplyVolume(volumeSlider.value);
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
            
            if (Utilities.IsValid(volumeController))
            {
                if (Utilities.IsValid(muteToggleOn) && Utilities.IsValid(muteToggleOff))
                {
                    muteToggleOn.SetActive(volumeController.muted);
                    muteToggleOff.SetActive(!volumeController.muted);
                }
                if (Utilities.IsValid(audio2DToggleOn) && Utilities.IsValid(audio2DToggleOff))
                {
                    audio2DToggleOn.SetActive(volumeController.audio2D);
                    audio2DToggleOff.SetActive(!volumeController.audio2D);
                }
            }
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(LocalControls))]
    internal class LocalControlsInspector : Editor
    {
        static bool _showObjectFoldout;
        static bool _showColorFoldout;

        SerializedProperty videoPlayerProperty;
        SerializedProperty staticUrlSourceProperty;
        SerializedProperty volumeControllerProperty;
        SerializedProperty colorProfileProperty;

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

        SerializedProperty backgroundImgsProperty;
        SerializedProperty backgroundMsgBarImgsProperty;
        SerializedProperty buttonImgsProperty;
        SerializedProperty buttonSelectedImgsProperty;
        SerializedProperty brightTextsProperty;
        SerializedProperty dimTextsProperty;
        SerializedProperty brightImgsProperty;
        SerializedProperty dimImgsProperty;
        SerializedProperty redImgsProperty;
        SerializedProperty sliderBrightImgsProperty;
        SerializedProperty sliderDimImgsProperty;
        SerializedProperty sliderGrabImgsProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(LocalControls.videoPlayer));
            staticUrlSourceProperty = serializedObject.FindProperty(nameof(LocalControls.staticUrlSource));
            volumeControllerProperty = serializedObject.FindProperty(nameof(LocalControls.volumeController));
            colorProfileProperty = serializedObject.FindProperty(nameof(LocalControls.colorProfile));

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

            backgroundImgsProperty = serializedObject.FindProperty(nameof(LocalControls.backgroundImgs));
            backgroundMsgBarImgsProperty = serializedObject.FindProperty(nameof(LocalControls.backgroundMsgBarImgs));
            buttonImgsProperty = serializedObject.FindProperty(nameof(LocalControls.buttonImgs));
            buttonSelectedImgsProperty = serializedObject.FindProperty(nameof(LocalControls.buttonSelectedImgs));
            brightTextsProperty = serializedObject.FindProperty(nameof(LocalControls.brightTexts));
            dimTextsProperty = serializedObject.FindProperty(nameof(LocalControls.dimTexts));
            brightImgsProperty = serializedObject.FindProperty(nameof(LocalControls.brightImgs));
            dimImgsProperty = serializedObject.FindProperty(nameof(LocalControls.dimImgs));
            redImgsProperty = serializedObject.FindProperty(nameof(LocalControls.redImgs));
            sliderBrightImgsProperty = serializedObject.FindProperty(nameof(LocalControls.sliderBrightImgs));
            sliderDimImgsProperty = serializedObject.FindProperty(nameof(LocalControls.sliderDimImgs));
            sliderGrabImgsProperty = serializedObject.FindProperty(nameof(LocalControls.sliderGrabImgs));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(staticUrlSourceProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
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

            _showColorFoldout = EditorGUILayout.Foldout(_showColorFoldout, "Color Profile Object References");
            if (_showColorFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(backgroundImgsProperty, true);
                EditorGUILayout.PropertyField(backgroundMsgBarImgsProperty, true);
                EditorGUILayout.PropertyField(buttonImgsProperty, true);
                EditorGUILayout.PropertyField(buttonSelectedImgsProperty, true);
                EditorGUILayout.PropertyField(brightTextsProperty, true);
                EditorGUILayout.PropertyField(dimTextsProperty, true);
                EditorGUILayout.PropertyField(brightImgsProperty, true);
                EditorGUILayout.PropertyField(dimImgsProperty, true);
                EditorGUILayout.PropertyField(redImgsProperty, true);
                EditorGUILayout.PropertyField(sliderBrightImgsProperty, true);
                EditorGUILayout.PropertyField(sliderDimImgsProperty, true);
                EditorGUILayout.PropertyField(sliderGrabImgsProperty, true);
                EditorGUI.indentLevel--;
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                LocalControls lc = (LocalControls)target;
                lc._UpdateLayout();
                lc._UpdateColor();
            }
        }
    }
#endif
}
