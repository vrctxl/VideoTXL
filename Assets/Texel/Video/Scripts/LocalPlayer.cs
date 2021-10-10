
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

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("VideoTXL/Specialty/Zoned Stream Player")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class LocalPlayer : UdonSharpBehaviour
    {
        [Tooltip("A proxy for dispatching video-related events to other listening behaviors, such as a screen manager")]
        public VideoPlayerProxy dataProxy;

        [Header("Optional Components")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Header("Playback")]
        [Tooltip("Optional trigger zone the player must be in to sustain playback.  Disables playing audio on world load.")]
        public CompoundZoneTrigger playbackZone;
        [Tooltip("Optional trigger zone that will start playback if player enters.")]
        public CompoundZoneTrigger triggerZone;

        [Header("Default Options")]
        public StaticUrlSource staticUrlSource;
        public VRCUrl streamUrl;

        [Tooltip("Write out video player events to VRChat log")]
        public bool debugLogging = true;

        [Tooltip("Automatically loop track when finished")]
        public bool loop;

        [Tooltip("Remember where video was stopped and resume at that position when re-triggered")]
        public bool resumePosition;

        [Tooltip("Whether to keep playing the same URL if an error occurs")]
        public bool retryOnError = true;

        [Header("Internal Objects")]
        [Tooltip("AVPro video player component")]
        public VRCAVProVideoPlayer avProVideo;
        [Tooltip("Unity video player component")]
        public VRCUnityVideoPlayer unityVideo;

        float retryTimeout = 6;

        // Realtime state

        short videoSourceOverride = VIDEO_SOURCE_NONE;

        [NonSerialized]
        public int localPlayerState = PLAYER_STATE_STOPPED;
        [NonSerialized]
        public VideoError localLastErrorCode;
        [NonSerialized]
        public bool seekableSource;
        [NonSerialized]
        public float trackDuration;
        [NonSerialized]
        public float trackPosition;

        // Constants

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        const short VIDEO_SOURCE_NONE = 0;
        const short VIDEO_SOURCE_AVPRO = 1;
        const short VIDEO_SOURCE_UNITY = 2;

        const int SOURCE_TYPE_URL = 0;
        const int SOURCE_TYPE_STATIC = 1;

        bool _hasSustainZone = false;
        bool _inSustainZone = false;
        bool _triggerZoneSame = false;

        bool _isStreamPlayer;
        int _urlSourceType;
        BaseVRCVideoPlayer _currentPlayer;

        float _lastVideoPosition = 0;

        VRCUrl playAtUrl;
        float playAt = 0;
        float playStartTime = 0;
        //float trackDuration = 0;

        void Start()
        {
            _hasSustainZone = Utilities.IsValid(playbackZone);
            if (_hasSustainZone)
            {
                _inSustainZone = playbackZone._LocalPlayerInZone();
                playbackZone._Register((UdonBehaviour)(Component)this, "_PlaybackZoneEnter", "_PlaybackZoneExit", null);
            }

            if (Utilities.IsValid(triggerZone))
            {
                if (_hasSustainZone && triggerZone == playbackZone)
                    _triggerZoneSame = true;
                else
                    triggerZone._Register((UdonBehaviour)(Component)this, "_TriggerPlay", null, null);
            }

            if (Utilities.IsValid(staticUrlSource))
            {
                _urlSourceType = SOURCE_TYPE_STATIC;
                staticUrlSource._RegisterPlayer((UdonBehaviour)GetComponent(typeof(UdonBehaviour)));
            }
            else
                _urlSourceType = SOURCE_TYPE_URL;

            if (Utilities.IsValid(avProVideo))
            {
                avProVideo.Loop = false;
                avProVideo.Stop();
            }
            if (Utilities.IsValid(unityVideo))
            {
                unityVideo.Loop = loop;
                unityVideo.Stop();
            }

            if (Utilities.IsValid(avProVideo))
                _UpdateVideoSource(VIDEO_SOURCE_AVPRO, videoSourceOverride);
            else
                _UpdateVideoSource(VIDEO_SOURCE_UNITY, videoSourceOverride);

            _UpdatePlayerState(PLAYER_STATE_STOPPED);
        }

        public void _TriggerPlay()
        {
            Debug.Log("[VideoTXL:ZonedStreamPlayer] Trigger play");
            if (playAt > 0 || localPlayerState == PLAYER_STATE_PLAYING || localPlayerState == PLAYER_STATE_LOADING)
                return;

            _PlayVideoAfter(_GetSelectedUrl(), 0);
        }

        public void _TriggerStop()
        {
            _StopVideo();
        }

        public void _PlaybackZoneEnter()
        {
            _inSustainZone = true;

            if (_triggerZoneSame)
                _TriggerPlay();
        }

        public void _PlaybackZoneExit()
        {
            _inSustainZone = false;
            _TriggerStop();
        }

        public void _Resync()
        {
            _StopVideo();
            _PlayVideo(_GetSelectedUrl());
        }

        public void _UrlChanged()
        {
            if (localPlayerState == PLAYER_STATE_PLAYING || localPlayerState == PLAYER_STATE_LOADING)
                _Resync();
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

        void _PlayVideo(VRCUrl url)
        {
            playAt = 0;
            if (!Utilities.IsValid(url))
                return;

            DebugLog("Play video " + url);

            string urlStr = url.Get();
            if (urlStr == null || urlStr == "")
                return;

            _UpdatePlayerState(PLAYER_STATE_LOADING);

#if !UNITY_EDITOR
            _currentPlayer.LoadURL(url);
#endif
        }

        void _StopVideo()
        {
            DebugLog("Stop video");

            if (seekableSource && resumePosition)
                _lastVideoPosition = _currentPlayer.GetTime();

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            _currentPlayer.Stop();

            playAt = 0;
        }

        public override void OnVideoReady()
        {
            float position = _currentPlayer.GetTime();
            float duration = _currentPlayer.GetDuration();
            DebugLog("Video ready, duration: " + duration + ", position: " + position);

            if (_hasSustainZone && !_inSustainZone)
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            // If a seekable video is loaded it should have a positive duration.  Otherwise we assume it's a non-seekable stream
            seekableSource = !float.IsInfinity(duration) && !float.IsNaN(duration) && duration > 1;
            dataProxy.seekableSource = seekableSource;
            _UpdateTracking(position, position, duration);

            _currentPlayer.Play();
        }

        public override void OnVideoStart()
        {
            DebugLog("Video start");

            if (_hasSustainZone && !_inSustainZone)
            {
                DebugLog("Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            _UpdatePlayerState(PLAYER_STATE_PLAYING);
            //_UpdatePlayerPaused(false);
            playStartTime = Time.time;

            if (seekableSource)
            {
                _currentPlayer.SetTime(_lastVideoPosition);
                _lastVideoPosition = 0;
            }
        }

        public override void OnVideoEnd()
        {
            if (!seekableSource && Time.time - playStartTime < 1)
            {
                DebugLog("Video end encountered at start of stream, ignoring");
                return;
            }

            DebugLog("Video end");

            seekableSource = false;
            dataProxy.seekableSource = false;

            _UpdatePlayerState(PLAYER_STATE_STOPPED);

            _lastVideoPosition = 0;

            // TODO: Loop for AVPro
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (localPlayerState == PLAYER_STATE_STOPPED)
                return;

            _currentPlayer.Stop();

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

        public void _StopAVPro()
        {
            avProVideo.Stop();
        }

        public void _StopUnity()
        {
            unityVideo.Stop();
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

            if (state != PLAYER_STATE_PLAYING)
            {
                dataProxy.paused = false;
                dataProxy.syncing = false;
            }

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

        void _UpdateTracking(float position, float target, float duration)
        {
            trackPosition = position;
            trackDuration = duration;
            dataProxy.trackPosition = position;
            dataProxy.trackTarget = target;
            dataProxy.trackDuration = duration;
            dataProxy._EmitTrackingUpdate();
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
                debugLog._Write("SyncPlayer", message);
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP && FALSE
    [CustomEditor(typeof(ZonedStreamPlayer))]
    internal class ZonedStreamPlayerInspector : Editor
    {
        SerializedProperty screenManagerProperty;
        SerializedProperty audioManagerProperty;
        SerializedProperty triggerManagerProperty;

        SerializedProperty avProVideoPlayerProperty;
        SerializedProperty unityVideoPlayerProperty;

        SerializedProperty staticUrlSourceProperty;
        SerializedProperty urlProperty;

        SerializedProperty loopProperty;
        SerializedProperty resumePositionProperty;

        private void OnEnable()
        {
            screenManagerProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.screenManager));
            audioManagerProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.audioManager));
            triggerManagerProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.triggerManager));

            avProVideoPlayerProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.avProVideo));
            unityVideoPlayerProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.unityVideo));

            staticUrlSourceProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.staticUrlSource));
            urlProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.streamUrl));

            loopProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.loop));
            resumePositionProperty = serializedObject.FindProperty(nameof(ZonedStreamPlayer.resumePosition));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawConvertToUdonBehaviourButton(target) ||
                UdonSharpGUI.DrawProgramSource(target))
                return;

            EditorGUILayout.PropertyField(screenManagerProperty);
            EditorGUILayout.PropertyField(audioManagerProperty);
            EditorGUILayout.PropertyField(triggerManagerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(unityVideoPlayerProperty);
            EditorGUILayout.PropertyField(avProVideoPlayerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(staticUrlSourceProperty);
            if (staticUrlSourceProperty.objectReferenceValue == null)
                EditorGUILayout.PropertyField(urlProperty);
            if (unityVideoPlayerProperty.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(loopProperty);
                EditorGUILayout.PropertyField(resumePositionProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
