
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
        public bool enableVolume = true;
        public bool enable2DAudioToggle = true;

        public GameObject volumeGroup;
        //public GameObject resyncGroup;

        public VRCUrlInputField urlInput;

        public GameObject volumeSliderControl;
        public GameObject audio2DControl;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public GameObject infoToggleOn;
        public GameObject infoToggleOff;
        public GameObject stopButton;
        public GameObject stopButtonDisabled;
        public GameObject pauseButton;
        public GameObject playButton;
        public GameObject playButtonDisabled;
        public GameObject lockButtonOpen;
        public GameObject lockButtonClosed;
        public GameObject lockButtonDenied;
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
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        bool infoPanelOpen = false;

        string statusOverride = null;
        string instanceMaster = "";
        string instanceOwner = "";

        void Start()
        {
            if (Utilities.IsValid(volumeController))
                volumeController._RegisterControls(gameObject);

#if !UNITY_EDITOR
            instanceMaster = Networking.GetOwner(gameObject).displayName;
#endif

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

        public void _HandleUrlInputClick()
        {
            if (!videoPlayer._CanTakeControl())
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleStop()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerStop();
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
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

        public void _HandleLock()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerLock();
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
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

        void _SetStatusOverride(string msg, float timeout)
        {
            statusOverride = msg;
            SendCustomEventDelayedSeconds("_ClearStatusOverride", timeout);
        }

        public void _ClearStatusOverride()
        {
            statusOverride = null;
        }

        private void Update()
        {
            bool canControl = videoPlayer._CanTakeControl();

            if (videoPlayer.localPlayerState == PLAYER_STATE_PLAYING)
            {
                urlInput.readOnly = true;
                urlInputControl.SetActive(false);

                bool enableControl = !videoPlayer.locked || canControl;
                stopButton.SetActive(enableControl);
                stopButtonDisabled.SetActive(!enableControl);

                if (!videoPlayer.seekableSource)
                {
                    SetStatusText("Streaming...");
                    progressSliderControl.SetActive(false);
                    
                    
                }
                else if (_draggingProgressSlider)
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration * progressSlider.value).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    progressSliderControl.SetActive(true);
                }
                else
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackPosition).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    progressSliderControl.SetActive(true);
                    progressSlider.value = Mathf.Clamp01(videoPlayer.trackPosition / videoPlayer.trackDuration);
                }
                progressSlider.interactable = canControl;
            } else
            {
                stopButton.SetActive(false);
                stopButtonDisabled.SetActive(true);
                progressSliderControl.SetActive(false);
                urlInputControl.SetActive(true);

                if (videoPlayer.localPlayerState == PLAYER_STATE_LOADING)
                {
                    SetPlaceholderText("Loading...");
                    urlInput.readOnly = true;
                    SetStatusText("");
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_ERROR)
                {
                    switch (videoPlayer.localLastErrorCode)
                    {
                        case VideoError.RateLimited:
                            SetPlaceholderText("Rate limited, wait and try again");
                            break;
                        case VideoError.PlayerError:
                            SetPlaceholderText("Video player error");
                            break;
                        case VideoError.InvalidURL:
                            SetPlaceholderText("Invalid URL or source offline");
                            break;
                        case VideoError.AccessDenied:
                            SetPlaceholderText("Video blocked, enable untrusted URLs");
                            break;
                        case VideoError.Unknown:
                        default:
                            SetPlaceholderText("Failed to load video");
                            break;
                    }

                    urlInput.readOnly = !canControl;
                    SetStatusText("");
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_STOPPED)
                {
                    urlInput.readOnly = !canControl;
                    if (canControl)
                    {
                        SetPlaceholderText("Enter Video URL...");
                        SetStatusText("");
                    }
                    else
                    {
                        SetPlaceholderText("");
                        SetStatusText(MakeOwnerMessage());
                    }
                }
            }

            lockButtonClosed.SetActive(videoPlayer.locked && canControl);
            lockButtonDenied.SetActive(videoPlayer.locked && !canControl);
            lockButtonOpen.SetActive(!videoPlayer.locked);

            // Move out of update
            instanceOwnerText.text = instanceOwner;
            masterText.text = instanceMaster;
            playerOwnerText.text = Networking.GetOwner(videoPlayer.gameObject).displayName;
            // videoOwnerText.text = videoPlayer.videoOwner;
            currentVideoInput.text = videoPlayer.currentUrl;
            lastVideoInput.text = videoPlayer.lastUrl;
        }

        void SetStatusText(string msg)
        {
            if (statusOverride != null)
                statusText.text = statusOverride;
            else
                statusText.text = msg;
        }

        void SetPlaceholderText(string msg)
        {
            if (statusOverride != null)
                placeholderText.text = "";
            else
                placeholderText.text = msg;
        }

        void FindOwners()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            VRCPlayerApi[] playerList = new VRCPlayerApi[playerCount];
            playerList = VRCPlayerApi.GetPlayers(playerList);

            foreach (VRCPlayerApi player in playerList)
            {
                if (!Utilities.IsValid(player))
                    continue;
                if (player.isInstanceOwner)
                    instanceOwner = player.displayName;
                if (player.isMaster)
                    instanceMaster = player.displayName;
            }
        }

        string MakeOwnerMessage()
        {
            if (instanceMaster == instanceOwner || instanceOwner == "")
                return $"Controls locked to master {instanceMaster}";
            else
                return $"Controls locked to master {instanceMaster} and owner {instanceOwner}";
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            instanceMaster = Networking.GetOwner(gameObject).displayName;
        }

        void UpdateToggleVisual()
        {
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
        SerializedProperty volumeControllerProperty;
        SerializedProperty colorProfileProperty;

        SerializedProperty autoLayoutProperty;
        //SerializedProperty enableResyncProprety;
        SerializedProperty enableVolumeProperty;
        SerializedProperty enable2DAudioProperty;

        SerializedProperty volumeGroupProperty;
        //SerializedProperty resyncGroupProperty;

        SerializedProperty urlInputProperty;

        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;
        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty infoToggleOnProperty;
        SerializedProperty infoToggleOffProperty;
        SerializedProperty stopButtonProperty;
        SerializedProperty stopButtonDisabledProperty;
        SerializedProperty pauseButtonProperty;
        SerializedProperty playButtonProperty;
        SerializedProperty playButtonDisabledProperty;
        SerializedProperty lockButtonOpenProperty;
        SerializedProperty lockButtonClosedProperty;
        SerializedProperty lockButtonDeniedProperty;
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
            volumeControllerProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeController));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            autoLayoutProperty = serializedObject.FindProperty(nameof(PlayerControls.autoLayout));
            //enableResyncProprety = serializedObject.FindProperty(nameof(LocalControls.enableResync));
            enableVolumeProperty = serializedObject.FindProperty(nameof(PlayerControls.enableVolume));
            enable2DAudioProperty = serializedObject.FindProperty(nameof(PlayerControls.enable2DAudioToggle));

            volumeGroupProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeGroup));
            //resyncGroupProperty = serializedObject.FindProperty(nameof(LocalControls.resyncGroup));

            urlInputProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInput));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DControl));
            progressSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInputControl));
            muteToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOff));
            infoToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.infoToggleOn));
            infoToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.infoToggleOff));
            stopButtonProperty = serializedObject.FindProperty(nameof(PlayerControls.stopButton));
            stopButtonDisabledProperty = serializedObject.FindProperty(nameof(PlayerControls.stopButtonDisabled));
            pauseButtonProperty = serializedObject.FindProperty(nameof(PlayerControls.pauseButton));
            playButtonProperty = serializedObject.FindProperty(nameof(PlayerControls.playButton));
            playButtonDisabledProperty = serializedObject.FindProperty(nameof(PlayerControls.playButtonDisabled));
            lockButtonOpenProperty = serializedObject.FindProperty(nameof(PlayerControls.lockButtonOpen));
            lockButtonClosedProperty = serializedObject.FindProperty(nameof(PlayerControls.lockButtonClosed));
            lockButtonDeniedProperty = serializedObject.FindProperty(nameof(PlayerControls.lockButtonDenied));
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
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
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
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(infoToggleOnProperty);
                EditorGUILayout.PropertyField(infoToggleOffProperty);
                EditorGUILayout.PropertyField(stopButtonProperty);
                EditorGUILayout.PropertyField(stopButtonDisabledProperty);
                EditorGUILayout.PropertyField(pauseButtonProperty);
                EditorGUILayout.PropertyField(playButtonProperty);
                EditorGUILayout.PropertyField(playButtonDisabledProperty);
                EditorGUILayout.PropertyField(lockButtonOpenProperty);
                EditorGUILayout.PropertyField(lockButtonClosedProperty);
                EditorGUILayout.PropertyField(lockButtonDeniedProperty);
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
                lc._UpdateColor();
            }
        }
    }
#endif
}
