
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Sync Player")]
    public class BasicSyncPlayer : UdonSharpBehaviour
    {
        [Tooltip("Stream Player Reference")]
        public VRCAVProVideoPlayer avProVideo;

        public VRCUrl defaultUrl;
        public bool legacyVideoPlayback = false;
        public bool defaultStream = false;

        public bool defaultLocked = false;

        public bool retryOnError = true;
        public float retryTimeout = 6;
        float syncFrequency = 5;
        float syncThreshold = 1;

        public Text statusText;

        [UdonSynced]
        VRCUrl _syncUrl;
        VRCUrl _localUrl;
        VRCUrl _selectedUrl;

        [UdonSynced]
        int _syncPlayerMode = PLAYER_MODE_VIDEO;
        int _localPlayerMode = PLAYER_MODE_VIDEO;

        [UdonSynced]
        int _syncVideoNumber;
        int _loadedVideoNumber;

        [UdonSynced, NonSerialized]
        public bool _syncOwnerPlaying;
        bool _localLoading = false;
        bool _localPlaying = false;

        [UdonSynced]
        float _syncVideoStartNetworkTime;

        [UdonSynced]
        bool _syncLocked = true;

        bool _rtsptSource = false;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PAUSED = 2;
        const int PLAYER_STATE_PLAYING = 3;
        const int PLAYER_STATE_ERROR = 4;

        [NonSerialized]
        public int localPlayerState = PLAYER_STATE_STOPPED;
        [NonSerialized]
        public VideoError localLastErrorCode;

        const int SCREEN_MODE_NORMAL = 0;
        const int SCREEN_MODE_LOGO = 1;
        const int SCREEN_MODE_LOADING = 2;
        const int SCREEN_MODE_ERROR = 3;

        BaseVRCVideoPlayer _currentPlayer;

        float _lastVideoPosition = 0;
        float _videoTargetTime = 0;

        bool _waitForSync;
        float _lastSyncTime;

        VRCUrl playAtUrl;
        float playAt = 0;
        bool playingOrLoading = false;

        // Realtime state

        [NonSerialized]
        public bool seekableSource;
        [NonSerialized]
        public float trackDuration;
        [NonSerialized]
        public float trackPosition;

        float playStartTime = 0;

        [NonSerialized]
        public bool locked;
        [NonSerialized]
        public bool localPlayerAccess;

        // Constants

        const int PLAYER_MODE_VIDEO = 0;
        const int PLAYER_MODE_STREAM = 1;

        void Start()
        {
            avProVideo.Loop = false;
            avProVideo.Stop();

            _currentPlayer = avProVideo;
            _selectedUrl = defaultUrl;
            _syncUrl = defaultUrl;
            _localUrl = defaultUrl;

            if (Networking.IsOwner(gameObject))
            {
                _syncLocked = defaultLocked;
                locked = _syncLocked;
                RequestSerialization();
            }
            
            _PlayVideo(defaultUrl);
        }

        public void _TriggerPlay()
        {
            DebugLog("Trigger play");
            if (playAt > 0 || playingOrLoading)
                return;

            _PlayVideoAfter(_selectedUrl, 0);
        }

        public void _TriggerStop()
        {
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _StopVideo();
            localPlayerState = PLAYER_STATE_STOPPED;
        }

        public void _TriggerLock()
        {
            if (!_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncLocked = !_syncLocked;
            locked = _syncLocked;
            RequestSerialization();
        }

        public void _Resync()
        {
            _StopVideo();
            _PlayVideo(_selectedUrl);
        }

        public void _ChangeUrl(VRCUrl url)
        {
            _selectedUrl = url;
            if (playingOrLoading)
                _Resync();
            else
                _PlayVideo(_selectedUrl);

            //if (Networking.IsOwner(gameObject))
            //    videoOwner = Networking.LocalPlayer.displayName;
        }

        public void _SetTargetTime(float time)
        {
            if (_syncLocked && !_CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - time;
            SyncVideo();
            RequestSerialization();
        }

        void _PlayVideoAfter(VRCUrl url, float delay)
        {
            playAtUrl = url;
            playAt = Time.time + delay;
        }

        void _PlayVideo(VRCUrl url)
        {
            DebugLog("Play video " + url);
            bool isOwner = Networking.IsOwner(gameObject);
            if (!isOwner && !_CanTakeControl())
                return;

            if (!Utilities.IsValid(url))
                return;

            string urlStr = url.Get();
            if (urlStr == null || urlStr == "")
                return;

            if (!isOwner)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncUrl = url;
            _syncVideoNumber += isOwner ? 1 : 2;
            _loadedVideoNumber = _syncVideoNumber;
            _syncOwnerPlaying = false;

            _syncVideoStartNetworkTime = float.MaxValue;
            RequestSerialization();

            _StartVideoLoad(url);
        }

        void _StartVideoLoad(VRCUrl url)
        {
            playAt = 0;
            if (url == null || url.Get() == "")
                return;

            DebugLog("Start video load " + url);

            playingOrLoading = true;
            localPlayerState = PLAYER_STATE_LOADING;

            _currentPlayer.Stop();
#if !UNITY_EDITOR
            _currentPlayer.LoadURL(url);
#endif
        }

        void _StopVideo()
        {
            if (_localPlayerMode == PLAYER_MODE_VIDEO)
                _lastVideoPosition = _currentPlayer.GetTime();

            _currentPlayer.Stop();
            _syncVideoStartNetworkTime = 0;
            _syncOwnerPlaying = false;
            _syncUrl = VRCUrl.Empty;
            _videoTargetTime = 0;
            RequestSerialization();

            playAt = 0;
            playingOrLoading = false;
            playStartTime = 0;
            localPlayerState = PLAYER_STATE_STOPPED;
        }

        public override void OnVideoReady()
        {
            float duration = _currentPlayer.GetDuration();
            DebugLog("Video ready, duration: " + duration);

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;

            // If player is owner: play video
            // If Player is remote:
            //   - If owner playing state is already synced, play video
            //   - Otherwise, wait until owner playing state is synced and play later in update()
            //   TODO: Streamline by always doing this in update instead?

            // statusText.text = "duration: " + _currentPlayer.GetDuration() + ", time: " + _currentPlayer.GetTime();

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
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _videoTargetTime;
                _syncOwnerPlaying = true;
                RequestSerialization();

                localPlayerState = PLAYER_STATE_PLAYING;
                playStartTime = Time.time;
            }
            else
            {
                if (!_syncOwnerPlaying)
                {
                    // TODO: Owner bypass
                    _currentPlayer.Pause();
                    _waitForSync = true;
                }
                else
                {
                    localPlayerState = PLAYER_STATE_PLAYING;
                    playStartTime = Time.time;
                    SyncVideo();
                }
            }
        }

        public override void OnVideoEnd()
        {
            if (seekableSource && Time.time - playStartTime < 1)
            {
                Debug.Log("Video end encountered at start of stream, ignoring");
                return;
            }

            playingOrLoading = false;
            localPlayerState = PLAYER_STATE_STOPPED;

            DebugLog("Video end");
            _lastVideoPosition = 0;
            _currentPlayer.Stop();

            if (Networking.IsOwner(gameObject))
            {
                _syncVideoStartNetworkTime = 0;
                _syncOwnerPlaying = false;
                RequestSerialization();
            }
        }

        public override void OnVideoError(VideoError videoError)
        {
            _currentPlayer.Stop();
            _videoTargetTime = 0;

            VRCUrl url = _selectedUrl;
            DebugLog("Video stream failed: " + url);
            DebugLog("Error code: " + videoError);

            playingOrLoading = false;
            localPlayerState = PLAYER_STATE_ERROR;
            localLastErrorCode = videoError;

            if (retryOnError)
                _PlayVideoAfter(url, retryTimeout);
        }

        public bool _CanTakeControl()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            return player.isMaster || player.isInstanceOwner || !_syncLocked;
        }

        bool TakeOwnership()
        {
            if (Networking.IsOwner(gameObject))
                return true;
            if (!_CanTakeControl())
                return false;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            return true;
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject))
                return;

            DebugLog($"Deserialize: video #{_syncVideoNumber}");

            locked = _syncLocked;

            if (localPlayerState == PLAYER_STATE_PLAYING && !_syncOwnerPlaying)
                _StopVideo();

            if (_syncVideoNumber == _loadedVideoNumber)
                return;

            // There was some code here to bypass load owner sync bla bla

            _localUrl = _syncUrl;
            _loadedVideoNumber = _syncVideoNumber;

            DebugLog("Starting video load from sync");

            _StartVideoLoad(_syncUrl);
        }

        void Update()
        {
            bool isOwner = Networking.IsOwner(gameObject);

            if (playAt > 0 && Time.time > playAt)
            {
                playAt = 0;
                _PlayVideo(playAtUrl);
            }

            if (seekableSource && localPlayerState == PLAYER_STATE_PLAYING)
            {
                trackDuration = _currentPlayer.GetDuration();
                trackPosition = _currentPlayer.GetTime();
            }

            // Video is playing: periodically sync with owner
            if (isOwner || !_waitForSync)
            {
                SyncVideoIfTime();
                return;
            }

            // Video is not playing, but still waiting for go-ahead from owner
            if (!_syncOwnerPlaying)
                return;

            // Got go-ahead from owner, start playing video
            _waitForSync = false;
            _currentPlayer.Play();

            localPlayerState = PLAYER_STATE_PLAYING;

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
            if (seekableSource)
            {
                float offsetTime = Mathf.Clamp((float)Networking.GetServerTimeInSeconds() - _syncVideoStartNetworkTime, 0f, _currentPlayer.GetDuration());
                if (Mathf.Abs(_currentPlayer.GetTime() - offsetTime) > syncThreshold)
                    _currentPlayer.SetTime(offsetTime);
            }
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

                    _StartVideoLoad(_syncUrl);
                    //PlayVideo(_syncedURL, false);
                    _videoTargetTime = startTime;

                    return;
                }
            }

            _currentPlayer.Stop();
            if (_syncOwnerPlaying)
                _StartVideoLoad(_syncUrl);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _RefreshOwnerData();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _RefreshOwnerData();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _RefreshOwnerData();
        }

        void _RefreshOwnerData()
        {
            
        }

        // Debug

        public Text debugText;
        string[] debugLines;
        int debugIndex = 0;

        void DebugLog(string message)
        {
            DebugLogWrite(message, false);
        }

        void DebugLogOwner(string message)
        {
            DebugLogWrite(message, true);
        }

        void DebugLogWrite(string message, bool owner)
        {
            Debug.Log("[VideoTXL:SyncPlayer] " + message);

            if (!Utilities.IsValid(debugText))
                return;

            if (debugLines == null || debugLines.Length == 0)
            {
                debugLines = new string[28];
                for (int i = 0; i < debugLines.Length; i++)
                    debugLines[i] = "";
            }

            debugLines[debugIndex] = "[SyncPlayer] " + message;

            string buffer = "";
            for (int i = debugIndex + 1; i < debugLines.Length; i++)
                buffer = buffer + debugLines[i] + "\n";
            for (int i = 0; i < debugIndex; i++)
                buffer = buffer + debugLines[i] + "\n";
            buffer = buffer + debugLines[debugIndex];

            debugIndex += 1;
            if (debugIndex >= debugLines.Length)
                debugIndex = 0;

            debugText.text = buffer;
        }
    }
}
