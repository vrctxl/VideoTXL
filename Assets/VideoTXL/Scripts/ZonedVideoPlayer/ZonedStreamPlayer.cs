
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

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Specialty/Zoned Stream Player")]
    public class ZonedStreamPlayer : UdonSharpBehaviour
    {
        public ScreenManager screenManager;
        public VolumeController audioManager;
        public TriggerManager triggerManager;

        public StaticUrlSource staticUrlSource;
        public VRCUrl streamUrl;

        public bool loop;
        public bool resumePosition;

        public bool retryOnError = true;
        public float retryTimeout = 6;
        
        bool _rtsptSource = false;

        const int SCREEN_SOURCE_UNITY = 0;
        const int SCREEN_SOURCE_AVPRO = 1;

        const int SCREEN_MODE_NORMAL = 0;
        const int SCREEN_MODE_LOGO = 1;
        const int SCREEN_MODE_LOADING = 2;
        const int SCREEN_MODE_ERROR = 3;
        const int SCREEN_MODE_AUDIO = 4;

        const int SOURCE_TYPE_URL = 0;
        const int SOURCE_TYPE_STATIC = 1;

        [Tooltip("Video Player Reference")]
        public VRCUnityVideoPlayer unityVideo;
        [Tooltip("Stream Player Reference")]
        public VRCAVProVideoPlayer avProVideo;

        bool _hasScreenManager = false;
        bool _hasTriggerManager = false;

        bool _isStreamPlayer;
        int _urlSourceType;
        BaseVRCVideoPlayer _currentPlayer;

        float _lastVideoPosition = 0;

        VRCUrl playAtUrl;
        float playAt = 0;
        bool playingOrLoading = false;

        void Start()
        {
            if (!Utilities.IsValid(screenManager))
                screenManager = GetComponentInChildren<ScreenManager>();
            _hasScreenManager = Utilities.IsValid(screenManager);

            if (!Utilities.IsValid(triggerManager))
                triggerManager = GetComponentInChildren<TriggerManager>();
            _hasTriggerManager = Utilities.IsValid(triggerManager);

            if (Utilities.IsValid(staticUrlSource))
            {
                _urlSourceType = SOURCE_TYPE_STATIC;
                staticUrlSource._RegisterPlayer((UdonBehaviour)GetComponent(typeof(UdonBehaviour)));
            }
            else
                _urlSourceType = SOURCE_TYPE_URL;

            if (unityVideo != null)
            {
                _currentPlayer = unityVideo;
                _UpdateScreenSource(SCREEN_SOURCE_UNITY);
            }
            else
            {
                _currentPlayer = avProVideo;
                _UpdateScreenSource(SCREEN_SOURCE_AVPRO);
            }

            _currentPlayer.Loop = false;
            if (!_isStreamPlayer)
                _currentPlayer.Loop = loop;

            _currentPlayer.Stop();

            _UpdateScreenMaterial(SCREEN_MODE_LOGO);

            if (_hasTriggerManager)
                triggerManager._RegisterPlayer((UdonBehaviour)GetComponent(typeof(UdonBehaviour)));
        }

        public void _TriggerPlay()
        {
            if (playAt > 0 || playingOrLoading)
                return;

            _PlayVideoAfter(_GetSelectedUrl(), 0);
        }

        public void _TriggerStop()
        {
            _StopVideo();
            _UpdateScreenMaterial(SCREEN_MODE_LOGO);
        }

        public void _Resync()
        {
            _StopVideo();
            _PlayVideo(_GetSelectedUrl());
        }

        public void _UrlChanged()
        {
            if (playingOrLoading)
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
            if (url == null || url.Get() == "")
                return;

            Debug.Log("[VideoTXL:ZonedStreamPlayer] Play video stream " + url);

            if (url != null)
            {
                string urlStr = url.Get();

                // RTSPT sources (and maybe others!?) trigger a spontaneous OnVideoEnd event at video start
                if (unityVideo == null && urlStr.Contains("rtspt://"))
                {
                    _rtsptSource = true;
                    Debug.Log("[VideoTXL:ZonedStreamPlayer] Detected RTSPT source");
                }
                else
                    _rtsptSource = false;
                _rtsptSource = true;
            }

            //if (audio != null)
            //    audio.enabled = true;

            playingOrLoading = true;

            _UpdateScreenMaterial(SCREEN_MODE_LOADING);
            _currentPlayer.Stop();
#if !UNITY_EDITOR
            _currentPlayer.LoadURL(url);
#endif
        }

        void _StopVideo()
        {
            if (!_isStreamPlayer)
                _lastVideoPosition = _currentPlayer.GetTime();
            _currentPlayer.Stop();

            playAt = 0;
            playingOrLoading = false;

            //if (audio != null)
            //    audio.enabled = false;
        }

        public override void OnVideoReady()
        {
            Debug.Log("[VideoTXL:ZonedStreamPlayer] Video ready");

            if (_hasTriggerManager && !triggerManager._IsTriggerActive())
            {
                Debug.Log("[VideoTXL:ZonedStreamPlayer] Canceling video: trigger not active");
                _StopVideo();
                return;
            }

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStart();

            _currentPlayer.Play();
        }

        public override void OnVideoStart()
        {
            Debug.Log("[VideoTXL:ZonedStreamPlayer] Video start");

            if (_hasTriggerManager && !triggerManager._IsTriggerActive())
            {
                Debug.Log("[VideoTXL:ZonedStreamPlayer] Canceling video: trigger not active");
                _StopVideo();

                if (Utilities.IsValid(audioManager))
                    audioManager._VideoStop();
            }
            else
            {
                _UpdateScreenMaterial(SCREEN_MODE_NORMAL);
                if (!_isStreamPlayer)
                {
                    _currentPlayer.SetTime(_lastVideoPosition);
                    _lastVideoPosition = 0;
                }
            }
        }

        public override void OnVideoEnd()
        {
            if (_rtsptSource)
            {
                Debug.Log("[VideoTXL:ZonedStreamPlayer] Video ended (ignored) for RTSPT source");
                return;
            }

            playingOrLoading = false;

            Debug.Log("[VideoTXL:ZonedStreamPlayer] Video end");
            _lastVideoPosition = 0;
            _UpdateScreenMaterial(SCREEN_MODE_LOGO);

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStop();
        }

        public override void OnVideoError(VideoError videoError)
        {
            _currentPlayer.Stop();

            VRCUrl url = _GetSelectedUrl();
            Debug.LogError("[VideoTXL:ZonedStreamPlayer] Video stream failed: " + url);
            Debug.LogError("[VideoTXL:ZonedStreamPlayer] " + videoError);

            playingOrLoading = false;

            if (_hasScreenManager)
                screenManager._UpdateVideoError(videoError);

            _UpdateScreenMaterial(SCREEN_MODE_ERROR);

            if (Utilities.IsValid(audioManager))
                audioManager._VideoStop();

            if (retryOnError)
                _PlayVideoAfter(url, retryTimeout);
        }

        void _UpdateScreenMaterial(int screenMode)
        {
            if (_hasScreenManager)
                screenManager._UpdateScreenMaterial(screenMode);
        }

        void _UpdateScreenSource(int screenSource)
        {
            _isStreamPlayer = screenSource == SCREEN_SOURCE_AVPRO;
            if (_hasScreenManager)
                screenManager._UpdateScreenSource(screenSource);
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
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
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
