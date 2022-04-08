
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Texel
{
    [AddComponentMenu("VideoTXL/Sync Player")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class SyncPlayer : UdonSharpBehaviour
    {
        [Tooltip("A proxy for dispatching video-related events to other listening behaviors, such as a screen manager")]
        public VideoPlayerProxy dataProxy;

        [Header("Optional Components")]
        [Tooltip("Pre-populated playlist to iterate through.  If default URL is set, the playlist will be disabled by default, otherwise it will auto-play.")]
        public Playlist playlist;

        [Tooltip("Set of input URLs to remap to alternate URLs on a per-platform basis")]
        public UrlRemapper urlRemapper;

        [Tooltip("Control access to player controls based on player type or whitelist")]
        public AccessControl accessControl;

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Tooltip("Optional trigger zone the player must be in to sustain playback.  Disables playing audio on world load.")]
        [HideInInspector]
        public CompoundZoneTrigger playbackZone;

        //[Tooltip("Optional component to start or stop player based on common trigger events")]
        //public TriggerManager triggerManager;

        [Header("Default Options")]
        [Tooltip("Optional default URL to play on world load")]
        public VRCUrl defaultUrl;

        [Tooltip("Whether player controls are locked to master and instance owner by default")]
        public bool defaultLocked = false;

        [Tooltip("Write out video player events to VRChat log")]
        public bool debugLogging = true;

        [Tooltip("Automatically loop track when finished")]
        public bool loop = false;

        [Tooltip("Whether to keep playing the same URL if an error occurs")]
        public bool retryOnError = true;

        [Header("Sync Options")]
        [Tooltip("How often to check if video playback has fallen out of sync")]
        public float syncFrequency = 5;

        [Tooltip("How far video playback must have fallen out of sync to perform a correction")]
        public float syncThreshold = 1;

        [Tooltip("Experimental.  Video playback will periodically resync audio and video.  May cause stuttering or temporary playback failure.")]
        public bool autoInternalAVSync = false;

        [Header("Internal Objects")]
        [Tooltip("AVPro video player component")]
        public VRCAVProVideoPlayer avProVideo;
        [Tooltip("Unity video player component")]
        public VRCUnityVideoPlayer unityVideo;

        float retryTimeout = 6;
        //float syncLatchUpdateFrequency = 0.2f;

        [UdonSynced]
        short _syncVideoSource = VIDEO_SOURCE_NONE;
        [UdonSynced]
        short _syncVideoSourceOverride = VIDEO_SOURCE_NONE;

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
        bool _syncLocked = true;

        [UdonSynced]
        bool _syncRepeatPlaylist;

        [NonSerialized]
        public int localPlayerState = PLAYER_STATE_STOPPED;
        [NonSerialized]
        public VideoError localLastErrorCode;

        BaseVRCVideoPlayer _currentPlayer;

        float _lastVideoPosition = 0;
        float _videoTargetTime = 0;

        bool _waitForSync;
        bool _holdReadyState = false;
        bool _heldVideoReady = false;
        float _lastSyncTime;
        float _playStartTime = 0;

        float _pendingLoadTime = 0;
        float _pendingPlayTime = 0;
        VRCUrl _pendingPlayUrl;

        bool _hasAccessControl = false;
        bool _hasSustainZone = false;
        bool _inSustainZone = false;
        bool _initDeserialize = false;

        bool init = false;

        // Realtime state

        [NonSerialized]
        public bool seekableSource;
        [NonSerialized]
        public float trackDuration;
        [NonSerialized]
        public float trackPosition;
        [NonSerialized]
        public float previousTrackPosition;
        [NonSerialized]
        public bool locked;
        [NonSerialized]
        public bool repeatPlaylist;
        [NonSerialized]
        public VRCUrl currentUrl = VRCUrl.Empty;
        [NonSerialized]
        public VRCUrl lastUrl = VRCUrl.Empty;
        [NonSerialized]
        public VRCUrl queuedUrl = VRCUrl.Empty;

        // Constants

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        const int SCREEN_MODE_NORMAL = 0;
        const int SCREEN_MODE_LOGO = 1;
        const int SCREEN_MODE_LOADING = 2;
        const int SCREEN_MODE_ERROR = 3;

        const short VIDEO_SOURCE_NONE = 0;
        const short VIDEO_SOURCE_AVPRO = 1;
        const short VIDEO_SOURCE_UNITY = 2;

        const int GAME_MODE_PC = 0;
        const int GAME_MODE_QUEST = 1;

        void Start()
        {
            _EnsureInit();
        }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;

            _Init();
        }

        void _Init()
        {
            dataProxy._Init();

#if UNITY_ANDROID
            dataProxy.quest = true;
#endif

            if (dataProxy.quest)
                DebugLog("Detected Quest platform");
            else if (Utilities.IsValid(Networking.LocalPlayer))
                DebugLog("Detected " + (Networking.LocalPlayer.IsUserInVR() ? "PC VR" : "PC Desktop") + " Platform");

            _hasAccessControl = Utilities.IsValid(accessControl);
            _hasSustainZone = Utilities.IsValid(playbackZone);
            if (_hasSustainZone)
            {
                _inSustainZone = playbackZone._LocalPlayerInZone();
                playbackZone._Register((UdonBehaviour)(Component)this, "_PlaybackZoneEnter", "_PlaybackZoneExit", null);
            }
            else
                _inSustainZone = true;

            if (Utilities.IsValid(urlRemapper))
                urlRemapper._SetGameMode(dataProxy.quest ? GAME_MODE_QUEST : GAME_MODE_PC);

            if (Utilities.IsValid(avProVideo))
            {
                avProVideo.Loop = false;
                avProVideo.Stop();
                avProVideo.EnableAutomaticResync = false;
                _currentPlayer = avProVideo;
            }
            if (Utilities.IsValid(unityVideo))
            {
                unityVideo.Loop = false;
                unityVideo.Stop();
                unityVideo.EnableAutomaticResync = false;
                _currentPlayer = unityVideo;
            }

            _UpdateVideoSource(VIDEO_SOURCE_AVPRO, _syncVideoSourceOverride);
            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            if (Utilities.IsValid(playlist))
                playlist._RegisterListChange(this, "_OnPlaylistListChange");

            if (Networking.IsOwner(gameObject))
            {
                _syncLocked = defaultLocked;
                _syncRepeatPlaylist = loop;
                _UpdateLockState(_syncLocked);
                _UpdateRepeatMode(_syncRepeatPlaylist);
                RequestSerialization();

                if (Utilities.IsValid(playlist))
                    playlist._Init();
            }

            if (Networking.IsOwner(gameObject))
            {
                if (Utilities.IsValid(playlist) && playlist.trackCount > 0)
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
            _inSustainZone = true;
            _StartVideoLoad();
        }

        public void _PlaybackZoneExit()
        {
            _inSustainZone = false;
            _StopVideo();
        }

        public void _OnPlaylistListChange()
        {
            DebugLog("Playlist track list changed");
            dataProxy._EmitPlaylistUpdate();

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
            if (localPlayerState == PLAYER_STATE_PLAYING || localPlayerState == PLAYER_STATE_LOADING)
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
            if (!seekableSource || localPlayerState != PLAYER_STATE_PLAYING)
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncOwnerPaused = !_syncOwnerPaused;

            if (_syncOwnerPaused)
            {
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _currentPlayer.GetTime();
                _videoTargetTime = _currentPlayer.GetTime();
                _VideoPause();
            }
            else
                _VideoPlay();

            _UpdatePlayerPaused(_syncOwnerPaused);

            RequestSerialization();
        }

        public void _TriggerLock()
        {
            if (!_IsAdmin())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncLocked = !_syncLocked;
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

        public void _SetSourceMode(short mode)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _syncVideoSourceOverride = mode;
            _UpdateVideoSource(_syncVideoSource, _syncVideoSourceOverride);

            RequestSerialization();
        }

        public void _Resync()
        {
            _ForceResync();
        }

        public void _ChangeUrl(VRCUrl url)
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
                _VideoPlay();

            _UpdatePlayerHold(false, false);
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
            if (localPlayerState != PLAYER_STATE_PLAYING)
                return;
            if (!seekableSource)
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Allowing AVPro to set time directly to end of track appears to trigger deadlock sometimes
            float duration = _currentPlayer.GetDuration();
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
            _pendingPlayTime = 0;
            if (!_IsUrlValid(url))
                return;

            DebugLog("Play video " + url);
            bool isOwner = Networking.IsOwner(gameObject);
            if (!isOwner && !_TakeControl())
                return;

            string urlStr = url.Get();

            _syncVideoSource = _syncVideoSourceOverride;
            if (_syncVideoSource == VIDEO_SOURCE_NONE)
            {
                if (_IsAutoVideoSource(urlStr))
                    _syncVideoSource = VIDEO_SOURCE_UNITY;
                else
                    _syncVideoSource = VIDEO_SOURCE_AVPRO;
            }

            _UpdateVideoSource(_syncVideoSource, _syncVideoSourceOverride);

            _syncUrl = url;
            _syncQuestUrl = questUrl;
            _syncVideoNumber += isOwner ? 1 : 2;
            _loadedVideoNumber = _syncVideoNumber;
            _syncOwnerPlaying = false;
            _syncOwnerPaused = false;

            _syncVideoStartNetworkTime = float.MaxValue;
            RequestSerialization();

            _videoTargetTime = _ParseTimeFromUrl(urlStr);
            _UpdateLastUrl();
            _UpdateQueuedUrlData();

            // Conditional player stop to try and avoid piling on AVPro at end of track
            // and maybe triggering bad things
            if (localPlayerState == PLAYER_STATE_PLAYING && _currentPlayer.IsPlaying && seekableSource)
            {
                float duration = _currentPlayer.GetDuration();
                float remaining = duration - _currentPlayer.GetTime();
                if (remaining > 2)
                    _VideoStop();
            }

            _StartVideoLoadDelay(delay);
        }

        public void _LoopVideo()
        {
            _PlayVideo(_syncUrl);
        }

        public void _PlayQueuedUrl()
        {
            VRCUrl url = _syncQueuedUrl;
            _syncQueuedUrl = VRCUrl.Empty;
            _PlayVideo(url);
        }

        public void _PlayPlaylistUrl()
        {
            _syncQueuedUrl = VRCUrl.Empty;
            if (Utilities.IsValid(playlist) && Utilities.IsValid(playlist.playlistData))
            {
                if (!playlist.PlaylistEnabled)
                    playlist._SetEnabled(true);

                if (playlist.holdOnReady && Networking.IsOwner(gameObject))
                    _HoldNextVideo();

                _PlayVideoFallback(playlist._GetCurrent(), playlist._GetCurrentQuest());
            }
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
            if (urlStr.Contains("vrcdn"))
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

            _UpdatePlayerState(PLAYER_STATE_LOADING);

            //#if !UNITY_EDITOR
            VRCUrl url = _syncUrl;
            if (dataProxy.quest && _syncQuestUrl != null && _syncQuestUrl != VRCUrl.Empty)
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

            _VideoLoadURL(url);
            //#endif
        }

        public void _StopVideo()
        {
            DebugLog("Stop video");

            if (seekableSource)
                _lastVideoPosition = _currentPlayer.GetTime();

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            _VideoStop();
            _videoTargetTime = 0;
            _pendingPlayTime = 0;
            _pendingLoadTime = 0;
            _playStartTime = 0;

            if (Networking.IsOwner(gameObject))
            {
                _syncVideoStartNetworkTime = 0;
                _syncOwnerPlaying = false;
                _syncOwnerPaused = false;
                _syncUrl = VRCUrl.Empty;
                RequestSerialization();
            }
        }

        public void _OnVideoReady()
        {
            float position = _currentPlayer.GetTime();
            float duration = _currentPlayer.GetDuration();
            DebugLog("Video ready, duration: " + duration + ", position: " + position);

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;
            dataProxy.seekableSource = seekableSource;
            _UpdateTracking(position, position, duration);

            // If player is owner: play video
            // If Player is remote:
            //   - If owner playing state is already synced, play video
            //   - Otherwise, wait until owner playing state is synced and play later in update()
            //   TODO: Streamline by always doing this in update instead?

            if (Networking.IsOwner(gameObject))
            {
                if (!_holdReadyState)
                    _VideoPlay();
                else
                    _UpdatePlayerHold(true, true);
            }
            else
            {
                // TODO: Stream bypass owner
                if (_syncOwnerPlaying)
                    _VideoPlay();
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

                _UpdatePlayerState(PLAYER_STATE_PLAYING);
                _UpdatePlayerPaused(false);
                _playStartTime = Time.time;

                _syncOwnerPlaying = true;
                _syncOwnerPaused = false;
                RequestSerialization();

                //if (!paused)
                _VideoSetTime(_videoTargetTime);

                SyncVideoImmediate();
            }
            else
            {
                if (!_syncOwnerPlaying || _syncOwnerPaused)
                {
                    // TODO: Owner bypass
                    _VideoPause();
                    _waitForSync = true;

                    _UpdatePlayerPaused(_syncOwnerPaused);
                }
                else
                {
                    _UpdatePlayerState(PLAYER_STATE_PLAYING);
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
            dataProxy.seekableSource = false;

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            //DebugLog("Video end");
            _lastVideoPosition = 0;

            if (Networking.IsOwner(gameObject))
            {
                bool hasPlaylist = Utilities.IsValid(playlist) && playlist.PlaylistEnabled;
                if (_IsUrlValid(_syncQueuedUrl))
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                else if (hasPlaylist && playlist.autoAdvance && playlist._MoveNext())
                    SendCustomEventDelayedFrames("_PlayPlaylistUrl", 1);
                else if (!hasPlaylist && _syncRepeatPlaylist)
                    SendCustomEventDelayedFrames("_LoopVideo", 1);
                else
                {
                    _syncVideoStartNetworkTime = 0;
                    _syncOwnerPlaying = false;
                    RequestSerialization();
                }
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

        public void _OnVideoError(VideoError videoError)
        {
            if (localPlayerState == PLAYER_STATE_STOPPED)
                return;

            _VideoStop();

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
            bool shouldFallback = videoError == VideoError.PlayerError && _syncVideoSourceOverride == VIDEO_SOURCE_NONE && _syncVideoSource == VIDEO_SOURCE_UNITY;

            _UpdatePlayerStateError(videoError);
            if (shouldFallback)
                _SetStreamFallback();

            if (Networking.IsOwner(gameObject))
            {
                if (shouldFallback)
                {
                    DebugLog("Retrying URL in stream mode");

                    _UpdateVideoSource(VIDEO_SOURCE_AVPRO, _syncVideoSourceOverride);
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

        public bool _IsAdmin()
        {
            if (_hasAccessControl)
                return accessControl._LocalHasAccess();

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return false;

            return player.isMaster || player.isInstanceOwner;
        }

        public bool _CanTakeControl()
        {
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

            _UpdateVideoSource(_syncVideoSource, _syncVideoSourceOverride);
            _UpdateLockState(_syncLocked);
            _UpdateRepeatMode(_syncRepeatPlaylist);
            _UpdateQueuedUrlData();

            if (_syncVideoNumber == _loadedVideoNumber)
            {
                if (_inSustainZone)
                {
                    if (localPlayerState == PLAYER_STATE_PLAYING && !_syncOwnerPlaying)
                        SendCustomEventDelayedFrames("_StopVideo", 1);
                    else if (dataProxy.paused && !_syncOwnerPaused)
                    {
                        _VideoPlay();
                        _UpdatePlayerPaused(false);
                    }
                    else if (localPlayerState == PLAYER_STATE_PLAYING && _syncOwnerPaused)
                    {
                        _VideoPause();
                        _UpdatePlayerPaused(true);
                    }
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

            if (_pendingPlayTime > 0 && time > _pendingPlayTime)
                _PlayVideo(_pendingPlayUrl);
            if (_pendingLoadTime > 0 && Time.time > _pendingLoadTime)
                _StartVideoLoad();

            if (seekableSource && localPlayerState == PLAYER_STATE_PLAYING)
            {
                float position = Mathf.Floor(_currentPlayer.GetTime());
                if (position != previousTrackPosition)
                {
                    previousTrackPosition = position;
                    float target = position;
                    //if (syncLatched)
                    //    target = GetTargetTime();

                    _UpdateTracking(position, target, _currentPlayer.GetDuration());
                }
            }

            if (seekableSource && _syncOwnerPaused)
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _currentPlayer.GetTime();


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
            _UpdatePlayerState(PLAYER_STATE_PLAYING);

            _waitForSync = false;
            _VideoPlay();

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

            float duration = _currentPlayer.GetDuration();
            float current = _currentPlayer.GetTime();
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
            _VideoSetTime(offsetTime);
        }

        /*void SyncVideo()
        {
            if (seekableSource && !syncLatched && !syncReadback)
            {
                float duration = _currentPlayer.GetDuration();
                float current = _currentPlayer.GetTime();
                float serverTime = (float)Networking.GetServerTimeInSeconds();
                float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
                if (Mathf.Abs(current - offsetTime) > syncThreshold && (duration - current) > 2)
                {
                    _currentPlayer.SetTime(offsetTime);
                    DebugLog($"Sync time (off by {current - offsetTime}s) [net={serverTime}, sync={_syncVideoStartNetworkTime}, cur={current}]");

                    syncReadback = true;
                    SendCustomEventDelayedSeconds("_SyncReadback", 1);

                    /*float readbackTime = _currentPlayer.GetTime();
                    if (offsetTime - readbackTime > 1)
                    {
                        if (Time.time - syncTime2 < 30)
                        {
                            DebugLog("Excessive sync drift, forcing full resync");
                            SendCustomEventDelayedFrames("_ForceResync", 1);
                            return;
                        }

                        DebugLog($"Starting extended synchronization (target={offsetTime}, readback={readbackTime})");
                        syncLatched = true;
                        _UpdatePlayerSyncing(true);
                        SendCustomEventDelayedSeconds("_SyncLatch", syncLatchUpdateFrequency);
                    }/
                }
            }
        }

        public void _SyncReadback()
        {
            syncReadback = false;

            float duration = _currentPlayer.GetDuration();
            float serverTime = (float)Networking.GetServerTimeInSeconds();
            float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
            float readbackTime = _currentPlayer.GetTime();

            if (offsetTime - readbackTime <= syncThreshold)
                return;

            // If we've had to perform extended sync twice in a short period of time, player probably can't read at 1x, so resync
            if (Time.time - syncTime2 < 30)
            {
                DebugLog("Excessive sync drift, forcing full resync");
                SendCustomEventDelayedFrames("_ForceResync", 1);
                return;
            }

            DebugLog($"Starting extended synchronization (target={offsetTime}, readback={readbackTime})");
            syncLatched = true;
            _UpdatePlayerSyncing(true);
            SendCustomEventDelayedSeconds("_SyncLatch", syncLatchUpdateFrequency);
        }

        bool syncLatched = false;
        bool syncReadback = false;
        float syncTime1 = 0;
        float syncTime2 = 0;
        float syncLatchRate = 0;

        public void _SyncLatch()
        {
            syncLatched = false;
            if (localPlayerState != PLAYER_STATE_PLAYING)
            {
                DebugLog("Not playing, canceling synchronization");
                _UpdatePlayerSyncing(false);
                return;
            }

            float duration = _currentPlayer.GetDuration();
            float current = _currentPlayer.GetTime();
            float serverTime = (float)Networking.GetServerTimeInSeconds();
            float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
            if (Mathf.Abs(current - offsetTime) > syncThreshold && (duration - current) > 2)
            {
                _currentPlayer.SetTime(offsetTime);
                float readbackTime = _currentPlayer.GetTime();
                float syncLatchProgress = readbackTime - current;
                syncLatchRate = syncLatchProgress / syncLatchUpdateFrequency;

                if (offsetTime - readbackTime > 1)
                {
                    /*if (syncLatchRate < 5)
                    {
                        DebugLog($"Excessive slow sync rate: {syncLatchRate} ({current}, {readbackTime}), forcing full resync");
                        _UpdatePlayerSyncing(false);
                        SendCustomEventDelayedFrames("_ForceResync", 1);
                        return;
                    }/

                    syncLatched = true;
                    SendCustomEventDelayedSeconds("_SyncLatch", syncLatchUpdateFrequency);
                }
            }

            if (!syncLatched)
            {
                DebugLog("Synchronized");
                _UpdatePlayerSyncing(false);
                syncTime2 = syncTime1;
                syncTime1 = Time.time;
            }
        }*/

        public void _AVSyncStart()
        {
            if (_currentPlayer == avProVideo)
            {
                DebugLogAs("AVPro", "Auto AV Sync");
                avProVideo.EnableAutomaticResync = true;
                SendCustomEventDelayedSeconds("_AVSyncEndAVPro", 1);
            }
            else if (_currentPlayer == unityVideo)
            {
                DebugLogAs("UnityVideo", "Auto AV Sync");
                unityVideo.EnableAutomaticResync = true;
                SendCustomEventDelayedSeconds("_AVSyncEndUnity", 1);
            }
            else
                SendCustomEventDelayedSeconds("_AVSyncStart", 30);
        }

        public void _AVSyncEndAVPro()
        {
            avProVideo.EnableAutomaticResync = false;
            SendCustomEventDelayedSeconds("_AVSyncStart", 30);
        }

        public void _AVSyncEndUnity()
        {
            unityVideo.EnableAutomaticResync = false;
            SendCustomEventDelayedSeconds("_AVSyncStart", 30);
        }

        float GetTargetTime()
        {
            float duration = _currentPlayer.GetDuration();
            float serverTime = (float)Networking.GetServerTimeInSeconds();
            return Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
        }

        public void _ForceResync()
        {
            bool isOwner = Networking.IsOwner(gameObject);
            if (isOwner)
            {
                if (seekableSource)
                {
                    float startTime = _videoTargetTime;
                    if (_currentPlayer.IsPlaying)
                        startTime = Mathf.Max(_currentPlayer.GetTime(), GetTargetTime());

                    _StartVideoLoad();
                    _videoTargetTime = startTime;
                }
                else
                    _StartVideoLoad();

                return;
            }

            _VideoStop();
            if (_syncOwnerPlaying)
                _StartVideoLoad();
        }

        void _VideoPlay()
        {
            DebugLogVideo("Play");
            _currentPlayer.Play();
        }

        void _VideoStop()
        {
            DebugLogVideo("Stop");
            if (Utilities.IsValid(_currentPlayer))
                _currentPlayer.Stop();
        }

        void _VideoPause()
        {
            DebugLogVideo("Pause");
            _currentPlayer.Pause();
        }

        void _VideoLoadURL(VRCUrl url)
        {
            DebugLogVideo($"Load URL: {url}");
            _currentPlayer.LoadURL(url);
        }

        void _VideoSetTime(float time)
        {
            DebugLogVideo($"Set time: {time}");
            _currentPlayer.SetTime(time);
        }

        public void _StopAVPro()
        {
            if (Utilities.IsValid(avProVideo))
            {
                DebugLogAs("AVPro", "Stop");
                avProVideo.Stop();
            }
        }

        public void _StopUnity()
        {
            if (Utilities.IsValid(unityVideo))
            {
                DebugLogAs("UnityVideo", "Stop");
                unityVideo.Stop();
            }
        }

        void _UpdateVideoSource(int source, int sourceOverride)
        {
            bool change = false;
            int oldSourceOverride = dataProxy.playerSourceOverride;

            if (oldSourceOverride != sourceOverride)
            {
                _StopVideo();

                dataProxy.playerSourceOverride = (short)sourceOverride;

                switch (dataProxy.playerSourceOverride)
                {
                    case VIDEO_SOURCE_AVPRO:
                        DebugLog("Setting video source override to AVPro");
                        _currentPlayer = avProVideo;
                        break;
                    case VIDEO_SOURCE_UNITY:
                        DebugLog("Setting video source override to Unity");
                        _currentPlayer = unityVideo;
                        break;
                    case VIDEO_SOURCE_NONE:
                    default:
                        DebugLog("Setting video source override to Auto");
                        _currentPlayer = unityVideo;
                        break;
                }

                change = true;
            }

            if (dataProxy.playerSource != source)
            {
                if (oldSourceOverride == sourceOverride)
                {
                    switch (dataProxy.playerSource)
                    {
                        case VIDEO_SOURCE_AVPRO: SendCustomEventDelayedFrames("_StopAVPro", 1); break;
                        case VIDEO_SOURCE_UNITY: SendCustomEventDelayedFrames("_StopUnity", 1); break;
                    }
                }

                dataProxy.playerSource = (short)source;

                switch (dataProxy.playerSource)
                {
                    case VIDEO_SOURCE_AVPRO:
                        DebugLog("Switching video source to AVPro");
                        _currentPlayer = avProVideo;
                        break;
                    case VIDEO_SOURCE_UNITY:
                        DebugLog("Switching video source to Unity");
                        _currentPlayer = unityVideo;
                        break;
                }

                change = true;
            }

            if (change)
                dataProxy._EmitStateUpdate();
        }

        void _UpdatePlayerState(int state)
        {
            localPlayerState = state;
            dataProxy.playerState = state;
            dataProxy.streamFallback = false;

            if (state != PLAYER_STATE_PLAYING)
            {
                dataProxy.paused = false;
                dataProxy.syncing = false;
            }

            dataProxy._EmitStateUpdate();
        }

        void _UpdatePlayerPaused(bool paused)
        {
            dataProxy.paused = paused;
            dataProxy._EmitStateUpdate();
        }

        void _UpdatePlayerSyncing(bool syncing)
        {
            dataProxy.syncing = syncing;
            dataProxy._EmitStateUpdate();
        }

        void _UpdatePlayerHold(bool holding, bool ready)
        {
            _holdReadyState = holding;
            _heldVideoReady = ready;

            dataProxy.heldReady = _heldVideoReady;
            dataProxy._EmitStateUpdate();
        }

        void _UpdatePlayerStateError(VideoError error)
        {
            localPlayerState = PLAYER_STATE_ERROR;
            localLastErrorCode = error;
            dataProxy.playerState = PLAYER_STATE_ERROR;
            dataProxy.lastErrorCode = error;
            dataProxy._EmitStateUpdate();
        }

        void _SetStreamFallback()
        {
            dataProxy.streamFallback = true;
            dataProxy._EmitStateUpdate();
        }

        void _UpdateLockState(bool state)
        {
            locked = state;
            dataProxy.locked = state;
            dataProxy._EmitLockUpdate();
        }

        void _UpdateTracking(float position, float target, float duration)
        {
            trackPosition = position;
            trackDuration = duration;
            dataProxy.trackPosition = position;
            dataProxy.trackTarget = target;
            dataProxy.trackDuration = duration;
            dataProxy._EmitTrackingUpdate();
        }

        void _UpdateRepeatMode(bool state)
        {
            repeatPlaylist = state;
            dataProxy.repeatPlaylist = state;
            dataProxy._EmitPlaylistUpdate();
        }

        void _UpdateQueuedUrlData()
        {
            if (_syncQueuedUrl == queuedUrl)
                return;

            queuedUrl = _syncQueuedUrl;
            dataProxy.queuedUrl = queuedUrl;
            dataProxy._EmitInfoUpdate();
        }

        void _UpdateLastUrl()
        {
            if (_syncUrl == currentUrl)
                return;

            lastUrl = currentUrl;
            currentUrl = _syncUrl;
            dataProxy.currentUrl = currentUrl;
            dataProxy.lastUrl = lastUrl;
            dataProxy._EmitInfoUpdate();
        }

        // Debug

        void DebugLog(string message)
        {
            if (debugLogging)
                Debug.Log("[VideoTXL:SyncPlayer] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SyncPlayer", message);
        }

        void DebugLogVideo(string message)
        {
            DebugLogAs(_currentPlayer == avProVideo ? "AVPro" : "UnityVideo", message);
        }

        void DebugLogAs(string comp, string message)
        {
            if (debugLogging)
                Debug.Log($"[VideoTXL:{comp}] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write(comp, message);
        }
    }
}
