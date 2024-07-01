
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using System;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControls : UdonSharpBehaviour
    {
        public SyncPlayer videoPlayer;
        public AudioManager audioManager;
        public ControlColorProfile colorProfile;

        public VRCUrlInputField urlInput;

        public GameObject volumeSliderControl;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;
        public GameObject syncSliderControl;

        public Image stopIcon;
        public Image pauseIcon;
        public Image lockedIcon;
        public Image unlockedIcon;
        public Image loadIcon;
        public Image resyncIcon;
        public Image repeatIcon;
        public Image repeatOneIcon;
        public Image shuffleIcon;
        public Image infoIcon;
        public Image nextIcon;
        public Image prevIcon;
        public Image playlistIcon;
        public Image masterIcon;
        public Image whitelistIcon;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public Slider volumeSlider;

        public Slider progressSlider;
        public Slider syncSlider;
        public Text statusText;
        public Text urlText;
        public Text placeholderText;
        public Text modeText;
        public Text queuedText;

        public Text playlistText;

        public OptionsUI optionsPanel; 
        public GameObject playlistPanel;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        string statusOverride = null;
        [NonSerialized]
        public string instanceMaster = "";
        [NonSerialized]
        public string instanceOwner = "";

        bool loadActive = false;
        VRCUrl pendingSubmit;
        bool pendingFromLoadOverride = false;

        VRCPlayerApi[] _playerBuffer = new VRCPlayerApi[100];

        void Start()
        {
            if (!videoPlayer)
            {
                Debug.LogError("[VideoTXL:PlayerControls] Video player reference missing");
                return;
            }

            videoPlayer._EnsureInit();

            if (optionsPanel)
                optionsPanel._SetControls(this);

            _PopulateMissingReferences();

            if (Utilities.IsValid(colorProfile))
            {
                normalColor = colorProfile.normalColor;
                disabledColor = colorProfile.disabledColor;
                activeColor = colorProfile.activeColor;
                attentionColor = colorProfile.attentionColor;
            }

            infoIcon.color = normalColor;
            _DisableAllVideoControls();

            queuedText.text = "";
            playlistText.text = "";

            if (Utilities.IsValid(audioManager))
                audioManager._RegisterControls(this);

            if (Utilities.IsValid(videoPlayer))
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_VideoStateUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_LOCK_UPDATE, this, "_VideoLockUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, "_VideoTrackingUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, "_OnVideoInfoUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_PLAYLIST_UPDATE, this, "_VideoPlaylistUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_READY, this, "_VideoStateUpdate");

                unlockedIcon.color = normalColor;

                if (Utilities.IsValid(videoPlayer.accessControl))
                    videoPlayer.accessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_ValidateAccess));
            }

            if (Utilities.IsValid(playlistPanel))
            {
                PlaylistUI pui = (PlaylistUI)playlistPanel.GetComponent(typeof(UdonBehaviour));
                if (Utilities.IsValid(pui))
                    pui._InitFromPlaylist((Playlist)videoPlayer.urlSource);
            }

#if !UNITY_EDITOR
            instanceMaster = Networking.GetOwner(gameObject).displayName;
            _FindOwners();
            SendCustomEventDelayedFrames("_RefreshPlayerAccessIcon", 1);
