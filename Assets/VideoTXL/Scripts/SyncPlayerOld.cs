
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
    public class SyncPlayerOld : UdonSharpBehaviour
    {
        [Tooltip("Optional component to control and synchronize player video screens and materials")]
        public ScreenManager screenManager;
        [Tooltip("Optional component to control and synchronize player audio sources")]
        public VolumeController audioManager;
        [Tooltip("Optional component to start or stop player based on common trigger events")]
        public TriggerManager triggerManager;
        [Tooltip("Optional component to control access to player controls based on player type or whitelist")]
        public AccessControl accessControl;

        [Tooltip("Video Player Reference")]
        public VRCUnityVideoPlayer unityVideo;
        [Tooltip("Stream Player Reference")]
        public VRCAVProVideoPlayer avProVideo;

        public VRCUrl defaultUrl;
        public bool legacyVideoPlayback = false;
        public bool defaultStream = false;

        public bool defaultLocked = false;

        public bool loop;
        public bool resumePosition;

        public bool retryOnError = true;
        public float retryTimeout = 6;
        float syncFrequency = 5;
        float syncThreshold = 1;

        GameObject[] eventHandlers;

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

        public const int SCREEN_SOURCE_UNITY = 0;
        public const int SCREEN_SOURCE_AVPRO = 1;

        const int SCREEN_MODE_NORMAL = 0;
        const int SCREEN_MODE_LOGO = 1;
        const int SCREEN_MODE_LOADING = 2;
        const int SCREEN_MODE_ERROR = 3;

        bool _initComplete = false;
        bool _hasScreenManager = false;
        bool _hasTriggerManager = false;
        bool _hasAudioManager = false;
        bool _hasAccessControl = false;

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

        [NonSerialized]
        public string instanceOwner;
        [NonSerialized]
        public string instanceMaster;
        [NonSerialized]
        public string playerOwner;
        [NonSerialized]
        public string videoOwner;
        [NonSerialized]
        public string currentUrl;
        [NonSerialized]
        public string lastUrl;

        [NonSerialized]
        public bool locked;
        [NonSerialized]
        public bool localPlayerAccess;

        // Constants

        const int PLAYER_MODE_VIDEO = 0;
        const int PLAYER_MODE_STREAM = 1;

        void Start()
        {
            // Find available component managers
            if (!Utilities.IsValid(screenManager))
                screenManager = GetComponentInChildren<ScreenManager>();
            _hasScreenManager = Utilities.IsValid(screenManager);

            if (!Utilities.IsValid(triggerManager))
                triggerManager = GetComponentInChildren<TriggerManager>();
            _hasTriggerManager = Utilities.IsValid(triggerManager);

            if (!Utilities.IsValid(audioManager))
                audioManager = GetComponentInChildren<VolumeController>();
            _hasAudioManager = Utilities.IsValid(audioManager);

            if (!Utilities.IsValid(accessControl))
                accessControl = GetComponentInChildren<AccessControl>();
            _hasAccessControl = Utilities.IsValid(accessControl);

            unityVideo.Loop = false;
            unityVideo.Stop();
            avProVideo.Loop = false;
            avProVideo.Stop();

            _currentPlayer = avProVideo;
            _selectedUrl = defaultUrl;
            _syncUrl = defaultUrl;
            _localUrl = defaultUrl;

            if (_hasTriggerManager)
                triggerManager._RegisterPlayer((UdonBehaviour)GetComponent(typeof(UdonBehaviour)));

            SendCustomEventDelayedFrames("_Init", 1);
        }

        public void _Init()
        {
            ChangePlayerMode(defaultStream ? PLAYER_MODE_STREAM : PLAYER_MODE_VIDEO);

            if (_hasScreenManager)
                screenManager._UpdateScreenSource((_localPlayerMode == PLAYER_MODE_VIDEO && legacyVideoPlayback) ? SCREEN_SOURCE_UNITY : SCREEN_SOURCE_AVPRO);

            _UpdateScreenMaterial(SCREEN_MODE_LOGO);
            _initComplete = true;
        }

        public void _RegisterEventHandler(GameObject handler)
        {
            if (!Utilities.IsValid(handler))
                return;

            if (!Utilities.IsValid(eventHandlers))
                eventHandlers = new GameObject[0];

            foreach (GameObject h in eventHandlers)
            {
                if (h == handler)
                    return;
            }

            GameObject[] newHandlers = new GameObject[eventHandlers.Length + 1];
            for (int i = 0; i < eventHandlers.Length; i++)
                newHandlers[i] = eventHandlers[i];

            newHandlers[eventHandlers.Length] = handler;
            eventHandlers = newHandlers;

            Debug.Log("[VideoTXL:SyncPlayer] registering new event handler");
        }

        void _EmitEvent(string eventName)
        {
            if (!Utilities.IsValid(eventHandlers))
                return;

            foreach (GameObject handler in eventHandlers)
            {
                if (!Utilities.IsValid(handler))
                    continue;
                UdonBehaviour script = (UdonBehaviour)handler.GetComponent(typeof(UdonBehaviour));
                if (Utilities.IsValid(script))
                    script.SendCustomEvent(eventName);
            }
        }

        public void _TriggerPlay()
        {
            DebugLog("Trigger play");
            if (!_initComplete)
                _Init();

            if (playAt > 0 || playingOrLoading)
                return;

            _PlayVideoAfter(_GetSelectedUrl(), 0);
        }

        public void _TriggerStop()
        {
            if (_syncLocked && !CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _StopVideo();
            localPlayerState = PLAYER_STATE_STOPPED;
            _UpdateScreenMaterial(SCREEN_MODE_LOGO);
        }

        public void _Resync()
        {
            _StopVideo();
            _PlayVideo(_GetSelectedUrl());
        }

        public void _TriggerLock()
        {
            if (!CanTakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _syncLocked = !_syncLocked;
            locked = _syncLocked;
            RequestSerialization();
        }

        public void _UrlChanged()
        {
            if (playingOrLoading)
                _Resync();
        }

        public void _ChangeUrl(VRCUrl url)
        {
            _selectedUrl = url;
            if (playingOrLoading)
                _Resync();
            else
                _PlayVideo(_GetSelectedUrl());

            //if (Networking.IsOwner(gameObject))
            //    videoOwner = Networking.LocalPlayer.displayName;
        }

        public void _SetTargetTime(float time)
        {
            _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - time;
            SyncVideo();
        }

        VRCUrl _GetSelectedUrl()
        {
            return _selectedUrl;
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
            if (!isOwner && !CanTakeControl())
                return;

            if (_syncUrl != null && url.Get() == "")
                return;

            if (!isOwner)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            lastUrl = currentUrl;
            currentUrl = url.Get();

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

            if (url != null)
            {
                string urlStr = url.Get();

                // RTSPT sources (and maybe others!?) trigger a spontaneous OnVideoEnd event at video start
                if (_currentPlayer == avProVideo && urlStr.Contains("rtspt://"))
                {
                    _rtsptSource = true;
                    DebugLog("Detected RTSPT source");
                }
                else
                    _rtsptSource = false;
            }

            playingOrLoading = true;
            localPlayerState = PLAYER_STATE_LOADING;

            _UpdateScreenMaterial(SCREEN_MODE_LOADING);
            _EmitEvent("_SyncPlayer_Play");

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
            localPlayerState = PLAYER_STATE_STOPPED;

            _EmitEvent("_SyncPlayer_Stop");
        }

        public override void OnVideoReady()
        {
            float duration = _currentPlayer.GetDuration();
            DebugLog("Video ready, duration: " + duration);

            if (_hasTriggerManager && !triggerManager._IsTriggerActive())
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStart();

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

            if (_hasTriggerManager && !triggerManager._IsTriggerActive())
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();

                if (Utilities.IsValid(audioManager))
                    audioManager._VideoStop();

                return;
            }

            if (Networking.IsOwner(gameObject))
            {
                _syncVideoStartNetworkTime = (float)Networking.GetServerTimeInSeconds() - _videoTargetTime;
                _syncOwnerPlaying = true;
                RequestSerialization();
                _UpdateScreenMaterial(SCREEN_MODE_NORMAL);
                localPlayerState = PLAYER_STATE_PLAYING;
                _EmitEvent("_SyncPlayer_Start");
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
                    _UpdateScreenMaterial(SCREEN_MODE_NORMAL);
                    localPlayerState = PLAYER_STATE_PLAYING;
                    SyncVideo();
                }
            }

            //statusText.text = "duration: " + _currentPlayer.GetDuration() + ", time: " + _currentPlayer.GetTime();
        }

        public override void OnVideoEnd()
        {
            if (_rtsptSource)
            {
                Debug.Log("Video ended (ignored) for RTSPT source");
                return;
            }

            playingOrLoading = false;
            localPlayerState = PLAYER_STATE_STOPPED;

            DebugLog("Video end");
            _lastVideoPosition = 0;
            _UpdateScreenMaterial(SCREEN_MODE_LOGO);

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStop();

            if (Networking.IsOwner(gameObject))
            {
                _syncVideoStartNetworkTime = 0;
                _syncOwnerPlaying = false;
                RequestSerialization();
            }

            _EmitEvent("_SyncPlayer_Stop");
        }

        public override void OnVideoError(VideoError videoError)
        {
            _currentPlayer.Stop();
            _videoTargetTime = 0;

            VRCUrl url = _GetSelectedUrl();
            DebugLog("Video stream failed: " + url);
            DebugLog("Error code: " + videoError);

            playingOrLoading = false;
            localPlayerState = PLAYER_STATE_ERROR;
            localLastErrorCode = videoError;

            if (_hasScreenManager)
                screenManager._UpdateVideoError(videoError);

            _UpdateScreenMaterial(SCREEN_MODE_ERROR);

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStop();

            _EmitEvent("_SyncPlayer_Error");

            if (retryOnError)
                _PlayVideoAfter(url, retryTimeout);
        }

        void _UpdateScreenMaterial(int screenMode)
        {
            if (_hasScreenManager)
                screenManager._UpdateScreenMaterial(screenMode);
        }

        void ChangePlayerMode(int mode)
        {
            _syncPlayerMode = mode;
            if (_syncPlayerMode == _localPlayerMode)
                return;

            _currentPlayer.Stop();
            if (mode == PLAYER_MODE_VIDEO && legacyVideoPlayback)
            {
                _currentPlayer = unityVideo;
                if (_hasScreenManager)
                    screenManager._UpdateScreenSource(SCREEN_SOURCE_UNITY);
                DebugLog($"Change player mode to {mode}, using unity video");
            }
            else
            {
                _currentPlayer = avProVideo;
                if (_hasScreenManager)
                    screenManager._UpdateScreenSource(SCREEN_SOURCE_AVPRO);
                DebugLog($"Change player mode to {mode}, using AVPro");
            }

            _localPlayerMode = mode;
            RequestSerialization();
        }

        bool LocalIsOwner()
        {
            return Networking.IsOwner(gameObject);
        }

        bool CanTakeControl()
        {
            // TODO: not the right place for this
            if (_hasAccessControl)
                localPlayerAccess = accessControl._LocalHasAccess();
            else
                localPlayerAccess = false;

            if (_hasAccessControl)
                return accessControl._LocalHasAccess();
            return !_syncLocked;
        }

        void TakeOwnership()
        {
            if (CanTakeControl() && !Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        //int _deserializeCounter;
        public override void OnPreSerialization()
        {
            //_deserializeCounter = 0;
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject))
                return;

            DebugLog($"Deserialize: video #{_syncVideoNumber}");

            // Needed to prevent "rewinding" behaviour of Udon synced strings/VRCUrl's where, when switching ownership 
            // the string will be populated with the second to last value locally observed.
            //if (_deserializeCounter < 10)
            //{
            //    _deserializeCounter++;
            //    return;
            //}

            locked = _syncLocked;

            if (_localPlayerMode != _syncPlayerMode)
                ChangePlayerMode(_syncPlayerMode);

            if (localPlayerState == PLAYER_STATE_PLAYING && !_syncOwnerPlaying)
                _StopVideo();

            if (_syncVideoNumber == _loadedVideoNumber)
                return;

            // There was some code here to bypass load owner sync bla bla

            _localUrl = _syncUrl;
            _loadedVideoNumber = _syncVideoNumber;

            lastUrl = currentUrl;
            currentUrl = _localUrl.Get();

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

            _EmitEvent("_SyncPlayer_Start");
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

            VRCPlayerApi objOwner = Networking.GetOwner(gameObject);
            if (Utilities.IsValid(objOwner))
                playerOwner = objOwner.displayName;
            else
                playerOwner = "";
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

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(SyncPlayerOld))]
    internal class SyncPlayerInspector : Editor
    {
        SerializedProperty screenManagerProperty;
        SerializedProperty audioManagerProperty;
        SerializedProperty triggerManagerProperty;
        SerializedProperty accessControlProperty;

        SerializedProperty avProVideoPlayerProperty;
        SerializedProperty unityVideoPlayerProperty;

        //SerializedProperty staticUrlSourceProperty;
        SerializedProperty urlProperty;

        SerializedProperty legacyVideoProperty;
        SerializedProperty loopProperty;
        SerializedProperty resumePositionProperty;

        SerializedProperty debugTextProperty;

        private void OnEnable()
        {
            screenManagerProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.screenManager));
            audioManagerProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.audioManager));
            triggerManagerProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.triggerManager));
            accessControlProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.accessControl));

            avProVideoPlayerProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.avProVideo));
            unityVideoPlayerProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.unityVideo));

            //staticUrlSourceProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.staticUrlSource));
            urlProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.defaultUrl));

            legacyVideoProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.legacyVideoPlayback));
            loopProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.loop));
            resumePositionProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.resumePosition));

            debugTextProperty = serializedObject.FindProperty(nameof(SyncPlayerOld.debugText));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(screenManagerProperty);
            EditorGUILayout.PropertyField(audioManagerProperty);
            EditorGUILayout.PropertyField(triggerManagerProperty);
            EditorGUILayout.PropertyField(accessControlProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(unityVideoPlayerProperty);
            EditorGUILayout.PropertyField(avProVideoPlayerProperty);
            EditorGUILayout.Space();
            //EditorGUILayout.PropertyField(staticUrlSourceProperty);
            //if (staticUrlSourceProperty.objectReferenceValue == null)
            EditorGUILayout.PropertyField(urlProperty);
            if (unityVideoPlayerProperty.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(loopProperty);
                EditorGUILayout.PropertyField(resumePositionProperty);
            }

            EditorGUILayout.PropertyField(legacyVideoProperty);

            EditorGUILayout.PropertyField(debugTextProperty);

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
