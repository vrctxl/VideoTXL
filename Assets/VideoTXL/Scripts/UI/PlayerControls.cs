
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
        public Texel.AudioManager audioManager;
        public Playlist playlist;
        public ControlColorProfile colorProfile;

        public VRCUrlInputField urlInput;

        public GameObject volumeSliderControl;
        public GameObject audio2DControl;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;

        public Image stopIcon;
        public Image pauseIcon;
        public Image lockedIcon;
        public Image unlockedIcon;
        public Image loadIcon;
        public Image resyncIcon;
        public Image repeatIcon;
        public Image shuffleIcon;
        public Image infoIcon;
        public Image playCurrentIcon;
        public Image playLastIcon;
        public Image nextIcon;
        public Image prevIcon;
        public Image playlistIcon;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public Slider volumeSlider;

        public Slider progressSlider;
        public Text statusText;
        public Text urlText;
        public Text placeholderText;

        public Text playlistText;

        public GameObject infoPanel;
        public Text instanceOwnerText;
        public Text masterText;
        public Text playerOwnerText;
        public Text videoOwnerText;
        public InputField currentVideoInput;
        public InputField lastVideoInput;

        VideoPlayerProxy dataProxy;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;
        const int PLAYER_STATE_PAUSED = 4;

        bool infoPanelOpen = false;

        string statusOverride = null;
        string instanceMaster = "";
        string instanceOwner = "";

        bool loadActive = false;
        VRCUrl pendingSubmit;
        bool pendingFromLoadOverride = false;

        void Start()
        {
            infoIcon.color = normalColor;
            _DisableAllVideoControls();

            if (Utilities.IsValid(audioManager))
                audioManager._RegisterControls(gameObject);
            if (Utilities.IsValid(videoPlayer) && Utilities.IsValid(videoPlayer.dataProxy)) {
                dataProxy = videoPlayer.dataProxy;
                dataProxy._RegisterEventHandler(gameObject, "_VideoStateUpdate");
                dataProxy._RegisterEventHandler(gameObject, "_VideoLockUpdate");
                dataProxy._RegisterEventHandler(gameObject, "_VideoTrackingUpdate");
                dataProxy._RegisterEventHandler(gameObject, "_VideoInfoUpdate");
                dataProxy._RegisterEventHandler(gameObject, "_VideoPlaylistUpdate");

                unlockedIcon.color = normalColor;
            }

#if !UNITY_EDITOR
            instanceMaster = Networking.GetOwner(gameObject).displayName;
            _FindOwners();
#endif
        }

        void _DisableAllVideoControls()
        {
            stopIcon.color = disabledColor;
            pauseIcon.color = disabledColor;
            lockedIcon.color = disabledColor;
            unlockedIcon.color = disabledColor;
            loadIcon.color = disabledColor;
            resyncIcon.color = disabledColor;
            repeatIcon.color = disabledColor;
            shuffleIcon.color = disabledColor;
            playCurrentIcon.color = disabledColor;
            playLastIcon.color = disabledColor;
            nextIcon.color = disabledColor;
            prevIcon.color = disabledColor;
            playlistIcon.color = disabledColor;
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

        /*public void _VolumeControllerUpdate()
        {
            if (!Utilities.IsValid(audioManager))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = audioManager.volume;
                if (volume != volumeSlider.value)
                    volumeSlider.value = volume;
            }

            UpdateToggleVisual();

            inVolumeControllerUpdate = false;
        }*/

        public void _VideoStateUpdate()
        {
            _UpdateAll();
        }

        public void _VideoLockUpdate()
        {
            _UpdateAll();
        }

        public void _VideoTrackingUpdate()
        {
            _UpdateTracking();
        }

        public void _VideoInfoUpdate()
        {
            _UpdateInfo();
        }

        public void _VideoPlaylistUpdate()
        {
            _UpdatePlaylistInfo();
        }

        public void _HandleUrlInput()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            pendingFromLoadOverride = loadActive;
            pendingSubmit = urlInput.GetUrl();

            SendCustomEventDelayedSeconds("_HandleUrlInputDelay", 0.5f);
        }

        public void _HandleUrlInputDelay()
        {
            VRCUrl url = urlInput.GetUrl();
            urlInput.SetUrl(VRCUrl.Empty);

            // Hack to get around Unity always firing OnEndEdit event for submit and lost focus
            // If loading override was on, but it's off immediately after submit, assume user closed override
            // instead of submitting.  Half second delay is a crude defense against a UI race.
            if (pendingFromLoadOverride && !loadActive)
                return;

            videoPlayer._ChangeUrl(url);
            if (Utilities.IsValid(playlist))
                playlist._SetEnabled(false);
            loadActive = false;
            _UpdateAll();
        }

        public void _HandleUrlInputClick()
        {
            if (!videoPlayer._CanTakeControl())
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleUrlInputChange()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            VRCUrl url = urlInput.GetUrl();
            if (url.Get().Length > 0)
                videoPlayer._UpdateQueuedUrl(urlInput.GetUrl());
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

        public void _HandlePause()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerPause();
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleResync()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            videoPlayer._Resync();
        }

        public void _HandlePlayCurrent()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;
            if (videoPlayer.currentUrl == VRCUrl.Empty)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._ChangeUrl(videoPlayer.currentUrl);
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandlePlayLast()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;
            if (videoPlayer.lastUrl == VRCUrl.Empty)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._ChangeUrl(videoPlayer.lastUrl);
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleInfo()
        {
            infoPanelOpen = !infoPanelOpen;
            infoPanel.SetActive(infoPanelOpen);
            infoIcon.color = infoPanelOpen ? activeColor : normalColor;
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

        public void _HandleLoad()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (!videoPlayer._CanTakeControl())
            {
                _SetStatusOverride(MakeOwnerMessage(), 3);
                return;
            }

            if (videoPlayer.localPlayerState == PLAYER_STATE_ERROR)
                loadActive = false;
            else
                loadActive = !loadActive;

            _UpdateAll();
        }

        public void _HandleRepeat()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerRepeatMode();
            else
                _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        bool _draggingProgressSlider = false;

        public void _HandleProgressBeginDrag()
        {
            _draggingProgressSlider = true;
            _UpdateTrackingDragging();
        }

        public void _HandleProgressEndDrag()
        {
            _draggingProgressSlider = false;
        }

        public void _HandleProgressSliderChanged()
        {
            if (!_draggingProgressSlider)
                return;

            if (float.IsInfinity(dataProxy.trackDuration) || dataProxy.trackDuration <= 0)
                return;

            float targetTime = dataProxy.trackDuration * progressSlider.value;
            videoPlayer._SetTargetTime(targetTime);
        }

        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager))
                audioManager._SetMasterMute(!audioManager.masterMute);
                //audioManager._ToggleMute();
        }

        public void _ToggleAudio2D()
        {
            if (inVolumeControllerUpdate)
                return;

            //if (Utilities.IsValid(audioManager))
            //    audioManager._ToggleAudio2D();
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager) && Utilities.IsValid(volumeSlider))
                audioManager._SetMasterVolume(volumeSlider.value);
                //audioManager._ApplyVolume(volumeSlider.value);
        }

        public void _HandlePlaylist()
        {
            if (!Utilities.IsValid(playlist) || !Utilities.IsValid(videoPlayer))
                return;

            if (playlist.playlistEnabled)
                return;

            playlist._SetEnabled(true);
            videoPlayer._ChangeUrl(playlist._GetCurrent());
        }

        public void _HandlePlaylistNext()
        {
            if (!Utilities.IsValid(playlist) || !Utilities.IsValid(videoPlayer))
                return;

            if (playlist._MoveNext())
                videoPlayer._ChangeUrl(playlist._GetCurrent());
        }

        public void _HandlePlaylistPrev()
        {
            if (!Utilities.IsValid(playlist) || !Utilities.IsValid(videoPlayer))
                return;

            if (playlist._MovePrev())
                videoPlayer._ChangeUrl(playlist._GetCurrent());
        }

        void _SetStatusOverride(string msg, float timeout)
        {
            statusOverride = msg;
            SendCustomEventDelayedSeconds("_ClearStatusOverride", timeout);
            _UpdateAll();
        }

        public void _ClearStatusOverride()
        {
            statusOverride = null;
            _UpdateAll();
        }

        public void _UpdateTrackingDragging()
        {
            int playerState = dataProxy.playerState;
            bool playingState = playerState == PLAYER_STATE_PLAYING || playerState == PLAYER_STATE_PAUSED;

            if (!_draggingProgressSlider || !playingState || loadActive || !dataProxy.seekableSource)
                return;

            string durationStr = System.TimeSpan.FromSeconds(dataProxy.trackDuration).ToString(@"hh\:mm\:ss");
            string positionStr = System.TimeSpan.FromSeconds(dataProxy.trackDuration * progressSlider.value).ToString(@"hh\:mm\:ss");
            SetStatusText(positionStr + " / " + durationStr);
            progressSliderControl.SetActive(true);

            SendCustomEventDelayedSeconds("_UpdateTrackingDragging", 0.1f);
        }

        public void _UpdateTracking()
        {
            int playerState = dataProxy.playerState;
            bool playingState = playerState == PLAYER_STATE_PLAYING || playerState == PLAYER_STATE_PAUSED;

            if (!playingState || loadActive)
                return;

            if (!videoPlayer.seekableSource)
            {
                SetStatusText("Streaming...");
                progressSliderControl.SetActive(false);
            }
            else if (!_draggingProgressSlider)
            {
                string durationStr = System.TimeSpan.FromSeconds(dataProxy.trackDuration).ToString(@"hh\:mm\:ss");
                string positionStr = System.TimeSpan.FromSeconds(dataProxy.trackPosition).ToString(@"hh\:mm\:ss");
                SetStatusText(positionStr + " / " + durationStr);
                progressSliderControl.SetActive(true);
                progressSlider.value = Mathf.Clamp01(dataProxy.trackPosition / dataProxy.trackDuration);
            }
        }

        public void _UpdateInfo()
        {
            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            string currentUrl = videoPlayer.currentUrl.Get();
            string lastUrl = videoPlayer.lastUrl.Get();

            playCurrentIcon.color = (enableControl && currentUrl != "") ? normalColor : disabledColor;
            playLastIcon.color = (enableControl && lastUrl != "") ? normalColor : disabledColor;

            instanceOwnerText.text = instanceOwner;
            masterText.text = instanceMaster;
            // videoOwnerText.text = videoPlayer.videoOwner;
            currentVideoInput.text = currentUrl;
            lastVideoInput.text = lastUrl;

            VRCPlayerApi owner = Networking.GetOwner(videoPlayer.gameObject);
            if (Utilities.IsValid(owner) && owner.IsValid())
                playerOwnerText.text = owner.displayName;
            else
                playerOwnerText.text = "";
            
        }

        public void _UpdatePlaylistInfo()
        {
            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            repeatIcon.color = videoPlayer.repeatPlaylist ? activeColor : normalColor;

            if (Utilities.IsValid(playlist) && playlist.trackCount > 0)
            {
                nextIcon.color = (enableControl && playlist.playlistEnabled && playlist._HasNextTrack()) ? normalColor : disabledColor;
                prevIcon.color = (enableControl && playlist.playlistEnabled && playlist._HasPrevTrack()) ? normalColor : disabledColor;
                playlistIcon.color = enableControl ? normalColor : disabledColor;

                string curIndex = playlist.playlistEnabled ? (playlist.currentIndex + 1).ToString() : "--";
                playlistText.text = $"{curIndex} / {playlist.trackCount}";
            }
            else
            {
                nextIcon.color = disabledColor;
                prevIcon.color = disabledColor;
                playlistIcon.color = disabledColor;
                playlistText.text = "";
            }
        }

        public void _UpdateAll()
        {
            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            int playerState = dataProxy.playerState;

            bool playingState = playerState == PLAYER_STATE_PLAYING || playerState == PLAYER_STATE_PAUSED;
            if (playingState && !loadActive)
            {
                urlInput.readOnly = true;
                urlInputControl.SetActive(false);

                stopIcon.color = enableControl ? normalColor : disabledColor;
                loadIcon.color = enableControl ? normalColor : disabledColor;
                resyncIcon.color = normalColor;

                if (playerState == PLAYER_STATE_PAUSED)
                    pauseIcon.color = activeColor;
                else
                    pauseIcon.color = (enableControl && videoPlayer.seekableSource) ? normalColor : disabledColor;

                progressSlider.interactable = enableControl;
                _UpdateTracking();
            }
            else
            {
                _draggingProgressSlider = false;

                stopIcon.color = disabledColor;
                loadIcon.color = disabledColor;
                progressSliderControl.SetActive(false);
                urlInputControl.SetActive(true);

                if (playerState == PLAYER_STATE_LOADING)
                {
                    stopIcon.color = enableControl ? normalColor : disabledColor;
                    loadIcon.color = enableControl ? normalColor : disabledColor;
                    resyncIcon.color = normalColor;
                    pauseIcon.color = disabledColor;

                    SetPlaceholderText("Loading...");
                    urlInput.readOnly = true;
                    SetStatusText("");
                }
                else if (playerState == PLAYER_STATE_ERROR)
                {
                    stopIcon.color = disabledColor;
                    loadIcon.color = normalColor;
                    resyncIcon.color = normalColor;
                    pauseIcon.color = disabledColor;
                    loadActive = false;

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
                else if (playerState == PLAYER_STATE_STOPPED || playingState)
                {
                    if (playerState == PLAYER_STATE_STOPPED)
                    {
                        loadActive = false;
                        pendingFromLoadOverride = false;
                        stopIcon.color = disabledColor;
                        loadIcon.color = disabledColor;
                        resyncIcon.color = disabledColor;
                        pauseIcon.color = disabledColor;
                    }
                    else
                    {
                        stopIcon.color = enableControl ? normalColor : disabledColor;
                        loadIcon.color = activeColor;
                        resyncIcon.color = normalColor;

                        if (playerState == PLAYER_STATE_PAUSED)
                            pauseIcon.color = activeColor;
                        else
                            pauseIcon.color = (enableControl && videoPlayer.seekableSource) ? normalColor : disabledColor;
                    }

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

            lockedIcon.enabled = videoPlayer.locked;
            unlockedIcon.enabled = !videoPlayer.locked;
            if (videoPlayer.locked)
                lockedIcon.color = canControl ? normalColor : attentionColor;

            _UpdatePlaylistInfo();
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

        void _FindOwners()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            VRCPlayerApi[] playerList = new VRCPlayerApi[playerCount];
            playerList = VRCPlayerApi.GetPlayers(playerList);

            foreach (VRCPlayerApi player in playerList)
            {
                if (!Utilities.IsValid(player) || !player.IsValid())
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
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            if (Utilities.IsValid(owner) && owner.IsValid())
                instanceMaster = owner.displayName;
            else
                instanceMaster = "";
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
                if (Utilities.IsValid(audio2DToggleOn) && Utilities.IsValid(audio2DToggleOff))
                {
                    //audio2DToggleOn.SetActive(audioManager.audio2D);
                    //audio2DToggleOff.SetActive(!audioManager.audio2D);
                }
            }
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(PlayerControls))]
    internal class PlayerControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;
        SerializedProperty volumeControllerProperty;
        SerializedProperty playlistProperty;
        SerializedProperty colorProfileProperty;

        SerializedProperty urlInputProperty;

        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;

        SerializedProperty stopIconProperty;
        SerializedProperty pauseIconProperty;
        SerializedProperty lockedIconProperty;
        SerializedProperty unlockedIconProperty;
        SerializedProperty loadIconProperty;
        SerializedProperty resyncIconProperty;
        SerializedProperty repeatIconProperty;
        SerializedProperty shuffleIconProperty;
        SerializedProperty infoIconProperty;
        SerializedProperty playCurrentIconProperty;
        SerializedProperty playlastIconProperty;
        SerializedProperty nextIconProperty;
        SerializedProperty prevIconProperty;
        SerializedProperty playlistIconProperty;

        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        SerializedProperty progressSliderProperty;
        SerializedProperty statusTextProperty;
        SerializedProperty urlTextProperty;
        SerializedProperty placeholderTextProperty;

        SerializedProperty playlistTextProperty;

        SerializedProperty infoPanelProperty;
        SerializedProperty instanceOwnerTextProperty;
        SerializedProperty masterTextProperty;
        SerializedProperty playerOwnerTextProperty;
        SerializedProperty videoOwnerTextProperty;
        SerializedProperty currentVideoInputProperty;
        SerializedProperty lastVideoInputProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(PlayerControls.videoPlayer));
            volumeControllerProperty = serializedObject.FindProperty(nameof(PlayerControls.audioManager));
            playlistProperty = serializedObject.FindProperty(nameof(PlayerControls.playlist));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            urlInputProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInput));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DControl));
            progressSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInputControl));

            stopIconProperty = serializedObject.FindProperty(nameof(PlayerControls.stopIcon));
            pauseIconProperty = serializedObject.FindProperty(nameof(PlayerControls.pauseIcon));
            lockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.lockedIcon));
            unlockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.unlockedIcon));
            loadIconProperty = serializedObject.FindProperty(nameof(PlayerControls.loadIcon));
            resyncIconProperty = serializedObject.FindProperty(nameof(PlayerControls.resyncIcon));
            repeatIconProperty = serializedObject.FindProperty(nameof(PlayerControls.repeatIcon));
            shuffleIconProperty = serializedObject.FindProperty(nameof(PlayerControls.shuffleIcon));
            infoIconProperty = serializedObject.FindProperty(nameof(PlayerControls.infoIcon));
            playCurrentIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playCurrentIcon));
            playlastIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playLastIcon));
            nextIconProperty = serializedObject.FindProperty(nameof(PlayerControls.nextIcon));
            prevIconProperty = serializedObject.FindProperty(nameof(PlayerControls.prevIcon));
            playlistIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistIcon));

            muteToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSlider));

            statusTextProperty = serializedObject.FindProperty(nameof(PlayerControls.statusText));
            placeholderTextProperty = serializedObject.FindProperty(nameof(PlayerControls.placeholderText));
            urlTextProperty = serializedObject.FindProperty(nameof(PlayerControls.urlText));
            progressSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSlider));

            playlistTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistText));

            infoPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.infoPanel));
            instanceOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.instanceOwnerText));
            masterTextProperty = serializedObject.FindProperty(nameof(PlayerControls.masterText));
            playerOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playerOwnerText));
            videoOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.videoOwnerText));
            currentVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.currentVideoInput));
            lastVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.lastVideoInput));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.PropertyField(playlistProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(stopIconProperty);
                EditorGUILayout.PropertyField(pauseIconProperty);
                EditorGUILayout.PropertyField(lockedIconProperty);
                EditorGUILayout.PropertyField(unlockedIconProperty);
                EditorGUILayout.PropertyField(loadIconProperty);
                EditorGUILayout.PropertyField(resyncIconProperty);
                EditorGUILayout.PropertyField(repeatIconProperty);
                EditorGUILayout.PropertyField(shuffleIconProperty);
                EditorGUILayout.PropertyField(infoIconProperty);
                EditorGUILayout.PropertyField(playCurrentIconProperty);
                EditorGUILayout.PropertyField(playlastIconProperty);
                EditorGUILayout.PropertyField(nextIconProperty);
                EditorGUILayout.PropertyField(prevIconProperty);
                EditorGUILayout.PropertyField(playlistIconProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
                EditorGUILayout.PropertyField(progressSliderProperty);
                EditorGUILayout.PropertyField(statusTextProperty);
                EditorGUILayout.PropertyField(urlTextProperty);
                EditorGUILayout.PropertyField(placeholderTextProperty);
                EditorGUILayout.PropertyField(playlistTextProperty);
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

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