#endif

            _UpdateAll();
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
            if (repeatOneIcon)
            {
                repeatOneIcon.color = disabledColor;
                repeatOneIcon.enabled = false;
            }
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
            //if (Utilities.IsValid(videoPlayer.playlist))
            //    videoPlayer.playlist._SetEnabled(false);
            loadActive = false;
            _UpdateAll();
        }

        public void _HandleUrlInputClick()
        {
            if (!videoPlayer._CanTakeControl())
                _SetStatusOverride(_MakeOwnerMessage(), 3);
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
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandlePause()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerPause();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
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
                _SetStatusOverride(_MakeOwnerMessage(), 3);
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
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleLock()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerLock();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleLoad()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (!videoPlayer._CanTakeControl())
            {
                _SetStatusOverride(_MakeOwnerMessage(), 3);
                return;
            }

            //if (videoPlayer.localPlayerState == PLAYER_STATE_ERROR)
            //    loadActive = false;
            //else
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
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        bool _draggingProgressSlider = false;
        bool _updatingProgressSlider = false;

        public void _HandleProgressBeginDrag()
        {
            _draggingProgressSlider = true;
            _UpdateTrackingDragging();
        }

        public void _HandleProgressEndDrag()
        {
            _draggingProgressSlider = false;
            _HandleProgressSliderChanged();
        }

        public void _HandleProgressSliderChanged()
        {
            if (_draggingProgressSlider || _updatingProgressSlider)
                return;

            if (float.IsInfinity(videoPlayer.trackDuration) || videoPlayer.trackDuration <= 0)
                return;

            float targetTime = videoPlayer.trackDuration * progressSlider.value;
            videoPlayer._SetTargetTime(targetTime);
        }

        public void _HandleSourceModeClick()
        {
            short mode = (short)(videoPlayer.playerSourceOverride + 1);
            if (mode > 2)
                mode = 0;

            _UpdateSource(mode);
        }

        public void _HandleSourceAuto()
        {
            _UpdateSource(VideoSource.VIDEO_SOURCE_NONE);
        }

        public void _HandleSourceStream()
        {
            _UpdateSource(VideoSource.VIDEO_SOURCE_AVPRO);
        }

        public void _HandleSourceVideo()
        {
            _UpdateSource(VideoSource.VIDEO_SOURCE_UNITY);
        }

        void _UpdateSource(short mode)
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (!videoPlayer._CanTakeControl())
            {
                _SetStatusOverride(_MakeOwnerMessage(), 3);
                return;
            }

            videoPlayer._SetSourceMode(mode);
        }
        
        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager))
                audioManager._SetMasterMute(!audioManager.masterMute);
            //audioManager._ToggleMute();
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
            if (!Utilities.IsValid(videoPlayer) || !Utilities.IsValid(videoPlayer.urlSource))
                return;


            // Toggle panel if present
            if (Utilities.IsValid(playlistPanel))
            {
                playlistPanel.SetActive(!playlistPanel.activeSelf);
                playlistIcon.color = playlistPanel.activeSelf ? activeColor : normalColor;

                SendCustomEventDelayedFrames("_ScrollPlaylistCurrent", 10);

                return;
            }

            // If no panel, legacy behavior of re-enabling playlist at current track
            /*videoPlayer.urlSource._SetEnabled(true);
            if (!videoPlayer.playlist.PlaylistEnabled)
                return;

            if (videoPlayer.playlist.holdOnReady)
                videoPlayer._HoldNextVideo();

            videoPlayer._ChangeUrl(videoPlayer.playlist._GetCurrentUrl());*/
        }

        public void _ScrollPlaylistCurrent()
        {
            PlaylistUI pui = (PlaylistUI)playlistPanel.GetComponent(typeof(UdonBehaviour));
            if (Utilities.IsValid(pui))
                pui._ScrollToCurrentTrack();
        }

        public void _HandlePlaylistNext()
        {
            if (!Utilities.IsValid(videoPlayer) || !Utilities.IsValid(videoPlayer.urlSource))
                return;

            videoPlayer.urlSource._MoveNext();
        }

        public void _HandlePlaylistPrev()
        {
            if (!Utilities.IsValid(videoPlayer) || !Utilities.IsValid(videoPlayer.urlSource))
                return;

            videoPlayer.urlSource._MovePrev();
        }

        public void _SetStatusOverride(string msg, float timeout)
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
            int playerState = videoPlayer.playerState;
            if (!_draggingProgressSlider || playerState != TXLVideoPlayer.VIDEO_STATE_PLAYING || loadActive || !videoPlayer.seekableSource)
                return;

            string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
            string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration * progressSlider.value).ToString(@"hh\:mm\:ss");
            SetStatusText(positionStr + " / " + durationStr);
            progressSliderControl.SetActive(true);
            syncSliderControl.SetActive(false);

            SendCustomEventDelayedSeconds("_UpdateTrackingDragging", 0.1f);
        }

        public void _UpdateTracking()
        {
            int playerState = videoPlayer.playerState;
            if (playerState != TXLVideoPlayer.VIDEO_STATE_PLAYING || loadActive)
                return;

            if (!videoPlayer.seekableSource)
            {
                SetStatusText("Streaming...");
                progressSliderControl.SetActive(false);
                syncSliderControl.SetActive(false);
            }
            else if (!_draggingProgressSlider)
            {
                if (videoPlayer.trackTarget - videoPlayer.trackPosition > 1)
                {
                    SetStatusText("Synchronizing...");
                    progressSliderControl.SetActive(false);
                    syncSliderControl.SetActive(true);
                    syncSlider.value = videoPlayer.trackPosition / videoPlayer.trackTarget;
                }
                else
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackPosition).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + " / " + durationStr);
                    progressSliderControl.SetActive(true);
                    syncSliderControl.SetActive(false);

                    _updatingProgressSlider = true;
                    progressSlider.value = (videoPlayer.trackDuration <= 0) ? 0f : Mathf.Clamp01(videoPlayer.trackPosition / videoPlayer.trackDuration);
                    _updatingProgressSlider = false;
                }
            }
        }

        public void _UpdateInfo()
        {
            string queuedUrl = videoPlayer.queuedUrl.Get();
            queuedText.text = (queuedUrl != "") ? "QUEUED" : "";
        }

        // TODO: Genericize to url source
        public void _UpdatePlaylistInfo()
        {
            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            TXLRepeatMode repeatMode = videoPlayer.RepeatMode;
            if (repeatMode == TXLRepeatMode.None)
            {
                repeatIcon.enabled = true;
                repeatIcon.color = normalColor;
                if (repeatOneIcon)
                    repeatOneIcon.enabled = false;
            } else if (repeatMode == TXLRepeatMode.All)
            {
                repeatIcon.enabled = true;
                repeatIcon.color = activeColor;
                if (repeatOneIcon)
                    repeatOneIcon.enabled = false;
            } else if (repeatMode == TXLRepeatMode.Single)
            {
                repeatIcon.enabled = false;
                if (repeatOneIcon)
                {
                    repeatOneIcon.enabled = true;
                    repeatOneIcon.color = activeColor;
                }
            }

            Playlist playlist = (Playlist)videoPlayer.urlSource;

            if (Utilities.IsValid(playlist) && playlist.trackCount > 0)
            {
                nextIcon.color = (enableControl && playlist.PlaylistEnabled && playlist._CanMoveNext()) ? normalColor : disabledColor;
                prevIcon.color = (enableControl && playlist.PlaylistEnabled && playlist._CanMovePrev()) ? normalColor : disabledColor;
                playlistIcon.color = enableControl ? normalColor : disabledColor;

                bool playlistActive = playlist.PlaylistEnabled && playlist.CurrentIndex >= 0 && playlist.trackCount > 0;
                if (!playlistActive)
                    playlistText.text = "";
                else if (playlist.trackCatalogMode)
                    playlistText.text = $"TRACK: {playlist.CurrentIndex + 1}";
                else
                    playlistText.text = $"TRACK: {playlist.CurrentIndex + 1} / {playlist.trackCount}";
            }
            else
            {
                nextIcon.color = disabledColor;
                prevIcon.color = disabledColor;
                playlistIcon.color = disabledColor;
                playlistText.text = "";
            }

            if (Utilities.IsValid(playlist) && Utilities.IsValid(playlistPanel))
                playlistIcon.color = playlistPanel.activeSelf ? activeColor : normalColor;
        }

        public void _UpdateAll()
        {
            if (!videoPlayer)
            {
                SetPlaceholderText("Invalid video player controls setup");
                return;
            } else if (!videoPlayer.VideoManager)
            {
                SetPlaceholderText("Invalid video player setup");
                return;
            }

            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            int playerState = videoPlayer.playerState;

            if (enableControl && loadActive)
            {
                loadIcon.color = activeColor;
                urlInputControl.SetActive(true);
                urlInput.readOnly = !canControl;
                SetPlaceholderText("Enter Video URL...");
                SetStatusText("");
            }
            else
                loadIcon.color = enableControl ? normalColor : disabledColor;

            if (playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING && !loadActive)
            {
                urlInput.readOnly = true;
                urlInputControl.SetActive(false);

                stopIcon.color = enableControl ? normalColor : disabledColor;
                //loadIcon.color = enableControl ? normalColor : disabledColor;
                resyncIcon.color = normalColor;

                if (videoPlayer.paused)
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
                //loadIcon.color = disabledColor;
                progressSliderControl.SetActive(false);
                syncSliderControl.SetActive(false);
                urlInputControl.SetActive(true);

                if (playerState == TXLVideoPlayer.VIDEO_STATE_LOADING)
                {
                    stopIcon.color = enableControl ? normalColor : disabledColor;
                    //loadIcon.color = enableControl ? normalColor : disabledColor;
                    resyncIcon.color = normalColor;
                    pauseIcon.color = disabledColor;

                    if (!loadActive)
                    {
                        SetPlaceholderText(videoPlayer.HoldVideos && videoPlayer._videoReady ? "Ready" : "Loading...");
                        urlInput.readOnly = true;
                        SetStatusText("");
                    }
                }
                else if (playerState == TXLVideoPlayer.VIDEO_STATE_ERROR)
                {
                    stopIcon.color = videoPlayer.retryOnError ? normalColor : disabledColor;
                    //loadIcon.color = normalColor;
                    resyncIcon.color = normalColor;
                    pauseIcon.color = disabledColor;
                    //loadActive = false;

                    if (!loadActive)
                    {
                        VideoManager manager = videoPlayer.VideoManager;
                        switch (manager.LastErrorClass)
                        {
                            case VideoErrorClass.VRChat:
                                switch (manager.LastError)
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
                                break;
                            case VideoErrorClass.TXL:
                                switch (manager.LastErrorTXL)
                                {
                                    case VideoErrorTXL.NoAVProInEditor:
                                        SetPlaceholderText("AVPro (stream) not supported in simulator");
                                        break;
                                    case VideoErrorTXL.Unknown:
                                        SetPlaceholderText("Unknown error (TXL)");
                                        break;
                                }
                                break;
                        }

                        if (videoPlayer.streamFallback)
                            SetPlaceholderText("Retrying as stream source");

                        urlInput.readOnly = !canControl;
                        SetStatusText("");
                    }
                }
                else if (playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING || playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                {
                    if (playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                    {
                        //loadActive = false;
                        pendingFromLoadOverride = false;
                        stopIcon.color = disabledColor;
                        //loadIcon.color = disabledColor;
                        resyncIcon.color = disabledColor;
                        pauseIcon.color = disabledColor;
                    }
                    else
                    {
                        stopIcon.color = enableControl ? normalColor : disabledColor;
                        //loadIcon.color = activeColor;
                        resyncIcon.color = normalColor;

                        if (videoPlayer.paused)
                            pauseIcon.color = activeColor;
                        else
                            pauseIcon.color = (enableControl && videoPlayer.seekableSource) ? normalColor : disabledColor;
                    }

                    if (!loadActive)
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
                            SetStatusText(_MakeOwnerMessage());
                        }
                    }
                }
            }

            lockedIcon.enabled = videoPlayer.locked;
            unlockedIcon.enabled = !videoPlayer.locked;
            if (videoPlayer.locked)
                lockedIcon.color = canControl ? normalColor : attentionColor;

            if (!videoPlayer.VideoManager.HasMultipleTypes)
            {
                modeText.text = "";
            }
            else
            {
                switch (videoPlayer.playerSourceOverride)
                {
                    case VideoSource.VIDEO_SOURCE_UNITY:
                        modeText.text = "VIDEO";
                        break;
                    case VideoSource.VIDEO_SOURCE_AVPRO:
                        modeText.text = "STREAM";
                        break;
                    case VideoSource.VIDEO_SOURCE_NONE:
                    default:
                        if (playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                            modeText.text = "AUTO";
                        else
                        {
                            switch (videoPlayer.playerSource)
                            {
                                case VideoSource.VIDEO_SOURCE_UNITY:
                                    modeText.text = "AUTO VIDEO";
                                    break;
                                case VideoSource.VIDEO_SOURCE_AVPRO:
                                    modeText.text = "AUTO STREAM";
                                    break;
                                case VideoSource.VIDEO_SOURCE_NONE:
                                default:
                                    modeText.text = "AUTO";
                                    break;
                            }
                        }
                        break;
                }
            }

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
            _playerBuffer = VRCPlayerApi.GetPlayers(_playerBuffer);

            foreach (VRCPlayerApi player in _playerBuffer)
            {
                if (!Utilities.IsValid(player) || !player.IsValid())
                    continue;
                if (player.isInstanceOwner)
                    instanceOwner = player.displayName;
                if (player.isMaster)
                    instanceMaster = player.displayName;
            }
        }

        public string _MakeOwnerMessage()
        {
            if (videoPlayer.accessControl)
                return $"Controls locked by access control";
            if (instanceMaster == instanceOwner || instanceOwner == "")
                return $"Controls locked to master {instanceMaster}";

            return $"Controls locked to master {instanceMaster} and owner {instanceOwner}";
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _FindOwners();
            _RefreshPlayerAccessIcon();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _FindOwners();
            _RefreshPlayerAccessIcon();
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

        public void _ValidateAccess()
        {
            _RefreshPlayerAccessIcon();
            _UpdateAll();
        }

        public void _RefreshPlayerAccessIcon()
        {
            masterIcon.enabled = false;
            whitelistIcon.enabled = false;

            if (!Utilities.IsValid(videoPlayer.accessControl))
            {
                masterIcon.enabled = videoPlayer._IsAdmin();
                return;
            }

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return;

            AccessControl acl = videoPlayer.accessControl;
            if (acl.allowInstanceOwner && player.isInstanceOwner)
                masterIcon.enabled = true;
            else if (acl.allowMaster && player.isMaster)
                masterIcon.enabled = true;
            else if (acl.allowWhitelist && acl._LocalWhitelisted())
                whitelistIcon.enabled = true;
        }

        void _PopulateMissingReferences()
        {
            if (!Utilities.IsValid(videoPlayer))
            {
                videoPlayer = transform.parent.GetComponent<SyncPlayer>();
                if (Utilities.IsValid(videoPlayer))
                    videoPlayer.debugLog._Write("PlayerControls", $"Missing syncplayer reference, found one on parent");
                else
                    Debug.LogError("Missing syncplayer reference, also could not find one on parent!");
            }

            // Volume

            if (!Utilities.IsValid(volumeSliderControl))
                volumeSliderControl = _FindGameObject("MainPanel/UpperRow/VolumeGroup/Slider");
            if (!Utilities.IsValid(volumeSlider))
                volumeSlider = (Slider)_FindComponent("MainPanel/UpperRow/VolumeGroup/Slider", typeof(Slider));
            if (!Utilities.IsValid(muteToggleOn))
                muteToggleOn = _FindGameObject("MainPanel/UpperRow/VolumeGroup/MuteButton/IconMuted");
            if (!Utilities.IsValid(muteToggleOff))
                muteToggleOff = _FindGameObject("MainPanel/UpperRow/VolumeGroup/MuteButton/IconVolume");

            // Icons

            if (!Utilities.IsValid(stopIcon))
                stopIcon = (Image)_FindComponent("MainPanel/UpperRow/ControlGroup/StopButton/IconStop", typeof(Image));
            if (!Utilities.IsValid(pauseIcon))
                pauseIcon = (Image)_FindComponent("MainPanel/UpperRow/ControlGroup/PauseButton/IconPause", typeof(Image));
            if (!Utilities.IsValid(lockedIcon))
                lockedIcon = (Image)_FindComponent("MainPanel/LowerRow/InputProgress/MasterLockButton/IconLocked", typeof(Image));
            if (!Utilities.IsValid(unlockedIcon))
                unlockedIcon = (Image)_FindComponent("MainPanel/LowerRow/InputProgress/MasterLockButton/IconUnlocked", typeof(Image));
            if (!Utilities.IsValid(loadIcon))
                loadIcon = (Image)_FindComponent("MainPanel/LowerRow/InputProgress/LoadButton/IconLoad", typeof(Image));
            if (!Utilities.IsValid(resyncIcon))
                resyncIcon = (Image)_FindComponent("MainPanel/UpperRow/SyncGroup/ResyncButton/IconResync", typeof(Image));
            if (!Utilities.IsValid(repeatIcon))
                repeatIcon = (Image)_FindComponent("MainPanel/UpperRow/ButtonGroup/RepeatButton/IconRepeat", typeof(Image));
            if (!Utilities.IsValid(repeatOneIcon))
                repeatIcon = (Image)_FindComponent("MainPanel/UpperRow/ButtonGroup/RepeatButton/IconRepeatOne", typeof(Image));
            if (!Utilities.IsValid(playlistIcon))
                playlistIcon = (Image)_FindComponent("MainPanel/UpperRow/ButtonGroup/PlaylistButton/IconPlaylist", typeof(Image));
            if (!Utilities.IsValid(infoIcon))
                infoIcon = (Image)_FindComponent("MainPanel/UpperRow/ButtonGroup/InfoButton/IconInfo", typeof(Image));
            if (!Utilities.IsValid(nextIcon))
                nextIcon = (Image)_FindComponent("MainPanel/UpperRow/ControlGroup/NextButton/IconNext", typeof(Image));
            if (!Utilities.IsValid(prevIcon))
                prevIcon = (Image)_FindComponent("MainPanel/UpperRow/ControlGroup/PrevButton/IconPrev", typeof(Image));
            if (!Utilities.IsValid(masterIcon))
                masterIcon = (Image)_FindComponent("MainPanel/LowerRow/InputProgress/PlayerAccess/IconMaster", typeof(Image));
            if (!Utilities.IsValid(whitelistIcon))
                whitelistIcon = (Image)_FindComponent("MainPanel/LowerRow/InputProgress/PlayerAccess/IconWhitelist", typeof(Image));

            // Super Bar

            if (!Utilities.IsValid(progressSliderControl))
                progressSliderControl = _FindGameObject("MainPanel/LowerRow/InputProgress/TrackingSlider");
            if (!Utilities.IsValid(progressSlider))
                progressSlider = (Slider)_FindComponent("MainPanel/LowerRow/InputProgress/TrackingSlider", typeof(Slider));
            if (!Utilities.IsValid(syncSliderControl))
                syncSliderControl = _FindGameObject("MainPanel/LowerRow/InputProgress/SyncSlider");
            if (!Utilities.IsValid(syncSlider))
                syncSlider = (Slider)_FindComponent("MainPanel/LowerRow/InputProgress/SyncSlider", typeof(Slider));
            if (!Utilities.IsValid(urlInputControl))
                urlInputControl = _FindGameObject("MainPanel/LowerRow/InputProgress/InputField");
            if (!Utilities.IsValid(urlInput))
                urlInput = (VRCUrlInputField)_FindComponent("MainPanel/LowerRow/InputProgress/InputField", typeof(VRCUrlInputField));
            if (!Utilities.IsValid(urlText))
                urlText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/InputField/TextMask/Text", typeof(Text));
            if (!Utilities.IsValid(statusText))
                statusText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/StatusText", typeof(Text));
            if (!Utilities.IsValid(placeholderText))
                placeholderText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/InputField/TextMask/Placeholder", typeof(Text));
            if (!Utilities.IsValid(modeText))
                modeText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/SourceMode", typeof(Text));
            if (!Utilities.IsValid(queuedText))
                queuedText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/QueuedText", typeof(Text));
            if (!Utilities.IsValid(playlistText))
                playlistText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/PlaylistText", typeof(Text));

            if (!Utilities.IsValid(urlInput) && Utilities.IsValid(videoPlayer.debugLog))
                videoPlayer.debugLog._Write("PlayerControls", "Could not resolve URL input component: is your VRC SDK missing VRCUrlInput?");
        }

        GameObject _FindGameObject(string path)
        {
            if (Utilities.IsValid(videoPlayer) && Utilities.IsValid(videoPlayer.debugLog))
                videoPlayer.debugLog._Write("PlayerControls", $"Missing UI Game Object {path}");

            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.gameObject;
        }

        Component _FindComponent(string path, System.Type type)
        {
            if (Utilities.IsValid(videoPlayer) && Utilities.IsValid(videoPlayer.debugLog))
                videoPlayer.debugLog._Write("PlayerControls", $"Missing UI Component {path}:{type}");

            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.GetComponent(type);
        }
    }
}
