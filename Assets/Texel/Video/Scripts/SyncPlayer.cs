
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Texel
{
    [AddComponentMenu("VideoTXL/Sync Player")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class SyncPlayer : TXLVideoPlayer
    {
        public Playlist playlist;
        public UrlRemapper urlRemapper;
        public AccessControl accessControl;

        [HideInInspector]
        public CompoundZoneTrigger playbackZone;
        public ZoneMembership playbackZoneMembership;

        public VRCUrl defaultUrl;
        public bool defaultLocked = false;
        public bool loop = false;
        public bool retryOnError = true;
        public bool autoFailbackToAVPro = true;

        public float syncFrequency = 5;
        public float syncThreshold = 1;
        public bool autoInternalAVSync = false;

        [SerializeField]
        public short defaultVideoSource = VideoSource.VIDEO_SOURCE_NONE;

        public bool debugLogging = true;
        public DebugLog debugLog;
        public DebugState debugState;

        float retryTimeout = 6;
        //float loadWaitTime = 2;
        //float syncLatchUpdateFrequency = 0.2f;

        [UdonSynced]
        short _syncVideoSource = VideoSource.VIDEO_SOURCE_NONE;
        [UdonSynced]
        short _syncVideoSourceOverride = VideoSource.VIDEO_SOURCE_NONE;
        [UdonSynced]
        short _syncScreenFit = SCREEN_FIT;

        [UdonSynced]
        VRCUrl _syncUrl = VRCUrl.Empty;
        [UdonSynced]
        VRCUrl _syncQueuedUrl = VRCUrl.Empty;
        [UdonSynced]
        VRCUrl _syncQuestUrl = VRCUrl.Empty;

        [UdonSynced]
        int _syncVideoNumber;
        int _loadedVideoNumber;

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

        [UdonSynced]
        bool _syncRepeatPlaylist;

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
        float _playStartTime = 0;
        bool _overrideLock = false;
        bool _suppressSourceUpdate = false;

        float _pendingLoadTime = 0;

        bool _hasAccessControl = false;
        bool _hasSustainZone = false;
        bool _inSustainZone = false;
        bool _initDeserialize = false;

        // Realtime state

        [NonSerialized]
        public float previousTrackPosition;

        protected override void _Init()
        {
            base._Init();

            if (IsQuest)
                DebugLog("Detected Quest platform");
            else if (Utilities.IsValid(Networking.LocalPlayer))
                DebugLog("Detected " + (Networking.LocalPlayer.IsUserInVR() ? "PC VR" : "PC Desktop") + " Platform");

            if (Utilities.IsValid(debugState))
                debugState._Regsiter(this, "_UpdateDebugState", "SyncPlayer");

            _hasAccessControl = Utilities.IsValid(accessControl);
            _hasSustainZone = Utilities.IsValid(playbackZoneMembership);
            if (_hasSustainZone)
            {
                if (Utilities.IsValid(Networking.LocalPlayer))
                    _inSustainZone = playbackZoneMembership._ContainsPlayer(Networking.LocalPlayer);
                playbackZoneMembership._RegisterAddPlayer(this, "_PlaybackZoneEnter", "playerArg");
                playbackZoneMembership._RegisterRemovePlayer(this, "_PlaybackZoneExit", "playerArg");
                //playbackZone._Register((UdonBehaviour)(Component)this, "_PlaybackZoneEnter", "_PlaybackZoneExit", null);
            }
            else
                _inSustainZone = true;

            if (Utilities.IsValid(urlRemapper))
                urlRemapper._SetGameMode(IsQuest ? UrlRemapper.GAME_MODE_QUEST : UrlRemapper.GAME_MODE_PC);

            videoMux._Register(VideoManager.VIDEO_READY_EVENT, this, "_OnVideoReady");
            videoMux._Register(VideoManager.VIDEO_START_EVENT, this, "_OnVideoStart");
            videoMux._Register(VideoManager.VIDEO_END_EVENT, this, "_OnVideoEnd");
            videoMux._Register(VideoManager.VIDEO_ERROR_EVENT, this, "_OnVideoError");
            videoMux._Register(VideoManager.SOURCE_CHANGE_EVENT, this, "_OnSourceChange");

            videoMux._UpdateLowLatency(VideoSource.LOW_LATENCY_ENABLE);

            _suppressSourceUpdate = true;
            _UpdateVideoSourceOverride(defaultVideoSource);
            videoMux._UpdateVideoSource(videoMux.ActiveSourceType);
            _suppressSourceUpdate = false;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            if (Utilities.IsValid(playlist))
                playlist._Register(Playlist.EVENT_LIST_CHANGE, this, "_OnPlaylistListChange");

            if (Networking.IsOwner(gameObject))
            {
                _syncLocked = defaultLocked;
                _syncRepeatPlaylist = loop;
                _UpdateLockState(_syncLocked);
                _UpdateRepeatMode(_syncRepeatPlaylist);
                RequestSerialization();

                if (Utilities.IsValid(playlist))
                    playlist._MasterInit();
            }

            if (Networking.IsOwner(gameObject))
            {
                if (Utilities.IsValid(playlist) && playlist.trackCount > 0 && playlist.autoAdvance)
                {
                    if (_IsUrlValid(defaultUrl))
                    {
                        playlist._SetEnabled(false);
                        _PlayVideoAfter(defaultUrl, 3);
                    }
                    else
                        SendCustomEventDelayedFrames("_PlayPlaylistUrl", 3);
                }
                else
                    _PlayVideoAfter(defaultUrl, 3);
            }

            if (autoInternalAVSync)
                SendCustomEventDelayedSeconds("_AVSyncStart", 1);

            if (!Networking.IsOwner(gameObject))
                SendCustomEventDelayedSeconds("_InitCheck", 5);
        }

        public void _InitCheck()
        {
            if (!_initDeserialize)
            {
                DebugLog("Deserialize not received in reasonable time");
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "RequestOwnerSync");
            }
        }

        public void _PlaybackZoneEnter()
        {
            if (playerArg == Networking.LocalPlayer)
            {
                _inSustainZone = true;

                VRCPlayerApi currentOwner = Networking.GetOwner(gameObject);
                if (!playbackZoneMembership._ContainsPlayer(currentOwner))
                    Networking.SetOwner(playerArg, gameObject);

                if (_syncVideoExpectedEndTime != 0)
                {
                    float serverTime = (float)Networking.GetServerTimeInSeconds();

                    // If time hasn't reached theoretical end of track, set starting time to where it would have been
                    // if there were still viewers in the playback zone
                    if (serverTime < _syncVideoExpectedEndTime)
                    {
                        _videoTargetTime = serverTime - _syncVideoStartNetworkTime;
                        DebugLog($"Playback enter: start at {_videoTargetTime}");
                        _StartVideoLoad();
                        return;
                    }

                    // Otherwise play next available track or stop, depending on queue/settings
                    if (Networking.IsOwner(playerArg, gameObject))
                    {
                        DebugLog($"Playback enter: is owner and track has ended");
                        _ConditionalPlayNext();
                        return;
                    }
                }

                DebugLog("Playback enter: no expected end time set");
                _StartVideoLoad();
            }
        }

        public void _PlaybackZoneExit()
        {
            if (playerArg == Networking.LocalPlayer)
            {
                _inSustainZone = false;

                if (Networking.IsOwner(gameObject))
                {
                    if (playbackZoneMembership._PlayerCount() > 0)
                    {
                        VRCPlayerApi nextPlayer = playbackZoneMembership._GetPlayer(0);
                        if (Utilities.IsValid(nextPlayer) && nextPlayer.IsValid())
                            Networking.SetOwner(nextPlayer, gameObject);
                    }
                }

                videoMux._VideoStop();
                _UpdatePlayerState(VIDEO_STATE_STOPPED);
            }
        }

        public void _OnPlaylistListChange()
        {
            DebugLog("Playlist track list changed");
            _UpdateHandlers(EVENT_VIDEO_PLAYLIST_UPDATE);

            if (Networking.IsOwner(gameObject))
            {
                if (!Utilities.IsValid(playlist))
                    return;

                if (playlist.PlaylistEnabled && playlist.holdOnReady)
                    _PlayPlaylistUrl();
            }
        }

        public void _TriggerPlay()
        {
            DebugLog("Trigger play");
            if (playerState == VIDEO_STATE_PLAYING || playerState == VIDEO_STATE_LOADING)
                return;

            _PlayVideo(_syncUrl);
        }

        public void _TriggerStop()
        {
            DebugLog("Trigger stop");
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _StopVideo();
        }

        public void _TriggerPause()
        {
            DebugLog("Trigger pause");
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!seekableSource || playerState != VIDEO_STATE_PLAYING)
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

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
            _SetLock(!_syncLocked);
        }

        public void _SetLock(bool state)
        {
            if (!_IsAdmin())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncLocked = state;
            _UpdateLockState(_syncLocked);
            RequestSerialization();
        }

        public void _TriggerRepeatMode()
        {
            DebugLog("Trigger repeat mode");
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncRepeatPlaylist = !_syncRepeatPlaylist;
            _UpdateRepeatMode(_syncRepeatPlaylist);
            RequestSerialization();
        }

        public override void _SetSourceMode(int mode)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _UpdateVideoSourceOverride(mode);
            if (mode != VideoSource.VIDEO_SOURCE_NONE)
                videoMux._UpdateVideoSource(_syncVideoSourceOverride);

            RequestSerialization();
        }

        public override void _SetSourceLatency(int latency)
        {
            videoMux._UpdateLowLatency(latency);
        }

        public override void _SetSourceResolution(int res)
        {
            videoMux._UpdatePreferredResolution(res);
        }

        public override void _SetScreenFit(int fit)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _syncScreenFit = (short)fit;
            _UpdateScreenFit(_syncScreenFit);

            RequestSerialization();
        }

        public override void _Resync()
        {
            _ForceResync();
        }

        public override void _ChangeUrl(VRCUrl url)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _syncQueuedUrl = VRCUrl.Empty;
            _PlayVideo(url);
        }

        public void _ChangeUrlQuestFallback(VRCUrl url, VRCUrl questUrl)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _syncQueuedUrl = VRCUrl.Empty;
            _PlayVideoFallback(url, questUrl);
        }

        public void _HoldNextVideo()
        {
            DebugLog("Holding next video");
            _UpdatePlayerHold(true, _heldVideoReady);
        }

        public void _ReleaseHold()
        {
            if (_heldVideoReady && Networking.IsOwner(gameObject))
                videoMux._VideoPlay();

            _UpdatePlayerHold(false, false);
        }

        public void _SkipNextAdvance()
        {
            if (Networking.IsOwner(gameObject))
                _skipAdvanceNextTrack = true;
        }

        public void _UpdateQueuedUrl(VRCUrl url)
        {
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncQueuedUrl = url;
            _UpdateQueuedUrlData();
        }

        public void _SetTargetTime(float time)
        {
            DebugLog($"Set target time: {time:N3}");
            if (_syncLocked && !_CanTakeControl())
                return;
            if (playerState != VIDEO_STATE_PLAYING)
                return;
            if (!seekableSource)
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Allowing AVPro to set time directly to end of track appears to trigger deadlock sometimes
            float duration = videoMux.VideoDuration;
            if (duration - time < 1)
            {
                bool hasPlaylist = Utilities.IsValid(playlist) && playlist.PlaylistEnabled;
                if (_IsUrlValid(_syncQueuedUrl))
                {
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                    return;
                }
                else if (hasPlaylist && playlist.autoAdvance && playlist._MoveNext())
                {
                    SendCustomEventDelayedFrames("_PlayPlaylistUrl", 1);
                    return;
                }
                else if (!hasPlaylist && _syncRepeatPlaylist)
                {
                    SendCustomEventDelayedFrames("_LoopVideo", 1);
                    return;
                }
                time = duration - 1;
            }

            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - time;
            _syncVideoExpectedEndTime = _syncVideoStartNetworkTime + duration;

            SyncVideoImmediate();
            RequestSerialization();
        }

        void _PlayVideo(VRCUrl url)
        {
            _PlayVideoAfter(url, 0);
        }

        void _PlayVideoFallback(VRCUrl url, VRCUrl questUrl)
        {
            _PlayVideoAfterFallback(url, questUrl, 0);
        }

        void _PlayVideoAfter(VRCUrl url, float delay)
        {
            _PlayVideoAfterFallback(url, VRCUrl.Empty, delay);
        }

        void _PlayVideoAfterFallback(VRCUrl url, VRCUrl questUrl, float delay)
        {
            if (!_IsUrlValid(url))
                return;

            DebugLog("Play video " + url);
            bool isOwner = Networking.IsOwner(gameObject);
            if (!isOwner && !_TakeControl())
                return;

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

            _suppressSourceUpdate = true;
            videoMux._UpdateVideoSource(_syncVideoSource);
            _suppressSourceUpdate = false;

            _syncUrl = url;
            _syncQuestUrl = questUrl;
            _syncVideoNumber += isOwner ? 1 : 2;
            _loadedVideoNumber = _syncVideoNumber;
            _syncOwnerPlaying = false;
            _syncOwnerPaused = false;
            _skipAdvanceNextTrack = false;

            _syncVideoStartNetworkTime = float.MaxValue;
            _syncVideoExpectedEndTime = 0;
            RequestSerialization();

            _videoTargetTime = _ParseTimeFromUrl(urlStr);
            _UpdateLastUrl();
            _UpdateQueuedUrlData();

            // Conditional player stop to try and avoid piling on AVPro at end of track
            // and maybe triggering bad things
            if (playerState == VIDEO_STATE_PLAYING && videoMux.VideoIsPlaying && seekableSource)
            {
                float duration = videoMux.VideoDuration;
                float remaining = videoMux.VideoTime;
                if (remaining > 2)
                    videoMux._VideoStop();
            }

            _StartVideoLoadDelay(delay);
        }

        public void _LoopVideo()
        {
            _overrideLock = true;
            _skipAdvanceNextTrack = false;

            _PlayVideo(_syncUrl);

            _overrideLock = false;
        }

        public void _PlayQueuedUrl()
        {
            _overrideLock = true;
            _skipAdvanceNextTrack = false;

            VRCUrl url = _syncQueuedUrl;
            _syncQueuedUrl = VRCUrl.Empty;
            _PlayVideo(url);

            _overrideLock = false;
        }

        public void _PlayPlaylistUrl()
        {
            _overrideLock = true;
            _skipAdvanceNextTrack = false;
            _syncQueuedUrl = VRCUrl.Empty;

            if (Utilities.IsValid(playlist) && Utilities.IsValid(playlist.playlistData))
            {
                if (!playlist.PlaylistEnabled)
                    playlist._SetEnabled(true);

                if (playlist.holdOnReady && Networking.IsOwner(gameObject))
                    _HoldNextVideo();

                _PlayVideoFallback(playlist._GetCurrent(), playlist._GetCurrentQuest());
            }

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
            if (urlStr.Contains("youtube.com/watch") || urlStr.Contains("youtu.be/"))
                return true;

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

            if (_syncUrl == null || _syncUrl.Get() == "")
                return;
            if (playerState == VIDEO_STATE_LOADING)
                return;

            _UpdatePlayerState(VIDEO_STATE_LOADING);

            VRCUrl url = _syncUrl;
            if (IsQuest && _syncQuestUrl != null && _syncQuestUrl != VRCUrl.Empty && _syncQuestUrl.Get().Trim() != "")
            {
                url = _syncQuestUrl;
                DebugLog("Loading Quest URL variant");
            }
            else if (Utilities.IsValid(urlRemapper))
            {
                url = urlRemapper._Remap(url);
                if (Utilities.IsValid(url) && _syncUrl.Get() != url.Get())
                    DebugLog("Remapped URL");
            }

            videoMux._VideoLoadURL(url);
        }

        public void _StopVideo()
        {
            DebugLog("Stop video");

            if (seekableSource)
                _lastVideoPosition = videoMux.VideoTime;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            videoMux._VideoStop();
            _videoTargetTime = 0;
            _pendingLoadTime = 0;
            _playStartTime = 0;

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
            float position = videoMux.VideoTime;
            float duration = videoMux.VideoDuration;
            DebugLog("Video ready, duration: " + duration + ", position: " + position);

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;
            _UpdateTracking(position, position, duration);

            // If player is owner: play video
            // If Player is remote:
            //   - If owner playing state is already synced, play video
            //   - Otherwise, wait until owner playing state is synced and play later in update()
            //   TODO: Streamline by always doing this in update instead?

            if (Networking.IsOwner(gameObject))
            {
                if (!_holdReadyState)
                    videoMux._VideoPlay();
                else
                    _UpdatePlayerHold(true, true);
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
            //DebugLog("Video start");

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
                _playStartTime = Time.time;

                _syncOwnerPlaying = true;
                _syncOwnerPaused = false;
                RequestSerialization();

                //if (!paused)
                videoMux._VideoSetTime(_videoTargetTime);

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
                    _playStartTime = Time.time;

                    SyncVideoImmediate();
                }
            }
        }

        public void _OnVideoEnd()
        {
            if (!seekableSource && Time.time - _playStartTime < 1)
            {
                Debug.Log("Video end encountered at start of stream, ignoring");
                return;
            }

            seekableSource = false;

            _UpdatePlayerState(VIDEO_STATE_STOPPED);

            //DebugLog("Video end");
            _lastVideoPosition = 0;

            _ConditionalPlayNext();
        }

        void _ConditionalPlayNext()
        {
            if (Networking.IsOwner(gameObject))
            {
                _overrideLock = true;

                bool hasPlaylist = Utilities.IsValid(playlist) && playlist.PlaylistEnabled;
                if (_IsUrlValid(_syncQueuedUrl))
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                else if (hasPlaylist && playlist.autoAdvance)
                {
                    if (_skipAdvanceNextTrack || playlist._MoveNext())
                        SendCustomEventDelayedFrames("_PlayPlaylistUrl", 1);
                    _skipAdvanceNextTrack = false;
                }
                else if (!hasPlaylist && _syncRepeatPlaylist)
                    SendCustomEventDelayedFrames("_LoopVideo", 1);
                else
                {
                    if (hasPlaylist && playlist.trackCatalogMode)
                    {
                        playlist._MoveTo(-1);
                        _UpdateHandlers(EVENT_VIDEO_PLAYLIST_UPDATE);
                    }

                    _syncUrl = VRCUrl.Empty;
                    _syncQuestUrl = VRCUrl.Empty;
                    _syncVideoStartNetworkTime = 0;
                    //_syncVideoExpectedEndTime = 0;
                    _syncOwnerPlaying = false;
                    RequestSerialization();
                }

                _overrideLock = false;
            }
        }

        // AVPro sends loop event but does not auto-loop, and setting time sometimes deadlocks player *sigh*
        public void _OnVideoLoop()
        {
            /*
            float current = _currentPlayer.GetTime();
            float duration = _currentPlayer.GetDuration();
            DebugLog($"Video loop duration={duration}, position={current}");

            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds();

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            _lastSyncTime = Time.realtimeSinceStartup;
            _currentPlayer.SetTime(0);
            */
        }

        public void _OnVideoError()
        {
            if (playerState == VIDEO_STATE_STOPPED)
                return;

            VideoError videoError = videoMux.LastError;
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

            DebugLog("Video stream failed: " + _syncUrl);
            DebugLog("Error code: " + code);

            // Try to fall back to AVPro if auto video failed (the youtube livestream problem)
            bool shouldFallback = autoFailbackToAVPro && videoError == VideoError.PlayerError && _syncVideoSourceOverride == VideoSource.VIDEO_SOURCE_NONE && _syncVideoSource == VideoSource.VIDEO_SOURCE_UNITY;

            _UpdatePlayerStateError(videoError);
            if (shouldFallback)
                _SetStreamFallback();

            if (Networking.IsOwner(gameObject))
            {
                if (shouldFallback)
                {
                    DebugLog("Retrying URL in stream mode");

                    // TODO: What if we don't have AVPro?
                    videoMux._UpdateVideoSource(VideoSource.VIDEO_SOURCE_AVPRO);
                    RequestSerialization();

                    _StartVideoLoadDelay(retryTimeout);
                    return;
                }

                if (retryOnError)
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
                }
            }
            else
            {
                _StartVideoLoadDelay(retryTimeout);
            }
        }

        public void _OnSourceChange()
        {
            DebugLog($"ONSOURCECHANGE {videoMux.ActiveSourceType}");
            playerSource = (short)videoMux.ActiveSourceType;
            _UpdateHandlers(EVENT_VIDEO_STATE_UPDATE);
            //dataProxy._EmitStateUpdate();

            if (!_suppressSourceUpdate)
                _ForceResync(true);
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

        public bool _TakeControl()
        {
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
            DebugLog($"Deserialize: video #{_syncVideoNumber}");

            if (Networking.IsOwner(gameObject))
            {
                DebugLog("But you're the owner.  This should not happen.");
                return;
            }

            _initDeserialize = true;

            videoMux._UpdateVideoSource(_syncVideoSource);
            _UpdateScreenFit(_syncScreenFit);
            _UpdateLockState(_syncLocked);
            _UpdateRepeatMode(_syncRepeatPlaylist);
            _UpdateQueuedUrlData();

            if (_syncVideoNumber == _loadedVideoNumber)
            {
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

                return;
            }

            // There was some code here to bypass load owner sync bla bla

            _loadedVideoNumber = _syncVideoNumber;
            _UpdateLastUrl();

            if (_inSustainZone)
            {
                DebugLog("Starting video load from sync");
                _StartVideoLoad();
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                DebugLog("Failed to sync");
                return;
            }
        }

        public void RequestOwnerSync()
        {
            DebugLog("RequestOwnerSync");
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

            DebugLog($"Sync video (off by {offset:N3}s) to {offsetTime:N3}");

            // Did we get into a situation where the player can't track?
            if (current == previousCurrent)
            {
                if (offsetTime - previousTarget > syncFrequency * .8f)
                {
                    DebugLog("Video did not advance during previous sync, forcing reload");
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

        void _UpdateScreenFit(short mode)
        {
            if (mode != screenFit)
            {
                DebugLog($"Setting screen fit to {mode}");
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

            if (state == VIDEO_STATE_PLAYING || state == VIDEO_STATE_STOPPED)
                _UpdatePlayerHold(false, false);

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

        void _UpdatePlayerHold(bool holding, bool ready)
        {
            _holdReadyState = holding;
            _heldVideoReady = ready;

            heldReady = _heldVideoReady;
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

        void _UpdateRepeatMode(bool state)
        {
            repeatPlaylist = state;
            _UpdateHandlers(EVENT_VIDEO_PLAYLIST_UPDATE);
        }

        void _UpdateQueuedUrlData()
        {
            if (_syncQueuedUrl == queuedUrl)
                return;

            queuedUrl = _syncQueuedUrl;
            _UpdateHandlers(EVENT_VIDEO_INFO_UPDATE);
        }

        void _UpdateLastUrl()
        {
            if (_syncUrl == currentUrl)
                return;

            lastUrl = currentUrl;
            currentUrl = _syncUrl;
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

        public void _UpdateDebugState()
        {
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            debugState._SetValue("owner", Utilities.IsValid(owner) ? owner.displayName : "--");
            debugState._SetValue("syncVideoSource", _syncVideoSource.ToString());
            debugState._SetValue("syncVideoSourceOverride", _syncVideoSourceOverride.ToString());
            debugState._SetValue("syncUrl", _syncUrl.ToString());
            debugState._SetValue("syncQuestUrl", _syncQuestUrl.ToString());
            debugState._SetValue("syncQueuedUrl", _syncQueuedUrl.ToString());
            debugState._SetValue("syncVideoNumber", _syncVideoNumber.ToString());
            debugState._SetValue("loadedVideoNumber", _loadedVideoNumber.ToString());
            debugState._SetValue("syncOwnerPlaying", _syncOwnerPlaying.ToString());
            debugState._SetValue("syncOwnerPaused", _syncOwnerPaused.ToString());
            debugState._SetValue("syncVideoStartNetworkTime", _syncVideoStartNetworkTime.ToString());
            debugState._SetValue("syncVideoExpectedEndTime", _syncVideoExpectedEndTime.ToString());
            debugState._SetValue("syncLocked", _syncLocked.ToString());
            debugState._SetValue("overrideLock", _overrideLock.ToString());
            debugState._SetValue("playerState", playerState.ToString());
            debugState._SetValue("lastErrorCode", lastErrorCode.ToString());
            debugState._SetValue("lastVideoPosition", _lastVideoPosition.ToString());
            debugState._SetValue("videoTargetTime", _videoTargetTime.ToString());
            debugState._SetValue("waitForSync", _waitForSync.ToString());
            debugState._SetValue("holdReadyState", _holdReadyState.ToString());
            debugState._SetValue("heldVideoReady", _heldVideoReady.ToString());
            debugState._SetValue("lastSyncTime", _lastSyncTime.ToString());
            debugState._SetValue("playStartTime", _playStartTime.ToString());
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
