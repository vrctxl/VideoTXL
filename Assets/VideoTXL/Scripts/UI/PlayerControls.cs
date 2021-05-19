
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/UI/Player Controls")]
    public class PlayerControls : UdonSharpBehaviour
    {
        public SyncPlayer videoPlayer;
        public StaticUrlSource staticUrlSource;
        public VolumeController volumeController;
        public ControlColorProfile colorProfile;

        public bool autoLayout = true;
        //public bool enableResync = true;
        //public bool enableQualitySelect = false;
        public bool enableVolume = true;
        public bool enable2DAudioToggle = true;

        public GameObject volumeGroup;
        //public GameObject resyncGroup;
        //public GameObject toggleGroup;

        public VRCUrlInputField urlInput;

        public GameObject volumeSliderControl;
        public GameObject audio2DControl;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;

        //public GameObject toggle720On;
        //public GameObject toggle720Off;
        //public GameObject toggle1080On;
        //public GameObject toggle1080Off;
        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public GameObject infoToggleOn;
        public GameObject infoToggleOff;
        public Slider volumeSlider;

        public Slider progressSlider;
        public Text statusText;
        public Text urlText;
        public Text placeholderText;

        public GameObject infoPanel;
        public Text instanceOwnerText;
        public Text masterText;
        public Text playerOwnerText;
        public Text videoOwnerText;
        public InputField currentVideoInput;
        public InputField lastVideoInput;

        bool progressSliderValid;

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

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PAUSED = 2;
        const int PLAYER_STATE_PLAYING = 3;
        const int PLAYER_STATE_ERROR = 4;

        bool infoPanelOpen = false;

        void Start()
        {
            //SendCustomEventDelayedFrames("_UpdateLayout", 1);

            if (Utilities.IsValid(volumeController))
                volumeController._RegisterControls(gameObject);
            //if (Utilities.IsValid(staticUrlSource))
            //    staticUrlSource._RegisterControls(gameObject);

            progressSliderValid = Utilities.IsValid(progressSliderControl);

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

        /*public void _UpdateLayout()
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
        }*/

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

        /*public void _Resync()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer.SendCustomEvent("_Resync");
        }*/

        public void _HandleUrlInput()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._ChangeUrl(urlInput.GetUrl());
            urlInput.SetUrl(VRCUrl.Empty);
        }

        public void _UrlChanged()
        {
            UpdateToggleVisual();
        }

        /*public void _SetQuality720()
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
        }*/

        public void _HandleStop()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._TriggerStop();
        }

        public void _HandlePlayPause()
        {

        }

        public void _HandleInfo()
        {
            infoPanelOpen = !infoPanelOpen;
            infoToggleOn.SetActive(infoPanelOpen);
            infoToggleOff.SetActive(!infoPanelOpen);
            infoPanel.SetActive(infoPanelOpen);
        }

        bool _draggingProgressSlider = false;

        public void _HandleProgressBeginDrag()
        {
            _draggingProgressSlider = true;
        }

        public void _HandleProgressEndDrag()
        {
            _draggingProgressSlider = false;
        }

        public void _HandleProgressSliderChanged()
        {
            if (!_draggingProgressSlider)
                return;

            if (float.IsInfinity(videoPlayer.trackDuration) || videoPlayer.trackDuration <= 0)
                return;

            float targetTime = videoPlayer.trackDuration * progressSlider.value;
            videoPlayer._SetTargetTime(targetTime);
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

        private void Update()
        {
            if (videoPlayer.localPlayerState == PLAYER_STATE_PLAYING)
            {
                urlInput.readOnly = true;
                urlInputControl.SetActive(false);
                if (!videoPlayer.seekableSource)
                {
                    SetStatusText("Streaming...");
                    if (progressSliderValid)
                        progressSliderControl.SetActive(false);
                    
                    
                }
                else if (_draggingProgressSlider)
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration * progressSlider.value).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    if (progressSliderValid)
                        progressSliderControl.SetActive(true);
                }
                else
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackPosition).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    if (progressSliderValid)
                    {
                        progressSliderControl.SetActive(true);
                        progressSlider.value = Mathf.Clamp01(videoPlayer.trackPosition / videoPlayer.trackDuration);
                    }
                }
            } else
            {
                urlInput.readOnly = false;
                urlInputControl.SetActive(true);
                SetStatusText("");
                if (progressSliderValid)
                    progressSliderControl.SetActive(false);

                if (videoPlayer.localPlayerState == PLAYER_STATE_LOADING)
                {
                    placeholderText.text = "Loading...";
                    urlInput.readOnly = true;
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_ERROR)
                {
                    switch (videoPlayer.localLastErrorCode)
                    {
                        case VideoError.RateLimited:
                            placeholderText.text = "Rate limited, wait and try again";
                            break;
                        case VideoError.PlayerError:
                            placeholderText.text = "Video player error";
                            break;
                        case VideoError.InvalidURL:
                            placeholderText.text = "Invalid URL or source offline";
                            break;
                        case VideoError.AccessDenied:
                            placeholderText.text = "Video blocked, enable untrusted URLs";
                            break;
                        case VideoError.Unknown:
                        default:
                            placeholderText.text = "Failed to load video";
                            break;
                    }

                    urlInput.readOnly = false;
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_STOPPED)
                {
                    placeholderText.text = "Enter Video URL...";
                    urlInput.readOnly = false;
                }
            }

            // Move out of update
            instanceOwnerText.text = videoPlayer.instanceOwner;
            masterText.text = videoPlayer.instanceMaster;
            playerOwnerText.text = videoPlayer.playerOwner;
            videoOwnerText.text = videoPlayer.videoOwner;
            currentVideoInput.text = videoPlayer.currentUrl;
            lastVideoInput.text = videoPlayer.lastUrl;
        }

        void SetStatusText(string msg)
        {
            statusText.text = msg;
        }

        void UpdateToggleVisual()
        {
            /*if (Utilities.IsValid(staticUrlSource))
            {
                bool is720 = staticUrlSource._IsQuality720();
                bool is1080 = staticUrlSource._IsQuality1080();
                toggle720On.SetActive(is720);
                toggle720Off.SetActive(!is720);
                toggle1080On.SetActive(is1080);
                toggle1080Off.SetActive(!is1080);
            }*/

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
    [CustomEditor(typeof(PlayerControls))]
    internal class PlayerControlsInspector : Editor
    {
        static bool _showObjectFoldout;
        static bool _showColorFoldout;

        SerializedProperty videoPlayerProperty;
        //SerializedProperty staticUrlSourceProperty;
        SerializedProperty volumeControllerProperty;
        SerializedProperty colorProfileProperty;

        SerializedProperty autoLayoutProperty;
        //SerializedProperty enableResyncProprety;
        //SerializedProperty enableQualitySelectProperty;
        SerializedProperty enableVolumeProperty;
        SerializedProperty enable2DAudioProperty;

        SerializedProperty volumeGroupProperty;
        //SerializedProperty resyncGroupProperty;
        //SerializedProperty qualityGroupproperty;

        SerializedProperty urlInputProperty;

        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;
        //SerializedProperty toggle720OnProperty;
        //SerializedProperty toggle720OffProperty;
        //SerializedProperty toggle1080OnProperty;
        //SerializedProperty toggle1080OffProperty;
        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty infoToggleOnProperty;
        SerializedProperty infoToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        SerializedProperty progressSliderProperty;
        SerializedProperty statusTextProperty;
        SerializedProperty urlTextProperty;
        SerializedProperty placeholderTextProperty;

        SerializedProperty infoPanelProperty;
        SerializedProperty instanceOwnerTextProperty;
        SerializedProperty masterTextProperty;
        SerializedProperty playerOwnerTextProperty;
        SerializedProperty videoOwnerTextProperty;
        SerializedProperty currentVideoInputProperty;
        SerializedProperty lastVideoInputProperty;

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
            videoPlayerProperty = serializedObject.FindProperty(nameof(PlayerControls.videoPlayer));
            //staticUrlSourceProperty = serializedObject.FindProperty(nameof(LocalControls.staticUrlSource));
            volumeControllerProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeController));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            autoLayoutProperty = serializedObject.FindProperty(nameof(PlayerControls.autoLayout));
            //enableResyncProprety = serializedObject.FindProperty(nameof(LocalControls.enableResync));
            //enableQualitySelectProperty = serializedObject.FindProperty(nameof(LocalControls.enableQualitySelect));
            enableVolumeProperty = serializedObject.FindProperty(nameof(PlayerControls.enableVolume));
            enable2DAudioProperty = serializedObject.FindProperty(nameof(PlayerControls.enable2DAudioToggle));

            volumeGroupProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeGroup));
            //resyncGroupProperty = serializedObject.FindProperty(nameof(LocalControls.resyncGroup));
            //qualityGroupproperty = serializedObject.FindProperty(nameof(LocalControls.toggleGroup));

            urlInputProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInput));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DControl));
            progressSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInputControl));
            //toggle720OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720On));
            //toggle720OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle720Off));
            //toggle1080OnProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080On));
            //toggle1080OffProperty = serializedObject.FindProperty(nameof(LocalControls.toggle1080Off));
            muteToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOff));
            infoToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.infoToggleOn));
            infoToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.infoToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSlider));

            statusTextProperty = serializedObject.FindProperty(nameof(PlayerControls.statusText));
            placeholderTextProperty = serializedObject.FindProperty(nameof(PlayerControls.placeholderText));
            urlTextProperty = serializedObject.FindProperty(nameof(PlayerControls.urlText));
            progressSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSlider));

            infoPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.infoPanel));
            instanceOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.instanceOwnerText));
            masterTextProperty = serializedObject.FindProperty(nameof(PlayerControls.masterText));
            playerOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playerOwnerText));
            videoOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.videoOwnerText));
            currentVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.currentVideoInput));
            lastVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.lastVideoInput));

            backgroundImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.backgroundImgs));
            backgroundMsgBarImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.backgroundMsgBarImgs));
            buttonImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.buttonImgs));
            buttonSelectedImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.buttonSelectedImgs));
            brightTextsProperty = serializedObject.FindProperty(nameof(PlayerControls.brightTexts));
            dimTextsProperty = serializedObject.FindProperty(nameof(PlayerControls.dimTexts));
            brightImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.brightImgs));
            dimImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.dimImgs));
            redImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.redImgs));
            sliderBrightImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.sliderBrightImgs));
            sliderDimImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.sliderDimImgs));
            sliderGrabImgsProperty = serializedObject.FindProperty(nameof(PlayerControls.sliderGrabImgs));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawConvertToUdonBehaviourButton(target) ||
                UdonSharpGUI.DrawProgramSource(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            //EditorGUILayout.PropertyField(staticUrlSourceProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
            EditorGUILayout.PropertyField(autoLayoutProperty);
            if (autoLayoutProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = videoPlayerProperty.objectReferenceValue != null;
                //EditorGUILayout.PropertyField(enableResyncProprety);
                //GUI.enabled = staticUrlSourceProperty.objectReferenceValue != null;
                //EditorGUILayout.PropertyField(enableQualitySelectProperty);
                //GUI.enabled = volumeControllerProperty.objectReferenceValue != null;
                EditorGUILayout.PropertyField(enableVolumeProperty);
                EditorGUILayout.PropertyField(enable2DAudioProperty);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(volumeGroupProperty);
                //EditorGUILayout.PropertyField(resyncGroupProperty);
                //EditorGUILayout.PropertyField(qualityGroupproperty);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                //EditorGUILayout.PropertyField(toggle720OnProperty);
                //EditorGUILayout.PropertyField(toggle720OffProperty);
                //EditorGUILayout.PropertyField(toggle1080OnProperty);
                //EditorGUILayout.PropertyField(toggle1080OffProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(infoToggleOnProperty);
                EditorGUILayout.PropertyField(infoToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
                EditorGUILayout.PropertyField(progressSliderProperty);
                EditorGUILayout.PropertyField(statusTextProperty);
                EditorGUILayout.PropertyField(urlTextProperty);
                EditorGUILayout.PropertyField(placeholderTextProperty);
                EditorGUILayout.PropertyField(infoPanelProperty);
                EditorGUILayout.PropertyField(instanceOwnerTextProperty);
                EditorGUILayout.PropertyField(masterTextProperty);
                EditorGUILayout.PropertyField(playerOwnerTextProperty);
                EditorGUILayout.PropertyField(videoOwnerTextProperty);
                EditorGUILayout.PropertyField(currentVideoInputProperty);
                EditorGUILayout.PropertyField(lastVideoInputProperty);
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
                PlayerControls lc = (PlayerControls)target;
                //lc._UpdateLayout();
                lc._UpdateColor();
            }
        }
    }
#endif
}
