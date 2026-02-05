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
    public enum UrlEntryMode
    {
        LoadUrl,
        AddToQueue,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControls : UdonSharpBehaviour
    {
        public SyncPlayer videoPlayer;
        [Obsolete("AudioManager will be taken from the bound video player instead")]
        public AudioManager audioManager;
        public ControlColorProfile colorProfile;

        [Tooltip("Which URL entry mode the controls object will start in")]
        public UrlEntryMode defaultUrlMode;
        [Tooltip("Whether the control will remember the user's URL entry mode after inputting a URL, or revert to the default")]
        public bool rememberUrlMode;

        public VRCUrlInputField urlInput;

        public GameObject volumeSliderControl;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;
        public GameObject syncSliderControl;
        public GameObject queueInputControl;

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
        public Image queueIcon;

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
        public Text titleText;
        public Text offsetText;

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

        bool initialized = false;
        bool loadActive = false;
        VRCUrl pendingSubmit;
        bool pendingFromLoadOverride = false;
        UrlEntryMode urlMode;
        //bool addToQueue = false;
        bool sourcePanelOpen = false;

        VRCPlayerApi[] _playerBuffer = new VRCPlayerApi[100];

        [NonSerialized]
        public VRCUrl internalArgUrl;

        void Start()
        {
            if (!videoPlayer)
            {
                Debug.LogError("[VideoTXL:PlayerControls] Video player reference missing");
                return;
            }

            videoPlayer._EnsureInit();

            urlMode = defaultUrlMode;

            if (optionsPanel)
                optionsPanel._SetControls(this);

            _PopulateMissingReferences();

            if (colorProfile)
            {
                normalColor = colorProfile.normalColor;
                disabledColor = colorProfile.disabledColor;
                activeColor = colorProfile.activeColor;
                attentionColor = colorProfile.attentionColor;
            }

            _SetIconColor(infoIcon, normalColor);
            _DisableAllVideoControls();

            _SetText(queuedText, "");
            _SetText(playlistText, "");
            _SetText(offsetText, "");

            if (videoPlayer)
            {
                if (gameObject.activeInHierarchy)
                    _RegisterVideoListeners();

                if (playlistPanel && videoPlayer.SourceManager)
                {
                    VideoSourceUI vui = playlistPanel.GetComponentInChildren<VideoSourceUI>();
                    if (vui)
                        vui._BindSourceManager(videoPlayer.sourceManager);
                }

                _SetIconColor(unlockedIcon, normalColor);
            }

#if !UNITY_EDITOR
            instanceMaster = Networking.GetOwner(gameObject).displayName;
            _FindOwners();
            SendCustomEventDelayedFrames("_RefreshPlayerAccessIcon", 1);
#endif

            initialized = true;

            _UpdateAll();
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            _RegisterVideoListeners();

            _RefreshPlayerAccessIcon();
            _UpdateAll();
        }

        private void OnDisable()
        {
            _UnregisterVideoListeners();
        }

        public void _BindVideoPlayer(SyncPlayer videoPlayer)
        {
            _UnregisterVideoListeners();

            this.videoPlayer = videoPlayer;

            if (gameObject.activeInHierarchy)
                _RegisterVideoListeners();

            if (playlistPanel && videoPlayer.SourceManager)
            {
                VideoSourceUI vui = playlistPanel.GetComponentInChildren<VideoSourceUI>();
                if (vui)
                    vui._BindSourceManager(videoPlayer.sourceManager);
            }
        }

        void _RegisterVideoListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_BIND_AUDIOMANAGER, this, nameof(_InternalOnBindAudioManager));
                videoPlayer._Register(TXLVideoPlayer.EVENT_UNBIND_AUDIOMANAGER, this, nameof(_InternalOnUnbindAudioManager));

                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_VideoStateUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_LOCK_UPDATE, this, "_VideoLockUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, "_VideoTrackingUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, "_OnVideoInfoUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_PLAYLIST_UPDATE, this, "_VideoPlaylistUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_READY, this, "_VideoStateUpdate");

                _RegisterAudioManagerListeners();

                if (videoPlayer.accessControl)
                    videoPlayer.accessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_ValidateAccess));

                if (videoPlayer.urlInfoResolver)
                    videoPlayer.urlInfoResolver._Register(UrlInfoResolver.EVENT_URL_INFO, this, nameof(_OnUrlInfoReady), nameof(internalArgUrl));
            }
        }

        void _UnregisterVideoListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_BIND_AUDIOMANAGER, this, nameof(_InternalOnBindAudioManager));
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_UNBIND_AUDIOMANAGER, this, nameof(_InternalOnUnbindAudioManager));

                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_VideoStateUpdate");
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_LOCK_UPDATE, this, "_VideoLockUpdate");
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, "_VideoTrackingUpdate");
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, "_OnVideoInfoUpdate");
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_PLAYLIST_UPDATE, this, "_VideoPlaylistUpdate");
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_READY, this, "_VideoStateUpdate");

                _UnregisterAudioManagerListeners();

                if (videoPlayer.accessControl)
                    videoPlayer.accessControl._Unregister(AccessControl.EVENT_VALIDATE, this, nameof(_ValidateAccess));

                if (videoPlayer.urlInfoResolver)
                    videoPlayer.urlInfoResolver._Unregister(UrlInfoResolver.EVENT_URL_INFO, this, nameof(_OnUrlInfoReady));

                if (playlistPanel && videoPlayer.SourceManager)
                {
                    VideoSourceUI vui = playlistPanel.GetComponentInChildren<VideoSourceUI>();
                    if (vui)
                        vui._BindSourceManager(null);
                }
            }
        }

        void _DisableAllVideoControls()
        {
            _SetIconColor(stopIcon, disabledColor);
            _SetIconColor(pauseIcon, disabledColor);
            _SetIconColor(lockedIcon, disabledColor);
            _SetIconColor(unlockedIcon, disabledColor);
            _SetIconColor(loadIcon, disabledColor);
            _SetIconColor(resyncIcon, disabledColor);
            _SetIconColor(repeatIcon, disabledColor);
            _SetIconColor(repeatOneIcon, disabledColor);
            _SetIconColor(nextIcon, disabledColor);
            _SetIconColor(prevIcon, disabledColor);
            _SetIconColor(playlistIcon, disabledColor);
            _SetIconColor(queueIcon, disabledColor);

            _SetIconEnabled(repeatOneIcon, false);
        }

        public void _InternalOnUnbindAudioManager()
        {
            _UnregisterAudioManagerListeners();
        }

        public void _InternalOnBindAudioManager()
        {
            if (gameObject.activeInHierarchy)
                _RegisterAudioManagerListeners();
        }

        /*public void _BindAudioManager(AudioManager audioManager)
        {
            _UnregisterAudioManagerListeners();

            this.audioManager = audioManager;
            _RegisterAudioManagerListeners();
        }*/

        void _RegisterAudioManagerListeners()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager)
            {
                manager._Register(AudioManager.EVENT_MASTER_VOLUME_UPDATE, this, nameof(_InternalOnMasterVolumeUpdate));
                manager._Register(AudioManager.EVENT_MASTER_MUTE_UPDATE, this, nameof(_InternalOnMasterMuteUpdate));

                _InternalOnMasterVolumeUpdate();
                _InternalOnMasterMuteUpdate();
            }
        }

        void _UnregisterAudioManagerListeners()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager)
            {
                manager._Unregister(AudioManager.EVENT_MASTER_VOLUME_UPDATE, this, nameof(_InternalOnMasterVolumeUpdate));
                manager._Unregister(AudioManager.EVENT_MASTER_MUTE_UPDATE, this, nameof(_InternalOnMasterMuteUpdate));
            }
        }

        public void _InternalOnMasterVolumeUpdate()
        {
            if (volumeSlider)
                volumeSlider.SetValueWithoutNotify(videoPlayer.AudioManager.masterVolume);
        }

        public void _InternalOnMasterMuteUpdate()
        {
            _SetActive(muteToggleOn, videoPlayer.AudioManager.masterMute);
            _SetActive(muteToggleOff, !videoPlayer.AudioManager.masterMute);
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

        public void _OnUrlInfoReady()
        {
            _UpdateInfo();
        }

        public void _HandleUrlInput()
        {
            if (!videoPlayer || !urlInput)
                return;

            pendingFromLoadOverride = loadActive;
            pendingSubmit = urlInput.GetUrl();

            SendCustomEventDelayedSeconds("_HandleUrlInputDelay", 0.5f);
        }

        public void _HandleUrlInputDelay()
        {
            if (!videoPlayer || !urlInput)
                return;

            VRCUrl url = urlInput.GetUrl();
            urlInput.SetUrl(VRCUrl.Empty);

            _SetText(urlText, "");

            // Hack to get around Unity always firing OnEndEdit event for submit and lost focus
            // If loading override was on, but it's off immediately after submit, assume user closed override
            // instead of submitting.  Half second delay is a crude defense against a UI race.
            if (pendingFromLoadOverride && !loadActive)
                return;

            bool loadOnQueue = urlMode == UrlEntryMode.AddToQueue;
            VideoUrlSource addSource = null;

            if (!videoPlayer.sourceManager)
                loadOnQueue = false;
            else
                addSource = videoPlayer.sourceManager._GetSource(videoPlayer.sourceManager._GetCanAddTrack());

            if (!addSource)
                loadOnQueue = false;

            if (loadOnQueue)
                addSource._AddTrack(url);
            else
                videoPlayer._ChangeUrl(url);

            //if (Utilities.IsValid(videoPlayer.playlist))
            //    videoPlayer.playlist._SetEnabled(false);
            loadActive = false;

            if (!rememberUrlMode)
                urlMode = defaultUrlMode;

            _UpdateAll();
        }

        public void _HandleUrlInputClick()
        {
            if (!videoPlayer)
                return;

            if (!videoPlayer._CanTakeControl())
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleUrlInputChange()
        {
            /*Debug.Log("_HandleURLInputChange");
            if (!Utilities.IsValid(videoPlayer))
                return;

            VRCUrl url = urlInput.GetUrl();
            if (url.Get().Length > 0)
                videoPlayer._UpdateQueuedUrl(urlInput.GetUrl());

            addToQueue = false;*/
        }

        public void _HandleStop()
        {
            if (!videoPlayer)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerStop();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandlePause()
        {
            if (!videoPlayer)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerPause();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleResync()
        {
            if (!videoPlayer)
                return;

            videoPlayer._Resync();
        }

        public void _HandlePlayCurrent()
        {
            if (!videoPlayer)
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
            if (!videoPlayer)
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
            if (!videoPlayer)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerLock();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleLoad()
        {
            if (!videoPlayer)
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
            if (!videoPlayer)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerRepeatMode();
            else
                _SetStatusOverride(_MakeOwnerMessage(), 3);
        }

        public void _HandleQueueToggle()
        {
            if (!videoPlayer)
                return;

            if (urlMode == UrlEntryMode.AddToQueue)
                urlMode = UrlEntryMode.LoadUrl;
            else if (urlMode == UrlEntryMode.LoadUrl)
                urlMode = UrlEntryMode.AddToQueue;

            _UpdateAll();
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
            if (!videoPlayer || !progressSlider)
                return;

            if (_draggingProgressSlider || _updatingProgressSlider)
                return;

            if (float.IsInfinity(videoPlayer.trackDuration) || videoPlayer.trackDuration <= 0)
                return;

            float targetTime = videoPlayer.trackDuration * progressSlider.value;
            videoPlayer._SetTargetTime(targetTime);
        }

        public void _HandleSourceModeClick()
        {
            if (!videoPlayer)
                return;

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
            if (!videoPlayer)
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
            if (videoPlayer && videoPlayer.AudioManager)
                videoPlayer.AudioManager._SetMasterMute(!videoPlayer.AudioManager.masterMute);
        }

        public void _UpdateVolumeSlider()
        {
            if (videoPlayer && videoPlayer.AudioManager && volumeSlider)
                videoPlayer.AudioManager._SetMasterVolume(volumeSlider.value);
        }

        public void _HandlePlaylist()
        {
            if (!videoPlayer || !videoPlayer.sourceManager)
                return;


            // Toggle panel if present
            if (playlistPanel)
            {
                sourcePanelOpen = !sourcePanelOpen;

                int ccount = playlistPanel.transform.childCount;
                for (int i = 0; i < ccount; i++)
                    playlistPanel.transform.GetChild(i).gameObject.SetActive(sourcePanelOpen);

                if (sourcePanelOpen)
                {
                    VideoSourceUI ui = playlistPanel.GetComponentInChildren<VideoSourceUI>();
                    if (ui)
                        ui._SelectActive();
                }

                _SetIconColor(playlistIcon, sourcePanelOpen ? activeColor : normalColor);

                //SendCustomEventDelayedFrames("_ScrollPlaylistCurrent", 10);

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

        /*public void _ScrollPlaylistCurrent()
        {
            PlaylistUI pui = (PlaylistUI)playlistPanel.GetComponent(typeof(UdonBehaviour));
            if (Utilities.IsValid(pui))
                pui._ScrollToCurrentTrack();
        }*/

        public void _HandlePlaylistNext()
        {
            if (!videoPlayer || !videoPlayer.sourceManager)
                return;

            videoPlayer.sourceManager._MoveNext();
        }

        public void _HandlePlaylistPrev()
        {
            if (!videoPlayer || !videoPlayer.sourceManager)
                return;

            videoPlayer.sourceManager._MovePrev();
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
            if (!videoPlayer)
                return;

            int playerState = videoPlayer.playerState;
            if (!_draggingProgressSlider || playerState != TXLVideoPlayer.VIDEO_STATE_PLAYING || loadActive || !videoPlayer.seekableSource)
                return;

            string durationStr = TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
            float positionSeconds = videoPlayer.trackDuration * (progressSlider ? progressSlider.value : 0f);
            string positionStr = TimeSpan.FromSeconds(positionSeconds).ToString(@"hh\:mm\:ss");
            SetStatusText(positionStr + " / " + durationStr);

            _SetActive(progressSliderControl, true);
            _SetActive(syncSliderControl, false);

            SendCustomEventDelayedSeconds("_UpdateTrackingDragging", 0.1f);
        }

        public void _UpdateTracking()
        {
            if (!videoPlayer)
                return;

            int playerState = videoPlayer.playerState;
            if (playerState != TXLVideoPlayer.VIDEO_STATE_PLAYING || loadActive)
                return;

            if (!videoPlayer.seekableSource)
            {
                SetStatusText("Streaming...");
                _SetActive(progressSliderControl, false);
                _SetActive(syncSliderControl, true);
            }
            else if (!_draggingProgressSlider)
            {
                if (videoPlayer.trackTarget - videoPlayer.trackPosition > 1)
                {
                    SetStatusText("Synchronizing...");
                    _SetActive(progressSliderControl, false);
                    _SetActive(syncSliderControl, true);

                    if (syncSlider && videoPlayer.trackTarget > 0)
                        syncSlider.value = videoPlayer.trackPosition / videoPlayer.trackTarget;
                }
                else
                {
                    string durationStr = TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = TimeSpan.FromSeconds(videoPlayer.trackPosition).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + " / " + durationStr);

                    _SetActive(progressSliderControl, true);
                    _SetActive(syncSliderControl, false);

                    if (progressSlider)
                    {
                        _updatingProgressSlider = true;
                        progressSlider.value = (videoPlayer.trackDuration <= 0)
                            ? 0f
                            : Mathf.Clamp01(videoPlayer.trackPosition / videoPlayer.trackDuration);
                        _updatingProgressSlider = false;
                    }
                }
            }
        }

        public void _UpdateInfo()
        {
            _SetText(titleText, _GetTitleText());

            //string queuedUrl = videoPlayer.queuedUrl.Get();
            //queuedText.text = (queuedUrl != "") ? "QUEUED" : "";
        }

        string _GetTitleText()
        {
            if (!videoPlayer)
                return "";

            if (videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                return "";
            if (videoPlayer.urlInfoResolver)
                return videoPlayer.urlInfoResolver._GetFormatted(videoPlayer.currentUrl);

            return "";
        }

        bool _IsQueueSupported()
        {
            if (!videoPlayer)
                return false;

            if (videoPlayer.sourceManager)
                return videoPlayer.sourceManager._GetCanAddTrack() >= 0;

            return false;
        }

        // TODO: Genericize to url source
        public void _UpdatePlaylistInfo()
        {
            if (!videoPlayer)
                return;

            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            TXLRepeatMode repeatMode = videoPlayer.RepeatMode;
            if (repeatMode == TXLRepeatMode.None)
            {
                _SetIconEnabled(repeatIcon, true);
                _SetIconEnabled(repeatOneIcon, false);
                _SetIconColor(repeatIcon, normalColor);
            }
            else if (repeatMode == TXLRepeatMode.All)
            {
                _SetIconEnabled(repeatIcon, true);
                _SetIconEnabled(repeatOneIcon, false);
                _SetIconColor(repeatIcon, activeColor);
            }
            else if (repeatMode == TXLRepeatMode.Single)
            {
                _SetIconEnabled(repeatIcon, false);
                _SetIconEnabled(repeatOneIcon, true);
                _SetIconColor(repeatOneIcon, activeColor);
            }

            SourceManager sourceManager = videoPlayer.SourceManager;

            _SetIconColor(nextIcon, disabledColor);
            _SetIconColor(prevIcon, disabledColor);
            _SetIconColor(playlistIcon, disabledColor);

            _SetText(playlistText, "");
            _SetText(queuedText, "");

            bool videoStopped = videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED;
            if (sourceManager && sourceManager.Count > 0)
            {
                _SetIconColor(nextIcon, (enableControl && sourceManager.CanMoveNext) ? normalColor : disabledColor);
                _SetIconColor(prevIcon, (enableControl && sourceManager.CanMovePrev) ? normalColor : disabledColor);
                _SetIconColor(playlistIcon, enableControl ? normalColor : disabledColor);

                VideoUrlSource source = videoPlayer.currentUrlSource;
                _SetText(queuedText, (source && !videoStopped) ? source.SourceName : "");
                _SetText(queuedText, (!source || videoStopped) ? "" : source.TrackDisplay);

                if (playlistPanel)
                    _SetIconColor(playlistIcon, sourcePanelOpen ? activeColor : normalColor);
            }
        }

        public void _UpdateAll()
        {
            if (!videoPlayer)
            {
                SetPlaceholderText("Invalid video player controls setup");
                return;
            }
            else if (!videoPlayer.VideoManager)
            {
                SetPlaceholderText("Invalid video player setup");
                return;
            }

            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            bool queueSupported = _IsQueueSupported();

            _SetActive(queueInputControl, false);

            int playerState = videoPlayer.playerState;

            if (enableControl && loadActive)
            {
                _SetIconColor(loadIcon, activeColor);

                _SetActive(urlInputControl, true);
                if (urlInput)
                    urlInput.readOnly = !canControl;
                SetStatusText("");

                _SetActive(queueInputControl, queueSupported);

                if (queueSupported && urlMode == UrlEntryMode.AddToQueue)
                {
                    _SetIconColor(queueIcon, activeColor);
                    SetPlaceholderText("Add Video URL to Queue...");
                }
                else
                {
                    _SetIconColor(queueIcon, normalColor);
                    SetPlaceholderText("Enter Video URL...");
                }
            }
            else
                _SetIconColor(loadIcon, enableControl ? normalColor : disabledColor);

            if (playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING && !loadActive)
            {
                if (urlInput)
                    urlInput.readOnly = true;
                _SetActive(urlInputControl, false);

                _SetIconColor(stopIcon, enableControl ? normalColor : disabledColor);
                _SetIconColor(resyncIcon, normalColor);

                if (videoPlayer.paused)
                    _SetIconColor(pauseIcon, activeColor);
                else
                    _SetIconColor(pauseIcon, (enableControl && videoPlayer.seekableSource) ? normalColor : disabledColor);

                if (progressSlider)
                    progressSlider.interactable = enableControl;

                _UpdateTracking();
            }
            else
            {
                _draggingProgressSlider = false;

                _SetIconColor(stopIcon, disabledColor);

                //loadIcon.color = disabledColor;
                _SetActive(progressSliderControl, false);
                _SetActive(syncSliderControl, false);
                _SetActive(urlInputControl, true);

                if (playerState == TXLVideoPlayer.VIDEO_STATE_LOADING)
                {
                    _SetIconColor(stopIcon, enableControl ? normalColor : disabledColor);
                    _SetIconColor(resyncIcon, normalColor);
                    _SetIconColor(pauseIcon, disabledColor);

                    if (!loadActive)
                    {
                        string loadStr = "Loading...";
                        VideoUrlSource source = videoPlayer.currentUrlSource;
                        if (source && source.SupportsRetry && source.RetryCount > 0)
                            loadStr = $"Loading... (retry {source.RetryCount} of {source.MaxRetryCount})";

                        SetPlaceholderText(videoPlayer.HoldVideos && videoPlayer._videoReady ? "Ready" : loadStr);
                        if (urlInput)
                            urlInput.readOnly = true;
                        SetStatusText("");
                    }
                }
                else if (playerState == TXLVideoPlayer.VIDEO_STATE_ERROR)
                {
                    _SetIconColor(stopIcon, videoPlayer.retryOnError ? normalColor : disabledColor);
                    _SetIconColor(resyncIcon, normalColor);
                    _SetIconColor(pauseIcon, disabledColor);

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
                            SetPlaceholderText("Video player error - Retrying as stream source");
                        else if (videoPlayer.videoFallback)
                            SetPlaceholderText("Video player error - Retrying as video source");

                        if (urlInput)
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

                        _SetIconColor(stopIcon, disabledColor);
                        _SetIconColor(resyncIcon, disabledColor);
                        _SetIconColor(pauseIcon, disabledColor);
                    }
                    else
                    {
                        _SetIconColor(stopIcon, enableControl ? normalColor : disabledColor);
                        _SetIconColor(resyncIcon, normalColor);

                        if (videoPlayer.paused)
                            _SetIconColor(pauseIcon, activeColor);
                        else
                            _SetIconColor(pauseIcon, (enableControl && videoPlayer.seekableSource) ? normalColor : disabledColor);
                    }

                    if (!loadActive)
                    {
                        if (urlInput)
                            urlInput.readOnly = !canControl;
                        if (canControl)
                        {
                            if (_IsQueueSupported() && urlMode == UrlEntryMode.AddToQueue)
                                SetPlaceholderText("Add Video URL to Queue...");
                            else
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

            _SetIconEnabled(lockedIcon, videoPlayer.locked);
            _SetIconEnabled(unlockedIcon, !videoPlayer.locked);

            if (videoPlayer.locked)
                _SetIconColor(lockedIcon, canControl ? normalColor : attentionColor);

            if (!videoPlayer.VideoManager.HasMultipleTypes)
                _SetText(modeText, "");
            else
            {
                switch (videoPlayer.playerSourceOverride)
                {
                    case VideoSource.VIDEO_SOURCE_UNITY:
                        _SetText(modeText, "VIDEO");
                        break;
                    case VideoSource.VIDEO_SOURCE_AVPRO:
                        _SetText(modeText, "STREAM");
                        break;
                    case VideoSource.VIDEO_SOURCE_NONE:
                    default:
                        if (playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                            _SetText(modeText, "AUTO");
                        else
                        {
                            switch (videoPlayer.playerSource)
                            {
                                case VideoSource.VIDEO_SOURCE_UNITY:
                                    _SetText(modeText, "AUTO VIDEO");
                                    break;
                                case VideoSource.VIDEO_SOURCE_AVPRO:
                                    _SetText(modeText, "AUTO STREAM");
                                    break;
                                case VideoSource.VIDEO_SOURCE_NONE:
                                default:
                                    _SetText(modeText, "AUTO");
                                    break;
                            }
                        }
                        break;
                }
            }

            if (videoPlayer.LocalOffset != 0)
                _SetText(offsetText, $"{(videoPlayer.LocalOffset > 0 ? '+' : '−')}{Mathf.Abs(videoPlayer.LocalOffset):F2}");
            else
                _SetText(offsetText, "");

            _UpdatePlaylistInfo();
            _UpdateInfo();
        }

        void SetStatusText(string msg)
        {
            _SetText(statusText, statusOverride != null ? statusOverride : msg);
        }

        void SetPlaceholderText(string msg)
        {
            _SetText(placeholderText, statusOverride != null ? "" : msg);
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
            if (videoPlayer && videoPlayer.accessControl)
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

        public void _ValidateAccess()
        {
            _RefreshPlayerAccessIcon();
            _UpdateAll();
        }

        public void _RefreshPlayerAccessIcon()
        {
            _SetIconEnabled(masterIcon, false);
            _SetIconEnabled(whitelistIcon, false);

            if (!videoPlayer || !videoPlayer.accessControl)
            {
                _SetIconEnabled(masterIcon, videoPlayer && videoPlayer._IsAdmin());
                return;
            }

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return;

            AccessControl acl = videoPlayer.accessControl;
            if (acl.allowInstanceOwner && player.isInstanceOwner)
                _SetIconEnabled(masterIcon, true);
            else if (acl.allowMaster && player.isMaster)
                _SetIconEnabled(masterIcon, true);
            else if (acl.allowWhitelist && acl._LocalWhitelisted())
                _SetIconEnabled(whitelistIcon, true);
        }

        void _SetIconColor (Image image, Color color)
        {
            if (image)
                image.color = color;
        }

        void _SetIconEnabled (Image image, bool enabled)
        {
            if (image)
                image.enabled = enabled;
        }

        void _SetText (Text text, string value)
        {
            if (text)
                text.text = value;
        }

        void _SetActive (GameObject obj, bool active)
        {
            if (obj)
                obj.SetActive(active);
        }

        void _PopulateMissingReferences()
        {
            if (!videoPlayer)
            {
                videoPlayer = transform.parent.GetComponent<SyncPlayer>();
                if (videoPlayer)
                    videoPlayer.debugLog._Write("PlayerControls", "Missing syncplayer reference, found one on parent");
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
                repeatOneIcon = (Image)_FindComponent("MainPanel/UpperRow/ButtonGroup/RepeatButton/IconRepeatOne", typeof(Image));
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
                urlInputControl = _FindGameObject("MainPanel/LowerRow/InputProgress/InputArea");
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
            if (!Utilities.IsValid(offsetText))
                offsetText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/OffsetText", typeof(Text));
            if (!Utilities.IsValid(playlistText))
                playlistText = (Text)_FindComponent("MainPanel/LowerRow/InputProgress/PlaylistText", typeof(Text));

            if (!Utilities.IsValid(urlInput) && Utilities.IsValid(videoPlayer) && Utilities.IsValid(videoPlayer.debugLog))
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
