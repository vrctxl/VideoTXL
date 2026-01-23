
using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [AddComponentMenu("VideoTXL/Sync Player")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class SyncPlayer : TXLVideoPlayer
    {
        public UrlRemapper urlRemapper;
        public UrlInfoResolver urlInfoResolver;
        public AccessControl accessControl;

        [Obsolete("Use trackedPlaybackZone")]
        public CompoundZoneTrigger playbackZone;
        [Obsolete("Use trackedPlaybackZone")]
        public ZoneMembership playbackZoneMembership;
        [SerializeField] internal TrackedZoneTrigger trackedZoneTrigger;

        public VRCUrl defaultUrl;
        public VRCUrl defaultQuestUrl;
        public bool defaultLocked = false;
        public bool loop = false;
        public bool retryOnError = true;
        public bool autoFailbackToAVPro = true;
        public bool holdLoadedVideos = false;

        public float syncFrequency = 5;
        public float syncThreshold = 1;
        public bool autoInternalAVSync = false;

        public TXLScreenFit defaultScreenFit = TXLScreenFit.Fit;
        public short defaultVideoSource = VideoSource.VIDEO_SOURCE_NONE;

        public bool debugLogging = true;
        public DebugLog debugLog;
        public DebugState debugState;
        public bool eventLogging = false;
        public bool traceLogging = false;

        float retryTimeout = 6;
        //float loadWaitTime = 2;
        //float syncLatchUpdateFrequency = 0.2f;

        [UdonSynced]
        short _syncVideoSource = VideoSource.VIDEO_SOURCE_NONE;
        [UdonSynced]
        short _syncVideoSourceOverride = VideoSource.VIDEO_SOURCE_NONE;
        short fallbackSourceOverride = VideoSource.VIDEO_SOURCE_NONE;

        [UdonSynced]
        byte _syncScreenFit = SCREEN_FIT;

        [UdonSynced]
        VRCUrl _syncUrl = VRCUrl.Empty;
        [UdonSynced]
        VRCUrl _syncQuestUrl = VRCUrl.Empty;
        [UdonSynced]
        short _syncUrlSourceIndex = -1;

        VRCUrl _preResolvedUrl = VRCUrl.Empty;
        VRCUrl _resolvedUrl = VRCUrl.Empty;

        [UdonSynced]
        int _syncVideoNumber;
        int _loadedVideoNumber;
        [UdonSynced]
        int _syncPlaybackNumber;

        [UdonSynced, NonSerialized]
        public bool _syncOwnerPlaying;

        [UdonSynced, NonSerialized]
        public bool _syncOwnerPaused = false;

        [UdonSynced]
        float _syncVideoStartNetworkTime;

        [UdonSynced]
        float _syncVideoExpectedEndTime;

        [UdonSynced]
        bool _syncLocked = true;

        [UdonSynced, FieldChangeCallback(nameof(_SyncRepeatMode))]
        TXLRepeatMode syncRepeatMode;

        [UdonSynced, FieldChangeCallback(nameof(HoldVideos))]
        bool _syncHoldVideos = false;

        //[NonSerialized]
        //public int localPlayerState = VIDEO_STATE_STOPPED;
        //[NonSerialized]
        //public VideoError localLastErrorCode;
        [NonSerialized]
        public VRCPlayerApi playerArg;

        float _lastVideoPosition = 0;
        float _videoTargetTime = 0;

        bool _waitForSync;
        bool _holdReadyState = false;
        bool _heldVideoReady = false;
        bool _skipAdvanceNextTrack = false;
        float _lastSyncTime;
        bool _overrideLock = false;
        bool _suppressSourceUpdate = false;
        public bool _videoReady = false;

        float _pendingLoadTime = 0;

        bool _hasAccessControl = false;
        bool _hasSustainZone = false;
        bool _inSustainZone = false;
        bool _initDeserialize = false;
        bool _usingDebug = false;

        [HideInInspector] public int internalArgSourceIndex;

        VideoUrlSource[] urlSourceList;

        // Realtime state

        [NonSerialized]
        public float previousTrackPosition;

        protected override void _Init()
        {
            base._Init();

            _usingDebug = debugLogging || Utilities.IsValid(debugLog);
            if (_usingDebug) DebugLog("Init");

            if (IsQuest)
            {
                if (_usingDebug) DebugLog("Detected Quest platform");
            }
            else if (Utilities.IsValid(Networking.LocalPlayer))
            {
                if (_usingDebug) DebugLog("Detected " + (Networking.LocalPlayer.IsUserInVR() ? "PC VR" : "PC Desktop") + " Platform");
            }

            if (Utilities.IsValid(debugState))
                _SetDebugState(debugState);

            if (eventLogging)
                eventDebugLog = debugLog;

            _hasAccessControl = Utilities.IsValid(accessControl);
            _hasSustainZone = playbackZoneMembership || trackedZoneTrigger;
            if (_hasSustainZone)
            {
                if (playbackZoneMembership && !trackedZoneTrigger)
                    trackedZoneTrigger = playbackZoneMembership.GetComponent<TrackedZoneTrigger>();

                if (trackedZoneTrigger)
                {
                    if (Utilities.IsValid(Networking.LocalPlayer))
                        _inSustainZone = trackedZoneTrigger._PlayerInZone(Networking.LocalPlayer);
                    trackedZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_OnPlaybackZoneEnter", "playerArg");
                    trackedZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, "_OnPlaybackZoneExit", "playerArg");
                }
                else if (playbackZoneMembership)
                {
                    if (Utilities.IsValid(Networking.LocalPlayer))
                        _inSustainZone = playbackZoneMembership._ContainsPlayer(Networking.LocalPlayer);
                    playbackZoneMembership._RegisterAddPlayer(this, "_OnPlaybackZoneEnter", "playerArg");
                    playbackZoneMembership._RegisterRemovePlayer(this, "_OnPlaybackZoneExit", "playerArg");
                    //playbackZone._Register((UdonBehaviour)(Component)this, "_PlaybackZoneEnter", "_PlaybackZoneExit", null);
                }
            }
            else
                _inSustainZone = true;

            if (Utilities.IsValid(urlRemapper))
                urlRemapper._SetPlatform(IsQuest ? GamePlatform.Quest : GamePlatform.PC);

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            if (Utilities.IsValid(sourceManager))
            {
                sourceManager._BindVideoPlayer(this);
                sourceManager._Register(SourceManager.EVENT_URL_READY, this, nameof(_OnSourceUrlReady));
            }

            _syncLocked = defaultLocked;
            _SyncRepeatMode = loop ? TXLRepeatMode.All : TXLRepeatMode.None;
            _syncScreenFit = (byte)defaultScreenFit;
            HoldVideos = holdLoadedVideos;
            _UpdateLockState(_syncLocked);
            _UpdateScreenFit(_syncScreenFit);

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            if (autoInternalAVSync)
                SendCustomEventDelayedSeconds("_AVSyncStart", 1);

            if (!Networking.IsOwner(gameObject))
                SendCustomEventDelayedSeconds(nameof(_InitCheck), 5);
        }

        protected override void _PostInit()
        {
            if (!videoMux)
            {
                DebugError("No video manager set at time of post init, skipping default playback");
                return;
            }

            if (Networking.IsOwner(gameObject))
            {
                if (_IsUrlValid(defaultUrl))
                    _PlayVideoAfterFallback(defaultUrl, defaultQuestUrl, -1, 3);
                else if (sourceManager)
                    SendCustomEventDelayedFrames("_PlayPlaylistUrl", 3);
            }

            _UpdateHandlers(EVENT_POSTINIT_DONE);
        }

        public void _InitCheck()
        {
            if (!_initDeserialize)
            {
                if (_usingDebug) DebugLog("Deserialize not received in reasonable time");
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "RequestOwnerSync");
            }
        }

        public override void _SetVideoManager(VideoManager manager)
        {
            if (videoMux)
            {
                if (_usingDebug) DebugLog("VideoManager already set");
                return;
            }

            base._SetVideoManager(manager);

            if (traceLogging) DebugTrace("Setting video manager");

            videoMux._Register(VideoManager.VIDEO_READY_EVENT, this, "_OnVideoReady");
            videoMux._Register(VideoManager.VIDEO_START_EVENT, this, "_OnVideoStart");
            videoMux._Register(VideoManager.VIDEO_END_EVENT, this, "_OnVideoEnd");
            videoMux._Register(VideoManager.VIDEO_ERROR_EVENT, this, "_OnVideoError");
            videoMux._Register(VideoManager.SOURCE_CHANGE_EVENT, this, "_OnSourceChange");

            videoMux._UpdateLowLatency(VideoSource.LOW_LATENCY_ENABLE);
            videoMux._SetAVSync(autoInternalAVSync);
            _UpdateVideoManagerLoop();

            _UpdateVideoSourceOverride(defaultVideoSource);
            _UpdateVideoManagerSourceNoResync(videoMux.ActiveSourceType);
        }

        public override void _SetAudioManager(AudioManager manager)
        {
            if (audioManager)
            {
                if (_usingDebug) DebugLog("AudioManager already set");
                return;
            }

            base._SetAudioManager(manager);

            if (traceLogging) DebugTrace("Setting audio manager");

            if (audioManager)
                audioManager._Register(AudioManager.EVENT_CHANNEL_GROUP_CHANGED, this, "_OnAudioProfileChanged");
        }

        internal TXLRepeatMode _SyncRepeatMode
        {
            get { return syncRepeatMode; }
            set
            {
                syncRepeatMode = value;
                repeatPlaylist = value != TXLRepeatMode.None;
 
                _UpdateVideoManagerLoop();

                _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
                _UpdateHandlers(EVENT_VIDEO_PLAYLIST_UPDATE);
            }
        }

        void _UpdateVideoManagerLoop()
        {
            bool enableLoop = syncRepeatMode != TXLRepeatMode.None;
            if (enableLoop)
            {
                bool activePlaylist = false;
                if (sourceManager && sourceManager._GetValidSource() >= 0)
                    activePlaylist = true;

                if (activePlaylist && syncRepeatMode != TXLRepeatMode.Single)
                    enableLoop = false;
            }

            if (videoMux)
            {
                if (!enableLoop)
                    videoMux._VideoSetLoop(false);
                else if (enableLoop && videoMux.ActiveSourceType == VideoSource.VIDEO_SOURCE_UNITY)
                    videoMux._VideoSetLoop(true);
            }
        }

        TXLRepeatMode _CheckRepeatMode(TXLRepeatMode mode)
        {
            bool activePlaylist = false;
            if (sourceManager && sourceManager._GetValidSource() >= 0)
                activePlaylist = true;

            if (mode == TXLRepeatMode.Single && !activePlaylist)
                mode = TXLRepeatMode.All;

            return mode;
        }

        public override UrlInfoResolver UrlInfoResolver
        {
            get { return urlInfoResolver; }
        }

        public override TXLRepeatMode RepeatMode
        {
            get { return syncRepeatMode; }
            set
            {
                if (traceLogging) DebugTrace($"RepeatMode = {value}");
                if (!_TakeControl())
                    return;

                value = _CheckRepeatMode(value);

                _SyncRepeatMode = value;
                RequestSerialization();
            }
        }

        public bool HoldVideos
        {
            get { return _syncHoldVideos; }
            set
            {
                if (!value && _videoReady)
                    _CancelHold();

                _syncHoldVideos = value;
                _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
            }
        }

        public bool HoldReady
        {
            get { return _videoReady && _syncPlaybackNumber < _syncVideoNumber; }
        }

        public void _OnPlaybackZoneEnter()
        {
            if (traceLogging) DebugTrace("Event OnPlaybackZoneEnter");

            if (playerArg == Networking.LocalPlayer)
            {
                _inSustainZone = true;

                VRCPlayerApi currentOwner = Networking.GetOwner(gameObject);
                if (trackedZoneTrigger)
                {
                    if (trackedZoneTrigger._PlayerInZone(currentOwner))
                        Networking.SetOwner(playerArg, gameObject);
                } else if (playbackZoneMembership)
                {
                    if (!playbackZoneMembership._ContainsPlayer(currentOwner))
                        Networking.SetOwner(playerArg, gameObject);
                }

                if (_syncVideoExpectedEndTime != 0)
                {
                    float serverTime = (float)Networking.GetServerTimeInSeconds();

                    // If time hasn't reached theoretical end of track, set starting time to where it would have been
                    // if there were still viewers in the playback zone
                    if (serverTime < _syncVideoExpectedEndTime)
                    {
                        _videoTargetTime = serverTime - _syncVideoStartNetworkTime;
                        if (_usingDebug) DebugLog($"Playback enter: start at {_videoTargetTime}");
                        _StartVideoLoad();
                        return;
                    }

                    // Otherwise play next available track or stop, depending on queue/settings
                    if (Networking.IsOwner(playerArg, gameObject))
                    {
                        if (_usingDebug) DebugLog($"Playback enter: is owner and track has ended");
                        _ConditionalPlayNext();
                        return;
                    }
                }

                if (_usingDebug) DebugLog("Playback enter: no expected end time set");
                _StartVideoLoad();
            }
        }

        public void _OnPlaybackZoneExit()
        {
            if (traceLogging) DebugTrace("Event OnPlaybackZoneExit");

            if (playerArg == Networking.LocalPlayer)
            {
                _inSustainZone = false;

                if (Networking.IsOwner(gameObject))
                {
                    if (trackedZoneTrigger)
                    {
                        if (trackedZoneTrigger.TrackedPlayerCount > 0)
                        {
                            DataList tracked = trackedZoneTrigger._GetTrackedPlayers();
                            if (tracked.TryGetValue(0, TokenType.Reference, out DataToken playerToken))
                            {
                                VRCPlayerApi nextPlayer = (VRCPlayerApi)playerToken.Reference;
                                if (Utilities.IsValid(nextPlayer) && nextPlayer.IsValid())
                                    Networking.SetOwner(nextPlayer, gameObject);
                            }
                        }
                    }
                    else if (playbackZoneMembership)
                    {
                        if (playbackZoneMembership._PlayerCount() > 0)
                        {
                            VRCPlayerApi nextPlayer = playbackZoneMembership._GetPlayer(0);
                            if (Utilities.IsValid(nextPlayer) && nextPlayer.IsValid())
                                Networking.SetOwner(nextPlayer, gameObject);
                        }
                    }
                }

                videoMux._VideoStop();
                _UpdatePlayerState(VIDEO_STATE_STOPPED);
            }
        }

        public override void _ValidateVideoSources()
        {
            if (traceLogging) DebugTrace("Validate Video Sources");

            if (Networking.IsOwner(gameObject))
            {
                if (_syncHoldVideos && playerState == VIDEO_STATE_STOPPED)
                    _ConditionalPlayNext();
            }
        }

        public void _TriggerPlay()
        {
            if (traceLogging) DebugTrace("Trigger Play");
            if (playerState == VIDEO_STATE_PLAYING || playerState == VIDEO_STATE_LOADING)
                return;

            _PlayVideo(_syncUrl, _syncUrlSourceIndex);
        }

        public void _TriggerStop()
        {
            if (traceLogging) DebugTrace("Trigger Stop");
            if (!_TakeControl())
                return;

            _StopVideo();
        }

        public void _TriggerPause()
        {
            if (traceLogging) DebugTrace("Trigger Pause");
            if (!seekableSource || playerState != VIDEO_STATE_PLAYING)
                return;
            if (!_TakeControl())
                return;

            _syncOwnerPaused = !_syncOwnerPaused;

            if (_syncOwnerPaused)
            {
                float videoTime = videoMux.VideoTime;
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - videoTime;
                _syncVideoExpectedEndTime = _syncVideoStartNetworkTime + trackDuration;
                _videoTargetTime = videoTime;
                videoMux._VideoPause();
            }
            else
                videoMux._VideoPlay();

            _UpdatePlayerPaused(_syncOwnerPaused);

            RequestSerialization();
        }

        public void _TriggerLock()
        {
            if (traceLogging) DebugTrace("Trigger Lock");
            _SetLock(!_syncLocked);
        }

        public void _SetLock(bool state)
        {
            if (traceLogging) DebugTrace("Set Lock");
            if (!_IsAdmin() || !_TakeControl())
                return;

            _syncLocked = state;
            _UpdateLockState(_syncLocked);
            RequestSerialization();
        }

        public void _TriggerRepeatMode()
        {
            if (traceLogging) DebugTrace("Trigger Repeat Mode");
            if (!_TakeControl())
                return;

            if (syncRepeatMode == TXLRepeatMode.None)
                RepeatMode = TXLRepeatMode.All;
            else if (syncRepeatMode == TXLRepeatMode.All)
            {
                if (_CheckRepeatMode(TXLRepeatMode.Single) == TXLRepeatMode.All)
                    RepeatMode = TXLRepeatMode.None;
                else
                    RepeatMode = TXLRepeatMode.Single;
            }
            else if (syncRepeatMode == TXLRepeatMode.Single)
                RepeatMode = TXLRepeatMode.None;
        }

        public void _TriggerInternalAVSync()
        {
            if (traceLogging) DebugTrace("Trigger Internal AVSync Mode");

            _UpdateAVSync(!autoInternalAVSync);
        }

        public override void _SetSourceMode(int mode)
        {
            if (traceLogging) DebugTrace("Set Source Mode");
            if (!_TakeControl())
                return;

#if UNITY_EDITOR_LINUX
            if (mode == VideoSource.VIDEO_SOURCE_AVPRO)
            {
                DebugLog("AVPro does not support Linux!");
                mode = VideoSource.VIDEO_SOURCE_UNITY;
            }
#endif

            _UpdateVideoSourceOverride(mode);
            if (mode != VideoSource.VIDEO_SOURCE_NONE)
            {
                videoMux._UpdateVideoSource(_syncVideoSourceOverride);
                _UpdateVideoManagerLoop();
            }

            RequestSerialization();
        }

        public override void _SetSourceLatency(int latency)
        {
            if (traceLogging) DebugTrace("Set Source latency");
            videoMux._UpdateLowLatency(latency);
        }

        public override void _SetSourceResolution(int res)
        {
            if (traceLogging) DebugTrace("Set Source Resolution");
            videoMux._UpdatePreferredResolution(res);
        }

        public override void _SetScreenFit(TXLScreenFit fit)
        {
            if (traceLogging) DebugTrace("Set Screen Fit");
            if (!_TakeControl())
                return;

            _syncScreenFit = (byte)fit;
            _UpdateScreenFit(_syncScreenFit);

            RequestSerialization();
        }

        public override void _Resync()
        {
            if (traceLogging) DebugTrace("Resync");
            _ForceResync();
        }

        public override void _ChangeUrl(VRCUrl url)
        {
            if (traceLogging) DebugTrace("Change Url");
            if (!_TakeControl())
                return;
            
            _PlayVideo(url, -1);
        }

        public void _ChangeUrlQuestFallback(VRCUrl url, VRCUrl questUrl)
        {
            if (traceLogging) DebugTrace("Change Url Quest Fallback");
            if (!_TakeControl())
                return;
            
            _PlayVideoFallback(url, questUrl, -1);
        }

        public void _SetHoldMode(bool holdState)
        {
            if (traceLogging) DebugTrace("Set Hold Mode");
            if (!_TakeControl())
                return;

            HoldVideos = holdState;
            RequestSerialization();
        }

        public void _ReleaseHold()
        {
            if (traceLogging) DebugTrace("Release Hold");
            if (_syncLocked && !_TakeControl())
                return;

            if (_syncPlaybackNumber < _syncVideoNumber)
            {
                _syncPlaybackNumber = _syncVideoNumber;
                RequestSerialization();

                if (_videoReady)
                    videoMux._VideoPlay();
            }
        }

        public void _CancelHold()
        {
            if (traceLogging) DebugTrace("Cancel Hold");
            _StopVideo();
        }

        /*public void _SkipNextAdvance()
        {
            if (traceLogging) DebugTrace("Skip Next Advance");
            if (Networking.IsOwner(gameObject))
                _skipAdvanceNextTrack = true;
        }*/

        [Obsolete("Queued URL has been replaced by Source Manager")]
        public void _UpdateQueuedUrl(VRCUrl url) { }

        public void _SetTargetTime(float time)
        {
            if (traceLogging) DebugTrace($"Set target time: {time:N3}");
            if (playerState != VIDEO_STATE_PLAYING)
                return;
            if (!seekableSource)
                return;
            if (!_TakeControl())
                return;

            // Allowing AVPro to set time directly to end of track appears to trigger deadlock sometimes
            float duration = videoMux.VideoDuration;
            if (duration - time < 1)
            {
                if (_PlayNextIfAvailable())
                    return;

                time = duration - 1;
            }

            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - time;
            _syncVideoExpectedEndTime = _syncVideoStartNetworkTime + duration;

            SyncVideoImmediate();
            RequestSerialization();
        }

        void _PlayVideo(VRCUrl url, short urlSourceIndex)
        {
            _PlayVideoAfter(url, urlSourceIndex, 0);
        }

        void _PlayVideoFallback(VRCUrl url, VRCUrl questUrl, short urlSourceIndex)
        {
            _PlayVideoAfterFallback(url, questUrl, urlSourceIndex, 0);
        }

        void _PlayVideoAfter(VRCUrl url, short urlSourceIndex, float delay)
        {
            _PlayVideoAfterFallback(url, VRCUrl.Empty, urlSourceIndex, delay);
        }

        void _PlayVideoAfterFallback(VRCUrl url, VRCUrl questUrl, short urlSourceIndex, float delay)
        {
            if (!_IsUrlValid(url))
                return;

            if (urlInfoResolver)
                urlInfoResolver._ResolveInfo(url);

            if (_usingDebug) DebugLog("Play video " + url);
            bool isOwner = Networking.IsOwner(gameObject);
            if (!isOwner && !_TakeControl())
                return;

            if (!_IsUrlValid(questUrl))
                questUrl = VRCUrl.Empty;

            if (currentUrlSource)
                currentUrlSource._OnVideoStop();

            string urlStr = url.Get();

            _syncVideoSource = _syncVideoSourceOverride;
            if (_syncVideoSource == VideoSource.VIDEO_SOURCE_NONE)
            {
                if (_IsAutoVideoSource(urlStr) && videoMux.SupportsUnity)
                    _syncVideoSource = VideoSource.VIDEO_SOURCE_UNITY;
                else if (videoMux.SupportsAVPro)
                    _syncVideoSource = VideoSource.VIDEO_SOURCE_AVPRO;

                if (_syncVideoSource == VideoSource.VIDEO_SOURCE_NONE)
                {
                    if (videoMux.SupportsUnity)
                        _syncVideoSource = VideoSource.VIDEO_SOURCE_UNITY;
                    else if (videoMux.SupportsAVPro)
                        _syncVideoSource = VideoSource.VIDEO_SOURCE_AVPRO;
                }
            }

            // If fallback trigger is set, override source type this one time
            if (fallbackSourceOverride != VideoSource.VIDEO_SOURCE_NONE)
            {
                _syncVideoSource = fallbackSourceOverride;
                fallbackSourceOverride = VideoSource.VIDEO_SOURCE_NONE;
            }

            _UpdateVideoManagerSourceNoResync(_syncVideoSource);

            _syncUrl = url;
            _syncQuestUrl = questUrl;
            _syncUrlSourceIndex = urlSourceIndex;
            _syncVideoNumber += isOwner ? 1 : 2;
            _loadedVideoNumber = _syncVideoNumber;
            _syncOwnerPlaying = false;
            _syncOwnerPaused = false;
            _skipAdvanceNextTrack = false;

            if (!HoldVideos)
                _syncPlaybackNumber = _syncVideoNumber;

            _syncVideoStartNetworkTime = float.MaxValue;
            _syncVideoExpectedEndTime = 0;
            RequestSerialization();

            _videoTargetTime = _ParseTimeFromUrl(urlStr);
            _UpdateLastUrl();

            // Conditional player stop to try and avoid piling on AVPro at end of track
            // and maybe triggering bad things
            if (playerState == VIDEO_STATE_PLAYING && videoMux.VideoIsPlaying && seekableSource)
            {
                float duration = videoMux.VideoDuration;
                float remaining = videoMux.VideoTime;
                if (remaining > 2)
                    videoMux._VideoStop();
            }
            else if (playerState == VIDEO_STATE_LOADING)
                videoMux._VideoStop();

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            _StartVideoLoadDelay(delay);
        }

        public void _LoopVideo()
        {
            if (traceLogging) DebugTrace("Loop Video");
            _overrideLock = true;
            _skipAdvanceNextTrack = false;

            _PlayVideo(_syncUrl, _syncUrlSourceIndex);

            _overrideLock = false;
        }

        [Obsolete("Queued URL has been replaced by Source Manager")]
        public void _PlayQueuedUrl() { }

        public void _OnSourceUrlReady()
        {
            if (traceLogging) DebugTrace($"Event OnSourceUrlReady");
            if (Networking.IsOwner(gameObject))
                _PlaySourceUrl();
        }

        public void _PlayPlaylistUrl()
        {
            if (sourceManager)
                sourceManager._AdvanceNext();
        }

        void _PlaySourceUrl()
        {
            if (traceLogging) DebugTrace($"Play Source URL");

            _overrideLock = true;
            _skipAdvanceNextTrack = false;

            if (sourceManager && sourceManager.ReadyUrl != VRCUrl.Empty)
                _PlayVideoFallback(sourceManager.ReadyUrl, sourceManager.ReadyQuestUrl, (short)sourceManager._GetSourceIndex(sourceManager.ReadySource));

            _overrideLock = false;
        }

        void _PlayPlaylistUrl(VideoUrlSource source)
        {
            if (traceLogging) DebugTrace($"Play Playlist Url from {source}");

            _overrideLock = true;
            _skipAdvanceNextTrack = false;

            if (source && source.IsValid)
                _PlayVideoFallback(source._GetCurrentUrl(), source._GetCurrentQuestUrl(), (short)sourceManager._GetSourceIndex(source));

            _overrideLock = false;
        }

        bool _IsUrlValid(VRCUrl url)
        {
            if (!Utilities.IsValid(url))
                return false;

            string urlStr = url.Get();
            if (urlStr == null || urlStr == "")
                return false;

            return true;
        }

        bool _IsAutoVideoSource(string urlStr)
        {
            // Assume youtube is video-based (but it could be a livestream...)

            // NB: As of 2024-07-25 Youtube no longer returns compatible codecs for Unity video.  Unless workaround is found,
            // Youtube videos must now be loaded on AVPro sources.
            if (urlStr.Contains("youtube.com/watch") || urlStr.Contains("youtu.be/"))
            {
#if UNITY_EDITOR
                return VideoManager && VideoManager.YoutubeAutoUnityInEditor;
#else
                return false;
#endif
            }

            // VRCDN sources are always stream
            if (urlStr.Contains("vrcdn.live"))
                return false;

            if (urlStr.EndsWith("mp4", StringComparison.CurrentCultureIgnoreCase))
                return true;
            if (urlStr.EndsWith("wmv", StringComparison.CurrentCultureIgnoreCase))
                return true;

            return false;
        }

        // Time parsing code adapted from USharpVideo project by Merlin
        float _ParseTimeFromUrl(string urlStr)
        {
            // Attempt to parse out a start time from YouTube links with t= or start=
            if (!urlStr.Contains("youtube.com/watch") && !urlStr.Contains("youtu.be/"))
                return 0;

            int tIndex = urlStr.IndexOf("?t=");
            if (tIndex == -1)
                tIndex = urlStr.IndexOf("&t=");
            if (tIndex == -1)
                tIndex = urlStr.IndexOf("?start=");
            if (tIndex == -1)
                tIndex = urlStr.IndexOf("&start=");
            if (tIndex == -1)
                return 0;

            char[] urlArr = urlStr.ToCharArray();
            int numIdx = urlStr.IndexOf('=', tIndex) + 1;

            string intStr = "";
            while (numIdx < urlArr.Length)
            {
                char currentChar = urlArr[numIdx];
                if (!char.IsNumber(currentChar))
                    break;

                intStr += currentChar;
                ++numIdx;
            }

            if (intStr.Length == 0)
                return 0;

            int secondsCount = 0;
            if (!int.TryParse(intStr, out secondsCount))
                return 0;

            return secondsCount;
        }

        void _StartVideoLoadDelay(float delay)
        {
            _pendingLoadTime = Time.time + delay;
        }

        void _StartVideoLoad()
        {
            _pendingLoadTime = 0;
            _videoReady = false;

            if (_syncUrl == null || _syncUrl.Get() == "")
                return;

            if (playerState == VIDEO_STATE_PLAYING || playerState == VIDEO_STATE_LOADING)
                videoMux._VideoStop();

            _UpdatePlayerState(VIDEO_STATE_LOADING);

            VRCUrl url = _syncUrl;
            if (IsQuest && _syncQuestUrl != null && _syncQuestUrl != VRCUrl.Empty && _syncQuestUrl.Get().Trim() != "")
            {
                url = _syncQuestUrl;
                if (_usingDebug) DebugLog($"Loading Quest URL variant: {url}");
            }

            _preResolvedUrl = url;

            if (Utilities.IsValid(urlRemapper))
            {
                url = urlRemapper._Remap(url);
                if (Utilities.IsValid(url) && _syncUrl.Get() != url.Get())
                {
                    if (_usingDebug) DebugLog($"Remapped URL: {url}");
                }
            }

            _resolvedUrl = url;
            videoMux._VideoLoadURL(url);
        }

        public void _StopVideo()
        {
            if (_usingDebug) DebugLog("Stop video");

            if (seekableSource)
                _lastVideoPosition = videoMux.VideoTime;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            videoMux._VideoStop();
            _videoTargetTime = 0;
            _pendingLoadTime = 0;
            _videoReady = false;

            if (currentUrlSource)
                currentUrlSource._OnVideoStop();

            if (Networking.IsOwner(gameObject))
            {
                _syncVideoStartNetworkTime = 0;
                //_syncVideoExpectedEndTime = 0;
                _syncOwnerPlaying = false;
                _syncOwnerPaused = false;
                _syncUrl = VRCUrl.Empty;
                RequestSerialization();
            }
        }

        public void _OnVideoReady()
        {
            if (traceLogging) DebugTrace("Event OnVideoReady");

            if (!_inSustainZone)
            {
                videoMux._VideoStop();
                _UpdatePlayerState(VIDEO_STATE_STOPPED);
                return;
            }

            float position = videoMux.VideoTime;
            float duration = videoMux.VideoDuration;
            if (_usingDebug) DebugLog("Video ready, duration: " + duration + ", position: " + position);

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;
            _UpdateTracking(position, position, duration);

            _videoReady = true;

            // If player is owner: play video
            // If Player is remote:
            //   - If owner playing state is already synced, play video
            //   - Otherwise, wait until owner playing state is synced and play later in update()
            //   TODO: Streamline by always doing this in update instead?

            if (currentUrlSource)
                currentUrlSource._OnVideoReady();

            _UpdateHandlers(EVENT_VIDEO_READY);

            if (Networking.IsOwner(gameObject))
            {
                if (_syncPlaybackNumber == _syncVideoNumber)
                    videoMux._VideoPlay();
            }
            else
            {
                // TODO: Stream bypass owner
                if (_syncOwnerPlaying)
                    videoMux._VideoPlay();
                else
                    _waitForSync = true;
            }
        }

        public void _OnVideoStart()
        {
            if (traceLogging) DebugTrace("Event OnVideoStart");

            _videoReady = false;

            if (Networking.IsOwner(gameObject))
            {
                //bool paused = _syncOwnerPaused;
                //if (paused)
                //    _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _currentPlayer.GetTime();
                //else
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _videoTargetTime;
                if (seekableSource)
                    _syncVideoExpectedEndTime = _syncVideoStartNetworkTime + trackDuration;
                else
                    _syncVideoExpectedEndTime = 0;

                _UpdatePlayerState(VIDEO_STATE_PLAYING);
                _UpdatePlayerPaused(false);

                _syncOwnerPlaying = true;
                _syncOwnerPaused = false;
                RequestSerialization();

                //if (!paused)
                if (seekableSource)
                    videoMux._VideoSetTime(_videoTargetTime);

                if (currentUrlSource)
                    currentUrlSource._OnVideoStart();

                SyncVideoImmediate();
            }
            else
            {
                if (!_syncOwnerPlaying || _syncOwnerPaused)
                {
                    // TODO: Owner bypass
                    videoMux._VideoPause();
                    _waitForSync = true;

                    _UpdatePlayerPaused(_syncOwnerPaused);
                }
                else
                {
                    _UpdatePlayerState(VIDEO_STATE_PLAYING);

                    if (currentUrlSource)
                        currentUrlSource._OnVideoStart();

                    SyncVideoImmediate();
                }
            }
        }

        public void _OnVideoEnd()
        {
            _videoReady = false;

            if (traceLogging) DebugTrace("Event OnVideoEnd");

            seekableSource = false;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);
            _lastVideoPosition = 0;

            if (currentUrlSource)
            {
                VideoEndAction action = currentUrlSource._OnVideoEnd();
                if (action == VideoEndAction.Retry)
                {
                    SendCustomEventDelayedFrames(nameof(_LoopVideo), 1);
                    return;
                } else if (action == VideoEndAction.Stop)
                {
                    _StopVideo();
                    return;
                }
            }

            _ConditionalPlayNext();
        }

        bool _PlayNextIfAvailable()
        {
            bool loadedTrack = false;

            if (sourceManager)
            {
                string currentUrl = _syncUrl != null ? _syncUrl.Get() : "";
                loadedTrack = sourceManager._AdvanceNext(currentUrl);
            }

            if (loadedTrack)
                return true;

            if (syncRepeatMode != TXLRepeatMode.None)
            {
                SendCustomEventDelayedFrames("_LoopVideo", 1);
                return true;
            }

            return false;
        }

        void _ConditionalPlayNext()
        {
            _InternalConditionalPlayNext();
        }

        void _ConditionalPlayNext(float delay)
        {
            SendCustomEventDelayedSeconds(nameof(_InternalConditionalPlayNext), delay);
        }

        public void _InternalConditionalPlayNext()
        {
            if (Networking.IsOwner(gameObject))
            {
                _overrideLock = true;

                if (_PlayNextIfAvailable())
                {
                    _overrideLock = false;
                    return;
                }

                _syncUrl = VRCUrl.Empty;
                _syncQuestUrl = VRCUrl.Empty;
                _syncUrlSourceIndex = -1;
                _syncVideoStartNetworkTime = 0;
                //_syncVideoExpectedEndTime = 0;
                _syncOwnerPlaying = false;
                RequestSerialization();

                _overrideLock = false;
            }
        }

        // AVPro sends loop event but does not auto-loop, and setting time sometimes deadlocks player *sigh*
        public void _OnVideoLoop()
        {
            if (traceLogging) DebugTrace("Event OnVideoLoop");
            /*
            float current = _currentPlayer.GetTime();
            float duration = _currentPlayer.GetDuration();
            if (_usingDebug) DebugLog($"Video loop duration={duration}, position={current}");

            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds();

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            _lastSyncTime = Time.realtimeSinceStartup;
            _currentPlayer.SetTime(0);
            */
        }

        public void _OnVideoError()
        {
            _videoReady = false;

            if (traceLogging) DebugTrace($"Event OnVideoError");
            if (playerState == VIDEO_STATE_STOPPED)
                return;

            VideoErrorClass videoErrorClass = videoMux.LastErrorClass;
            VideoError videoError = videoMux.LastError;
            videoMux._VideoStop();

            string code = "";
            switch (videoErrorClass)
            {
                case VideoErrorClass.VRChat:
                    switch (videoError)
                    {
                        case VideoError.AccessDenied: code = "Access Denied"; break;
                        case VideoError.InvalidURL: code = "Invalid URL"; break;
                        case VideoError.PlayerError: code = "Player Error"; break;
                        case VideoError.RateLimited: code = "Rate Limited"; break;
                        case VideoError.Unknown: code = "Unknown Error"; break;
                    }
                    break;
                case VideoErrorClass.TXL:
                    switch (videoMux.LastErrorTXL)
                    {
                        case VideoErrorTXL.NoAVProInEditor: code = "AVPro Not Supported in Simulator"; break;
                        case VideoErrorTXL.RetryEndStream: code = "Retry End of Stream"; break;
                        case VideoErrorTXL.Unknown: code = "Unknown Error (TXL)"; break;
                    }
                    break;
            }

            if (_usingDebug)
            {
                DebugLog("Video stream failed: " + _syncUrl);
                DebugLog("Error code: " + code);
            }

            // Try to fall back to AVPro if auto video failed (the youtube livestream problem)
            bool shouldFallback = autoFailbackToAVPro &&
                videoErrorClass == VideoErrorClass.VRChat &&
                videoError == VideoError.PlayerError &&
                videoMux.SupportsAVPro &&
                _syncVideoSourceOverride == VideoSource.VIDEO_SOURCE_NONE &&
                _syncVideoSource == VideoSource.VIDEO_SOURCE_UNITY;

            _UpdatePlayerStateError(videoError);
            if (shouldFallback)
                _SetStreamFallback();

            VideoErrorAction action = VideoErrorAction.Default;
            if (currentUrlSource)
            {
                if (videoErrorClass == VideoErrorClass.VRChat)
                    action = currentUrlSource._OnVideoError(videoError);
                else if (videoErrorClass == VideoErrorClass.TXL)
                    action = currentUrlSource._OnVideoError(videoMux.LastErrorTXL);
            }

            if (action == VideoErrorAction.Default && retryOnError)
                action = VideoErrorAction.Retry;

            if (videoErrorClass == VideoErrorClass.TXL && videoMux.LastErrorTXL == VideoErrorTXL.RetryEndStream)
                action = VideoErrorAction.Retry;

            if (Networking.IsOwner(gameObject))
            {
#if !UNITY_EDITOR_LINUX
                if (shouldFallback)
                {
                    if (_usingDebug) DebugLog("Retrying URL in stream mode");

                    fallbackSourceOverride = VideoSource.VIDEO_SOURCE_AVPRO;
                    _PlayVideoAfterFallback(_syncUrl, _syncQuestUrl, _syncUrlSourceIndex, retryTimeout);
                    return;
                }
#endif

                if (_usingDebug) DebugLog($"Error retry action: {action}");

                if (action == VideoErrorAction.Retry)
                    _StartVideoLoadDelay(retryTimeout);
                else if (action == VideoErrorAction.Advance)
                    _ConditionalPlayNext(retryTimeout);
                else
                    _StopVideo();

                /*if (retryOnError)
                {
                    _StartVideoLoadDelay(retryTimeout);
                }
                else
                {
                    _syncVideoStartNetworkTime = 0;
                    //_syncVideoExpectedEndTime = 0;
                    _videoTargetTime = 0;
                    _syncOwnerPlaying = false;
                    RequestSerialization();
                }*/
            }
            else
            {
                if (!shouldFallback && (action == VideoErrorAction.Advance || action == VideoErrorAction.Retry))
                    _StartVideoLoadDelay(retryTimeout);

                //if (!shouldFallback && retryOnError)
                //    _StartVideoLoadDelay(retryTimeout);
            }
        }

        public void _OnSourceChange()
        {
            if (traceLogging) DebugTrace($"Event OnSourceChange activeSourceType={videoMux.ActiveSourceType}");

            if (urlRemapper)
                urlRemapper._SetVideoSource(videoMux.ActiveSource);

            playerSource = (short)videoMux.ActiveSourceType;
            _UpdateHandlers(EVENT_VIDEO_SOURCE_CHANGE);

            if (!_suppressSourceUpdate && _inSustainZone)
                _ForceResync(true);
        }

        public void _OnAudioProfileChanged()
        {
            string groupName = "none";
            if (audioManager.SelectedChannelGroup)
                groupName = audioManager.SelectedChannelGroup.groupName;

            if (traceLogging) DebugTrace($"Event OnAudioProfileChanged channelGroup={groupName}");

            if (urlRemapper)
            {
                urlRemapper._SetAudioProfile(audioManager.SelectedChannelGroup);

                if (!_suppressSourceUpdate && _inSustainZone && urlRemapper._ValidRemapped(_preResolvedUrl, _resolvedUrl))
                    _ForceResync(false);
            }
        }

        public override bool SupportsOwnership
        {
            get { return true; }
            protected set { }
        }

        public override bool SupportsLock
        {
            get { return true; }
            protected set { }
        }

        public bool _IsAdmin()
        {
            if (_hasAccessControl)
                return accessControl._LocalHasAccess();

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return false;

            return player.isMaster || player.isInstanceOwner;
        }

        public override bool _CanTakeControl()
        {
            if (_overrideLock)
                return true;
            if (_hasAccessControl)
                return !_syncLocked || accessControl._LocalHasAccess();

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return false;

            return player.isMaster || player.isInstanceOwner || !_syncLocked;
        }

        public override bool _TakeControl()
        {
            if (traceLogging) DebugTrace("Take Control");
            if (!_CanTakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }

        //public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        //{
        //    return base.OnOwnershipRequest(requestingPlayer, requestedOwner);
        //}

        public override void OnDeserialization()
        {
            if (_usingDebug) DebugLog($"Deserialize: video #{_syncVideoNumber}");

            if (Networking.IsOwner(gameObject))
            {
                if (_usingDebug) DebugLog("But you're the owner.  This should not happen.");
                return;
            }

            _initDeserialize = true;

            _UpdateVideoManagerSourceNoResync(_syncVideoSource);
            playerSourceOverride = _syncVideoSourceOverride;

            _UpdateScreenFit(_syncScreenFit);
            _UpdateLockState(_syncLocked);

            if (_syncVideoNumber == _loadedVideoNumber)
            {
                if (_syncPlaybackNumber == _syncVideoNumber && _videoReady)
                    _waitForSync = true;

                if (_inSustainZone)
                {
                    if (playerState == VIDEO_STATE_PLAYING && !_syncOwnerPlaying)
                        SendCustomEventDelayedFrames("_StopVideo", 1);
                    else if (paused && !_syncOwnerPaused)
                    {
                        videoMux._VideoPlay();
                        _UpdatePlayerPaused(false);
                    }
                    else if (playerState == VIDEO_STATE_PLAYING && _syncOwnerPaused)
                    {
                        videoMux._VideoPause();
                        _UpdatePlayerPaused(true);
                    }
                    if (!_IsUrlValid(_syncUrl))
                        SendCustomEventDelayedFrames("_StopVideo", 1);
                }

                _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);

                return;
            }

            // There was some code here to bypass load owner sync bla bla

            if (urlInfoResolver)
                urlInfoResolver._ResolveInfo(_syncUrl);

            _loadedVideoNumber = _syncVideoNumber;
            _UpdateLastUrl();

            if (_inSustainZone)
            {
                if (_usingDebug) DebugLog("Starting video load from sync");
                _StartVideoLoad();
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                if (_usingDebug) DebugLog("Failed to sync");
                return;
            }
        }

        public void RequestOwnerSync()
        {
            if (_usingDebug) DebugLog("RequestOwnerSync");
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        void Update()
        {
            if (!_inSustainZone)
                return;

            bool isOwner = Networking.IsOwner(gameObject);
            float time = Time.time;

            if (_pendingLoadTime > 0 && Time.time > _pendingLoadTime)
                _StartVideoLoad();

            if (seekableSource)
            {
                float videoTime = videoMux.VideoTime;

                if (playerState == VIDEO_STATE_PLAYING)
                {
                    float position = Mathf.Floor(videoTime);
                    if (position != previousTrackPosition)
                    {
                        previousTrackPosition = position;
                        float target = position;
                        //if (syncLatched)
                        //    target = GetTargetTime();

                        _UpdateTracking(position, target, videoMux.VideoDuration);
                    }
                }

                if (_syncOwnerPaused)
                {
                    _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - videoTime;
                    _syncVideoExpectedEndTime = _syncVideoStartNetworkTime + trackDuration;
                }
            }


            // Video is playing: periodically sync with owner
            if (isOwner || !_waitForSync)
            {
                SyncVideoIfTime();
                return;
            }

            // Video is not playing, but still waiting for go-ahead from owner
            if (!_syncOwnerPlaying || _syncOwnerPaused)
                return;

            // Got go-ahead from owner, start playing video
            _UpdatePlayerState(VIDEO_STATE_PLAYING);

            _waitForSync = false;
            videoMux._VideoPlay();

            SyncVideoImmediate();
        }

        void SyncVideoIfTime()
        {
            if (Time.realtimeSinceStartup - _lastSyncTime > syncFrequency)
            {
                _lastSyncTime = Time.realtimeSinceStartup;
                SyncVideo();
            }
        }

        void SyncVideoImmediate()
        {
            //syncTime1 = -100;
            //syncTime2 = -100;
            _ResetSyncState();
            SyncVideo();
        }

        float previousCurrent = 0;
        float previousTarget = 0;

        void _ResetSyncState()
        {
            previousCurrent = -1;
            previousTarget = -1;
            _lastSyncTime = Time.realtimeSinceStartup;
        }

        void SyncVideo()
        {
            if (!seekableSource)
                return;

            float duration = videoMux.VideoDuration;
            float current = videoMux.VideoTime;
            float serverTime = (float)Networking.GetServerTimeInSeconds(); // local sync offset
            float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);

            // If we're almost at the end, don't sync to avoid possible contention with AVPro
            if (duration - current < 2)
                return;

            // Don't need to sync if we're within threshold
            float offset = current - offsetTime;
            if (Mathf.Abs(offset) < syncThreshold)
                return;

            if (_usingDebug) DebugLog($"Sync video (off by {offset:N3}s) to {offsetTime:N3}");

            // Did we get into a situation where the player can't track?
            if (current == previousCurrent)
            {
                if (offsetTime - previousTarget > syncFrequency * .8f)
                {
                    if (_usingDebug) DebugLog("Video did not advance during previous sync, forcing reload");
                    previousTarget = 0;

                    _ForceResync();
                    return;
                }
            }
            else
                previousTarget = offsetTime;

            previousCurrent = current;
            videoMux._VideoSetTime(offsetTime);
        }

        float GetTargetTime()
        {
            float duration = videoMux.VideoDuration;
            float serverTime = (float)Networking.GetServerTimeInSeconds();
            return Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
        }

        public void _ForceResync()
        {
            _ForceResync(false);
        }

        void _ForceResync(bool usePreviousSource)
        {
            bool isOwner = Networking.IsOwner(gameObject);
            if (isOwner)
            {
                if (seekableSource)
                {
                    float startTime = _videoTargetTime;
                    bool isPlaying = usePreviousSource ? videoMux.PreviousStatePlaying : videoMux.VideoIsPlaying;
                    if (isPlaying)
                    {
                        float videoTime = usePreviousSource ? videoMux.PreviousStateTime : videoMux.VideoTime;
                        startTime = Mathf.Max(videoTime, GetTargetTime());
                    }

                    _StartVideoLoad();
                    _videoTargetTime = startTime;
                }
                else
                    _StartVideoLoad();

                return;
            }

            videoMux._VideoStop();
            if (_syncOwnerPlaying)
                _StartVideoLoad();
        }

        void _UpdateVideoManagerSourceNoResync(int sourceType)
        {
            _suppressSourceUpdate = true;
            videoMux._UpdateVideoSource(sourceType);
            _UpdateVideoManagerLoop();
            _suppressSourceUpdate = false;
        }

        void _UpdateScreenFit(byte mode)
        {
            if (mode != screenFit)
            {
                if (_usingDebug) DebugLog($"Setting screen fit to {mode}");
                screenFit = mode;
                _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
            }
        }

        void _UpdateVideoSourceOverride(int sourceType)
        {
            _syncVideoSourceOverride = (short)sourceType;

            playerSourceOverride = (short)sourceType;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdatePlayerState(int state)
        {
            playerState = state;
            streamFallback = false;

            if (state != VIDEO_STATE_PLAYING)
            {
                paused = false;
                syncing = false;
            }

            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdatePlayerPaused(bool paused)
        {
            this.paused = paused;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdatePlayerSyncing(bool syncing)
        {
            this.syncing = syncing;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdatePlayerStateError(VideoError error)
        {
            playerState = VIDEO_STATE_ERROR;
            lastErrorCode = error;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _SetStreamFallback()
        {
            streamFallback = true;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdateLockState(bool state)
        {
            locked = state;
            _UpdateHandlers(EVENT_VIDEO_LOCK_UPDATE);
        }

        void _UpdateTracking(float position, float target, float duration)
        {
            trackPosition = position;
            trackDuration = duration;
            trackTarget = target;
            _UpdateHandlers(EVENT_VIDEO_TRACKING_UPDATE);
        }

        void _UpdateAVSync(bool state)
        {
            autoInternalAVSync = state;
            if (videoMux)
                videoMux._SetAVSync(autoInternalAVSync);
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
        }

        void _UpdateLastUrl()
        {
            if (_syncUrl == currentUrl)
                return;

            lastUrl = currentUrl;
            currentUrl = _syncUrl;
            if (sourceManager)
                currentUrlSource = sourceManager._GetSource(_syncUrlSourceIndex);

            _UpdateHandlers(EVENT_VIDEO_INFO_UPDATE);
        }

        // Debug

        void DebugLog(string message)
        {
            if (debugLogging)
                Debug.Log("[VideoTXL:SyncPlayer] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SyncPlayer", message);
        }

        void DebugError(string message, bool force = false)
        {
            if (debugLogging || force)
                Debug.LogError("[VideoTXL:SyncPlayer] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SyncPlayer", message);
        }

        void DebugTrace(string message)
        {
            DebugLog(message);
        }

        public void _SetDebugState(DebugState debug)
        {
            if (debugState)
            {
                debugState._Unregister(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
                debugState = null;
            }

            if (!debug)
                return;

            debugState = debug;
            debugState._Register(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
            debugState._SetContext(this, nameof(_InternalUpdateDebugState), "SyncPlayer");
        }

        public void _InternalUpdateDebugState()
        {
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            debugState._SetValue("isQuest", IsQuest.ToString());
            debugState._SetValue("owner", Utilities.IsValid(owner) ? owner.displayName : "--");
            debugState._SetValue("currentUrlSource", Utilities.IsValid(currentUrlSource) ? currentUrlSource.ToString() : "--");
            debugState._SetValue("syncVideoSource", _syncVideoSource.ToString());
            debugState._SetValue("syncVideoSourceOverride", _syncVideoSourceOverride.ToString());
            debugState._SetValue("syncUrl", _syncUrl.ToString());
            debugState._SetValue("syncQuestUrl", _syncQuestUrl.ToString());
            debugState._SetValue("syncVideoNumber", _syncVideoNumber.ToString());
            debugState._SetValue("loadedVideoNumber", _loadedVideoNumber.ToString());
            debugState._SetValue("syncPlaybackNumber", _syncPlaybackNumber.ToString());
            debugState._SetValue("syncOwnerPlaying", _syncOwnerPlaying.ToString());
            debugState._SetValue("syncOwnerPaused", _syncOwnerPaused.ToString());
            debugState._SetValue("syncVideoStartNetworkTime", _syncVideoStartNetworkTime.ToString());
            debugState._SetValue("syncVideoExpectedEndTime", _syncVideoExpectedEndTime.ToString());
            debugState._SetValue("syncLocked", _syncLocked.ToString());
            debugState._SetValue("syncHoldVideos", _syncHoldVideos.ToString());
            debugState._SetValue("overrideLock", _overrideLock.ToString());
            debugState._SetValue("playerState", playerState.ToString());
            debugState._SetValue("lastErrorCode", lastErrorCode.ToString());
            debugState._SetValue("lastVideoPosition", _lastVideoPosition.ToString());
            debugState._SetValue("videoTargetTime", _videoTargetTime.ToString());
            debugState._SetValue("waitForSync", _waitForSync.ToString());
            debugState._SetValue("holdReadyState", _holdReadyState.ToString());
            debugState._SetValue("heldVideoReady", _heldVideoReady.ToString());
            debugState._SetValue("lastSyncTime", _lastSyncTime.ToString());
            debugState._SetValue("pendingLoadTime", _pendingLoadTime.ToString());
            debugState._SetValue("seekableSource", seekableSource.ToString());
            debugState._SetValue("trackDuration", trackDuration.ToString());
            debugState._SetValue("trackPosition", trackPosition.ToString());
            debugState._SetValue("hasAccessControl", _hasAccessControl.ToString());
            debugState._SetValue("hasSustainZone", _hasSustainZone.ToString());
            debugState._SetValue("inSustainZone", _inSustainZone.ToString());
        }
    }
}
