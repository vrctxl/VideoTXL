
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalPlayer : TXLVideoPlayer
    {
        [Header("Optional Components")]
        [Tooltip("Set of input URLs to remap to alternate URLs on a per-platform basis")]
        public UrlRemapper urlRemapper;

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Header("Playback")]
        [Tooltip("Optional trigger zone the player must be in to sustain playback.  Disables playing audio on world load.")]
        public ZoneTrigger playbackZone;
        [Tooltip("Optional trigger zone that will start playback if player enters.")]
        public ZoneTrigger triggerZone;
        [Tooltip("Starts playback when player joins world.  Any defined playback zone will come into effect once the player enters it.")]
        public bool playOnJoin = false;

        [Header("Default Options")]
        public StaticUrlSource staticUrlSource;
        public VRCUrl streamUrl;
        [Tooltip("How content not matching a screen's aspect ratio should be fit by default.  Affects the output CRT and materials with the screen fit property mapped.")]
        public TXLScreenFit defaultScreenFit = TXLScreenFit.Fit;

        [Tooltip("Write out video player events to VRChat log")]
        public bool debugLogging = true;

        [Tooltip("Automatically loop track when finished")]
        public bool loop;

        [Tooltip("Remember where video was stopped and resume at that position when re-triggered")]
        public bool resumePosition;

        [Tooltip("Whether to keep playing the same URL if an error occurs")]
        public bool retryOnError = true;

        float retryTimeout = 6;

        const int SOURCE_TYPE_URL = 0;
        const int SOURCE_TYPE_STATIC = 1;

        bool _hasSustainZone = false;
        bool _inSustainZone = false;
        bool _triggerZoneSame = false;

        bool _isStreamPlayer;
        int _urlSourceType;

        float _lastVideoPosition = 0;

        VRCUrl playAtUrl;
        float playAt = 0;
        float playStartTime = 0;

        protected override void _Init()
        {
            base._Init();

            if (Utilities.IsValid(urlRemapper))
                urlRemapper._SetGameMode(IsQuest ? UrlRemapper.GAME_MODE_QUEST : UrlRemapper.GAME_MODE_PC);

            _hasSustainZone = Utilities.IsValid(playbackZone);
            if (_hasSustainZone)
            {
                _inSustainZone = playbackZone._LocalPlayerInZone();
                playbackZone._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_PlaybackZoneEnter");
                playbackZone._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, "_PlaybackZoneExit");
            }

            if (Utilities.IsValid(triggerZone))
            {
                if (_hasSustainZone && triggerZone == playbackZone)
                    _triggerZoneSame = true;
                else
                    triggerZone._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_TriggerPlay");
            }

            if (Utilities.IsValid(staticUrlSource))
            {
                _urlSourceType = SOURCE_TYPE_STATIC;
                staticUrlSource._RegisterPlayer((UdonBehaviour)GetComponent(typeof(UdonBehaviour)));
            }
            else
                _urlSourceType = SOURCE_TYPE_URL;

            videoMux._EnsureInit();
            videoMux._Register(VideoManager.VIDEO_READY_EVENT, this, "_OnVideoReady");
            videoMux._Register(VideoManager.VIDEO_START_EVENT, this, "_OnVideoStart");
            videoMux._Register(VideoManager.VIDEO_END_EVENT, this, "_OnVideoEnd");
            videoMux._Register(VideoManager.VIDEO_ERROR_EVENT, this, "_OnVideoError");
            videoMux._Register(VideoManager.SOURCE_CHANGE_EVENT, this, "_OnSourceChange");

            if (videoMux.SupportsAVPro)
                videoMux._UpdateVideoSource(VideoSource.VIDEO_SOURCE_AVPRO);
            else if (videoMux.SupportsUnity)
                videoMux._UpdateVideoSource(VideoSource.VIDEO_SOURCE_UNITY);

            _SetScreenFit(defaultScreenFit);

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            if (playOnJoin)
            {
                _inSustainZone = true;
                _TriggerPlay();
            }
        }

        public void _TriggerPlay()
        {
            DebugLog("Trigger play");
            if (playAt > 0 || playerState == VIDEO_STATE_PLAYING || playerState == VIDEO_STATE_LOADING)
                return;

            _PlayVideoAfter(_GetSelectedUrl(), 0);
        }

        public void _TriggerStop()
        {
            DebugLog("Trigger Stop");
            _StopVideo();
        }

        public void _TriggerPause()
        {
            _PauseVideo();
        }

        public void _PlaybackZoneEnter()
        {
            _inSustainZone = true;

            if (_triggerZoneSame)
                _TriggerPlay();
        }

        public void _PlaybackZoneExit()
        {
            DebugLog("Playback Zone Exit");
            _inSustainZone = false;
            _TriggerStop();
        }

        public override void _Resync()
        {
            DebugLog("Resync");
            if (playerState == VIDEO_STATE_STOPPED)
                return;

            _StopVideo();
            _PlayVideo(_GetSelectedUrl());
        }

        public void _UrlChanged()
        {
            _Resync();
        }

        public override void _ChangeUrl(VRCUrl url)
        {
            streamUrl = url;
            _UrlChanged();
        }

        void _PlayVideoAfter(VRCUrl url, float delay)
        {
            playAtUrl = url;
            playAt = Time.time + delay;
        }

        VRCUrl _GetSelectedUrl()
        {
            switch (_urlSourceType)
            {
                case SOURCE_TYPE_STATIC:
                    return staticUrlSource._GetUrl();
                case SOURCE_TYPE_URL:
                default:
                    return streamUrl;
            }
        }

        public override void _SetSourceLatency(int latency)
        {
            videoMux._UpdateLowLatency(latency);
        }

        public override void _SetSourceResolution(int res)
        {
            videoMux._UpdatePreferredResolution(res);
        }

        public override void _SetScreenFit(TXLScreenFit fit)
        {
            screenFit = (byte)fit;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _PlayVideo(VRCUrl url)
        {
            playAt = 0;
            if (!Utilities.IsValid(url))
                return;

            DebugLog("Play video " + url);

            string urlStr = url.Get();
            if (urlStr == null || urlStr == "")
                return;

            _UpdatePlayerState(VIDEO_STATE_LOADING);

            VRCUrl resolvedUrl = url;
            if (Utilities.IsValid(urlRemapper))
            {
                resolvedUrl = urlRemapper._Remap(url);
                if (Utilities.IsValid(resolvedUrl) && resolvedUrl.Get() != url.Get())
                    DebugLog("Remapped URL");
            }

            videoMux._VideoLoadURL(resolvedUrl);
        }

        void _StopVideo()
        {
            DebugLog("Stop video");

            if (seekableSource && resumePosition)
                _lastVideoPosition = videoMux.VideoTime;
            else
                _lastVideoPosition = 0;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            videoMux._VideoStop();

            playAt = 0;
            seekableSource = false;
            paused = false;
        }

        void _PauseVideo()
        {
            DebugLog("Pause video");

            if (playerState != VIDEO_STATE_PLAYING)
                return;

            if (paused)
            {
                videoMux._VideoPlay();
                if (seekableSource)
                {
                    DebugLog($"Set time to {_lastVideoPosition}");
                    videoMux._VideoSetTime(_lastVideoPosition);
                }
            }
            else
            {
                if (seekableSource)
                    _lastVideoPosition = videoMux.VideoTime;
                videoMux._VideoPause();
            }

            paused = !paused;
        }

        public void _OnVideoReady()
        {
            float position = videoMux.VideoTime;
            float duration = videoMux.VideoDuration;
            DebugLog("Video ready, duration: " + duration + ", position: " + position);

            if (_hasSustainZone && !_inSustainZone)
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;
            _UpdateTracking(position, position, duration);

            videoMux._VideoPlay();
        }

        public void _OnVideoStart()
        {
            DebugLog("Video start");

            if (_hasSustainZone && !_inSustainZone)
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            _UpdatePlayerState(VIDEO_STATE_PLAYING);
            //_UpdatePlayerPaused(false);
            playStartTime = Time.time;

            if (seekableSource)
            {
                videoMux._VideoSetTime(_lastVideoPosition);
                _lastVideoPosition = 0;
            }
        }

        public void _OnVideoEnd()
        {
            if (!seekableSource && Time.time - playStartTime < 1)
            {
                DebugLog("Video end encountered at start of stream, ignoring");
                return;
            }

            DebugLog("Video end");

            seekableSource = false;
            paused = false;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            _lastVideoPosition = 0;

            // TODO: Loop for AVPro
        }

        public void _OnVideoError(VideoError videoError)
        {
            if (playerState == VIDEO_STATE_STOPPED)
                return;

            videoMux._VideoStop();

            string code = "";
            switch (videoError)
            {
                case VideoError.AccessDenied: code = "Access Denied"; break;
                case VideoError.InvalidURL: code = "Invalid URL"; break;
                case VideoError.PlayerError: code = "Player Error"; break;
                case VideoError.RateLimited: code = "Rate Limited"; break;
                case VideoError.Unknown: code = "Unknown Error"; break;
            }

            VRCUrl url = _GetSelectedUrl();
            DebugLog("Video stream failed: " + url);
            DebugLog("Error code: " + code);

            _UpdatePlayerStateError(videoError);

            if (retryOnError)
                _PlayVideoAfter(url, retryTimeout);
        }

        public void _OnSourceChange()
        {
            playerSource = (short)videoMux.ActiveSourceType;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);

            _Resync();
        }

        void _UpdatePlayerState(int state)
        {
            playerState = state;

            if (state != VIDEO_STATE_PLAYING)
            {
                paused = false;
                syncing = false;
            }

            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdatePlayerStateError(VideoError error)
        {
            playerState = VIDEO_STATE_ERROR;
            lastErrorCode = error;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdateTracking(float position, float target, float duration)
        {
            trackPosition = position;
            trackDuration = duration;
            trackTarget = target;
            _UpdateHandlers(EVENT_VIDEO_TRACKING_UPDATE);
        }

        // Update is called once per frame
        void Update()
        {
            if (playAt > 0 && Time.time > playAt)
            {
                playAt = 0;
                _PlayVideo(playAtUrl);
            }
        }

        // Debug
        void DebugLog(string message)
        {
            if (debugLogging)
                Debug.Log("[VideoTXL:LocalPlayer] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("LocalPlayer", message);
        }
    }
}
