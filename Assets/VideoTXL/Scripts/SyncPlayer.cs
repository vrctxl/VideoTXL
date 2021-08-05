
using System;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Sync Player")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncPlayer : UdonSharpBehaviour
    {
        [Tooltip("A proxy for dispatching video-related events to other listening behaviors, such as a screen manager")]
        public VideoPlayerProxy dataProxy;

        [Header("Optional Components")]
        [Tooltip("Pre-populated playlist to iterate through.  Overrides default URL option")]
        public Playlist playlist;

        [Tooltip("Control access to player controls based on player type or whitelist")]
        public AccessControl accessControl;

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        //[Tooltip("Optional component to control and synchronize player video screens and materials")]
        //public ScreenManager screenManager;
        //[Tooltip("Optional component to control and synchronize player audio sources")]
        //public VolumeController audioManager;
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

        [Header("Internal Objects")]
        [Tooltip("AVPro video player component")]
        public VRCAVProVideoPlayer avProVideo;

        float retryTimeout = 6;
        float syncFrequency = 5;
        float syncThreshold = 1;
        float syncLatchUpdateFrequency = 0.1f;

        [UdonSynced]
        VRCUrl _syncUrl;
        VRCUrl _queuedUrl;

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
        float _lastSyncTime;
        float _playStartTime = 0;

        float _pendingLoadTime = 0;
        float _pendingPlayTime = 0;
        VRCUrl _pendingPlayUrl;

        bool _hasAccessControl = false;

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

        // Constants

        const int PLAYER_STATE_STOPPED = 0x01;
        const int PLAYER_STATE_LOADING = 0x02;
        const int PLAYER_STATE_SYNC = 0x04;
        const int PLAYER_STATE_PLAYING = 0x08;
        const int PLAYER_STATE_ERROR = 0x10;
        const int PLAYER_STATE_PAUSED = 0x20;

        const int SCREEN_MODE_NORMAL = 0;
        const int SCREEN_MODE_LOGO = 1;
        const int SCREEN_MODE_LOADING = 2;
        const int SCREEN_MODE_ERROR = 3;

        const int SCREEN_SOURCE_UNITY = 0;
        const int SCREEN_SOURCE_AVPRO = 1;

        bool _StateIs(int state, int set)
        {
            return (state & set) > 0;
        }

        void Start()
        {
            dataProxy._Init();

            _hasAccessControl = Utilities.IsValid(accessControl);

            avProVideo.Loop = false;
            avProVideo.Stop();
            _currentPlayer = avProVideo;

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

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
                    _PlayVideo(playlist._GetCurrent());
                else             
                    _PlayVideo(defaultUrl);
            }

            SendCustomEventDelayedSeconds("_AVSyncStart", 1);
        }

        public void _TriggerPlay()
        {
            DebugLog("Trigger play");
            if (_StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_LOADING | PLAYER_STATE_SYNC))
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
            if (!seekableSource || !_StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_PAUSED))
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncOwnerPaused = !_syncOwnerPaused;

            if (_syncOwnerPaused) {
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _currentPlayer.GetTime();
                _currentPlayer.Pause();
                _UpdatePlayerState(PLAYER_STATE_PAUSED);
            } else
                _currentPlayer.Play();

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

        public void _Resync()
        {
            _ForceResync();
        }

        public void _ChangeUrl(VRCUrl url)
        {
            if (_syncLocked && !_CanTakeControl())
                return;

            _PlayVideo(url);

            _queuedUrl = VRCUrl.Empty;
        }

        public void _UpdateQueuedUrl(VRCUrl url)
        {
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _queuedUrl = url;
        }

        public void _SetTargetTime(float time)
        {
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!_StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_PAUSED | PLAYER_STATE_SYNC))
                return;
            if (!seekableSource)
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Allowing AVPro to set time directly to end of track appears to trigger deadlock sometimes
            float duration = _currentPlayer.GetDuration();
            if (duration - time < 1)
            {
                bool hasPlaylist = Utilities.IsValid(playlist) && playlist.playlistEnabled;
                if (_IsUrlValid(_queuedUrl))
                {
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                    return;
                }
                else if (hasPlaylist && playlist._MoveNext())
                {
                    _queuedUrl = playlist._GetCurrent();
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
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
            SyncVideo();
            RequestSerialization();
        }

        void _PlayVideo(VRCUrl url)
        {
            _pendingPlayTime = 0;
            if (!_IsUrlValid(url))
                return;

            DebugLog("Play video " + url);
            bool isOwner = Networking.IsOwner(gameObject);
            if (!isOwner && !_TakeControl())
                return;

            _syncUrl = url;
            _syncVideoNumber += isOwner ? 1 : 2;
            _loadedVideoNumber = _syncVideoNumber;
            _syncOwnerPlaying = false;
            _syncOwnerPaused = false;

            _syncVideoStartNetworkTime = float.MaxValue;
            RequestSerialization();

            _videoTargetTime = _ParseTimeFromUrl(url.Get());
            _UpdateLastUrl();

            // Conditional player stop to try and avoid piling on AVPro at end of track
            // and maybe triggering bad things
            bool playingState = _StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_PAUSED | PLAYER_STATE_SYNC);
            if (playingState && _currentPlayer.IsPlaying && seekableSource)
            {
                float duration = _currentPlayer.GetDuration();
                float remaining = duration - _currentPlayer.GetTime();
                if (remaining > 2)
                    _currentPlayer.Stop();
            }

            _StartVideoLoad();
        }

        public void _LoopVideo()
        {
            _PlayVideo(_syncUrl);
        }

        public void _PlayQueuedUrl()
        {
            _PlayVideo(_queuedUrl);
            _queuedUrl = VRCUrl.Empty;
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

            DebugLog("Start video load " + _syncUrl);
            _UpdatePlayerState(PLAYER_STATE_LOADING);

#if !UNITY_EDITOR
            _currentPlayer.LoadURL(_syncUrl);
#endif
        }

        public void _StopVideo()
        {
            DebugLog("Stop video");

            if (seekableSource)
                _lastVideoPosition = _currentPlayer.GetTime();

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            _currentPlayer.Stop();
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

        public override void OnVideoReady()
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
                _currentPlayer.Play();
            else
            {
                // TODO: Stream bypass owner
                if (_syncOwnerPlaying)
                    _currentPlayer.Play();
                else
                    _waitForSync = true;
            }
        }

        public override void OnVideoStart()
        {
            DebugLog("Video start");

            if (Networking.IsOwner(gameObject))
            {
                bool paused = _StateIs(localPlayerState, PLAYER_STATE_PAUSED);
                if (paused)
                    _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _currentPlayer.GetTime();
                else
                    _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _videoTargetTime;

                _UpdatePlayerState(PLAYER_STATE_PLAYING);
                _playStartTime = Time.time;

                _syncOwnerPlaying = true;
                _syncOwnerPaused = false;
                RequestSerialization();

                if (!paused)
                    _currentPlayer.SetTime(_videoTargetTime);

                SyncVideo();
            }
            else
            {
                if (!_syncOwnerPlaying || _syncOwnerPaused)
                {
                    // TODO: Owner bypass
                    _currentPlayer.Pause();
                    _waitForSync = true;

                    if (_syncOwnerPaused)
                        _UpdatePlayerState(PLAYER_STATE_PAUSED);
                }
                else
                {
                    _UpdatePlayerState(PLAYER_STATE_PLAYING);
                    _playStartTime = Time.time;
                    
                    SyncVideo();
                }
            }
        }

        public override void OnVideoEnd()
        {
            if (!seekableSource && Time.time - _playStartTime < 1)
            {
                Debug.Log("Video end encountered at start of stream, ignoring");
                return;
            }

            seekableSource = false;
            dataProxy.seekableSource = false;

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            DebugLog("Video end");
            _lastVideoPosition = 0;

            if (Networking.IsOwner(gameObject))
            {
                bool hasPlaylist = Utilities.IsValid(playlist) && playlist.playlistEnabled;
                if (_IsUrlValid(_queuedUrl))
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                else if (hasPlaylist && playlist._MoveNext()) {
                    _queuedUrl = playlist._GetCurrent();
                    SendCustomEventDelayedFrames("_PlayQueuedUrl", 1);
                }
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
        public override void OnVideoLoop()
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

        public override void OnVideoError(VideoError videoError)
        {
            _currentPlayer.Stop();

            DebugLog("Video stream failed: " + _syncUrl);
            DebugLog("Error code: " + videoError);

            _UpdatePlayerStateError(videoError);

            if (Networking.IsOwner(gameObject))
            {
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
            return player.isMaster || player.isInstanceOwner;
        }

        public bool _CanTakeControl()
        {
            if (_hasAccessControl)
                return !_syncLocked || accessControl._LocalHasAccess();

            VRCPlayerApi player = Networking.LocalPlayer;
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

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject))
                return;

            DebugLog($"Deserialize: video #{_syncVideoNumber}");

            _UpdateLockState(_syncLocked);
            _UpdateRepeatMode(_syncRepeatPlaylist);

            if (_syncVideoNumber == _loadedVideoNumber)
            {
                bool playingState = _StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_PAUSED | PLAYER_STATE_SYNC);
                if (playingState && !_syncOwnerPlaying)
                    SendCustomEventDelayedFrames("_StopVideo", 1);
                else if (_StateIs(localPlayerState, PLAYER_STATE_PAUSED) && !_syncOwnerPaused)
                {
                    DebugLog("Unpausing video");
                    _currentPlayer.Play();
                    _UpdatePlayerState(PLAYER_STATE_PLAYING);
                } else if (_StateIs(localPlayerState, PLAYER_STATE_PLAYING) && _syncOwnerPaused)
                {
                    DebugLog("Pausing video");
                    _currentPlayer.Pause();
                    _UpdatePlayerState(PLAYER_STATE_PAUSED);
                }

                return;
            }

            // There was some code here to bypass load owner sync bla bla

            _loadedVideoNumber = _syncVideoNumber;
            _UpdateLastUrl();

            DebugLog("Starting video load from sync");

            _StartVideoLoad();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                DebugLog("Failed to sync");
                return;
            }
        }

        void Update()
        {
            bool isOwner = Networking.IsOwner(gameObject);
            float time = Time.time;

            if (_pendingPlayTime > 0 && time > _pendingPlayTime)
                _PlayVideo(_pendingPlayUrl);
            if (_pendingLoadTime > 0 && Time.time > _pendingLoadTime)
                _StartVideoLoad();

            bool playingState = _StateIs(localPlayerState, PLAYER_STATE_PLAYING | PLAYER_STATE_PAUSED | PLAYER_STATE_SYNC);
            if (seekableSource && playingState)
            {
                float position = Mathf.Floor(_currentPlayer.GetTime());
                if (position != previousTrackPosition)
                {
                    previousTrackPosition = position;
                    float target = position;
                    if (syncLatched)
                        target = GetTargetTime();

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
            _currentPlayer.Play();

            SyncVideo();
        }

        void SyncVideoIfTime()
        {
            if (Time.realtimeSinceStartup - _lastSyncTime > syncFrequency)
            {
                _lastSyncTime = Time.realtimeSinceStartup;
                SyncVideo();
            }
        }

        void SyncVideo()
        {
            if (seekableSource && !syncLatched)
            {
                float duration = _currentPlayer.GetDuration();
                float current = _currentPlayer.GetTime();
                float serverTime = (float)Networking.GetServerTimeInSeconds();
                float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
                if (Mathf.Abs(current - offsetTime) > syncThreshold && (duration - current) > 2)
                {
                    _currentPlayer.SetTime(offsetTime);
                    DebugLog($"Sync time (off by {current - offsetTime}s) [net={serverTime}, sync={_syncVideoStartNetworkTime}, cur={current}]");

                    float readbackTime = _currentPlayer.GetTime();
                    if (offsetTime - readbackTime > 1)
                    {
                        DebugLog($"Starting extended synchronization (target={offsetTime}, readback={readbackTime})");
                        syncLatched = true;
                        _UpdatePlayerState(PLAYER_STATE_SYNC);
                        SendCustomEventDelayedSeconds("_SyncLatch", syncLatchUpdateFrequency);
                    }
                }
            }
        }

        bool syncLatched = false;

        public void _SyncLatch()
        {
            syncLatched = false;

            float duration = _currentPlayer.GetDuration();
            float current = _currentPlayer.GetTime();
            float serverTime = (float)Networking.GetServerTimeInSeconds();
            float offsetTime = Mathf.Clamp(serverTime - _syncVideoStartNetworkTime, 0f, duration);
            if (Mathf.Abs(current - offsetTime) > syncThreshold && (duration - current) > 2)
            {
                _currentPlayer.SetTime(offsetTime);
                float readbackTime = _currentPlayer.GetTime();
                if (offsetTime - readbackTime > 1)
                {
                    syncLatched = true;
                    SendCustomEventDelayedSeconds("_SyncLatch", syncLatchUpdateFrequency);
                }
            }

            if (!syncLatched)
            {
                DebugLog("Synchronized");
                _UpdatePlayerState(PLAYER_STATE_PLAYING);
            }
        }

        public void _AVSyncStart()
        {
            _currentPlayer.EnableAutomaticResync = true;
            SendCustomEventDelayedSeconds("_AVSyncEnd", 1);
        }

        public void _AVSyncEnd()
        {
            _currentPlayer.EnableAutomaticResync = false;
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
                        startTime = _currentPlayer.GetTime();

                    _StartVideoLoad();
                    _videoTargetTime = startTime;
                }
                return;
            }

            _currentPlayer.Stop();
            if (_syncOwnerPlaying)
                _StartVideoLoad();
        }

        void _UpdatePlayerState(int state)
        {
            localPlayerState = state;
            dataProxy.playerState = state;
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
            if (!debugLogging)
                Debug.Log("[VideoTXL:SyncPlayer] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SyncPlayer", message);
        }
    }
}
