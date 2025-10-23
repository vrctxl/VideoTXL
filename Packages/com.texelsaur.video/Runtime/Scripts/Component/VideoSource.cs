﻿
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Texel
{
    public enum VideoSourceBackend
    {
        AVPro = 1,
        Unity = 2,
    }

    public enum VideoSourceLatency
    {
        Standard = 1,
        LowLatency = 2,
    }

    public enum VideoSourceEvent
    {
        Ready,
        Start,
        End,
        Error,
        Loop,
        Pause,
        Play,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSource : UdonSharpBehaviour
    {
        [Tooltip("The main video source manager")]
        public VideoManager videoMux;
        [Tooltip("Internal object for capturing the video source's render texture.")]
        public MeshRenderer captureRenderer;
        [Tooltip("If multiple resolutions are available for a given URL, the video source will attempt to load the video with the largest resolution that is equal to or smaller than this limit.")]
        public int maxResolution = 720;

        [Tooltip("The audio group definitions associated with this source.  This list is usually auto-generated from the update components button in the main video player inspector.")]       
        public VideoSourceAudioGroup[] audioGroups;

        [Tooltip("A special audio source for AVPro video sources that's enabled for all audio groups and required for proper functioning of audio group switching.  This source should usually be used for AudioLink, and will be used if no override is specified in an audio group.")]
        public AudioSource avproReservedChannel;
        [Tooltip("Whether this source has AVPro's low latency option enabled.  Low latency is necessary for some sources like VRCDN RSTP URLs.")]
        public bool lowLatency = false;

        [NonSerialized]
        internal bool traceLogging = false;

        int id = 0;
        BaseVRCVideoPlayer videoPlayer;

        public const short VIDEO_SOURCE_NONE = 0;
        public const short VIDEO_SOURCE_AVPRO = 1;
        public const short VIDEO_SOURCE_UNITY = 2;

        public const short LOW_LATENCY_UNKNOWN = 0;
        public const short LOW_LATENCY_DISABLE = 1;
        public const short LOW_LATENCY_ENABLE = 2;

        public short VideoSourceType { get; private set; }
        public VideoSourceEvent LastEvent { get; private set; }

        public int ID
        {
            get { return id; }
        }

        public BaseVRCVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
        }

        public void _Register(VideoManager mux, int muxId)
        {
            videoMux = mux;
            id = muxId;

            _CheckIntegrity();
            _AutoDetect();
            _InitVideoPlayer();
        }

        void _CheckIntegrity()
        {
            if (!videoMux)
            {
                Debug.LogError($"Video source {id} registered without a valid video manager.");
                videoMux = gameObject.transform.parent.GetComponentInParent<VideoManager>();
                if (videoMux)
                    _DebugLog($"Found video manager on parent: {videoMux.gameObject.name}");
                else
                    Debug.LogError("Could not find parent video manager.  Video playback via this source will not work.");
            }

            // Try to repair missing required components
            if (!captureRenderer)
            {
                Debug.LogError($"Video source {id} missing captureRenderer.");

                captureRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
                if (captureRenderer)
                    _DebugLog($"Found child renderer on: {captureRenderer.gameObject.name}");
                else
                    Debug.LogError("Could not find child renderer.  Video playback via this source will not work.");
            }

            if (audioGroups.Length == 0)
                Debug.LogError($"Video source {id} has no audio groups.  Try updating connected components on the main video player object.");
        }

        void _AutoDetect()
        {
            // The type-based lookup was actually finding and incorrectly casting VRCUnityVideoPlayer components ._.
            VRCAVProVideoPlayer avp = (VRCAVProVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            if (avp != null)
            {
                videoPlayer = avp;
                VideoSourceType = VIDEO_SOURCE_AVPRO;

                if (!avproReservedChannel)
                {
                    Debug.LogError($"Video source {id} is an AVPro video source, but has no audio source set on its reserved channel.");

                    Transform reserved = transform.Find("ReservedAudioSource");
                    if (reserved)
                    {
                        avproReservedChannel = transform.GetComponent<AudioSource>();
                        if (avproReservedChannel)
                            _DebugLog($"Found suspected reserved audio source on: {reserved.name}");
                        else
                            _DebugLog($"Could not infer reserved audio source.  Some audio playback functions may not work correctly.");
                    }
                }

                /*if (avp.UseLowLatency != lowLatency)
                {
                    lowLatency = avp.UseLowLatency;
                    _DebugLog($"Low latency mismatch, using = {lowLatency}");
                }*/

                /*if (avp.MaximumResolution != maxResolution)
                {
                    maxResolution = avp.MaximumResolution;
                    _DebugLog($"Max resolution mismatch, using = {maxResolution}");
                }*/
                return;
            }

            VRCUnityVideoPlayer unity = (VRCUnityVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (unity != null)
            {
                videoPlayer = unity;
                VideoSourceType = VIDEO_SOURCE_UNITY;

                /*if (unity.MaximumResolution != maxResolution)
                {
                    maxResolution = unity.MaximumResolution;
                    _DebugLog($"Max resolution mismatch, using = {maxResolution}");
                }*/
                return;
            }

            Debug.LogError($"Video source {id} has no attached VRCUnityVideoPlayer or VRCAVProVideoPlayer component.");

            VideoSourceType = VIDEO_SOURCE_NONE;
        }

        void _InitVideoPlayer()
        {
            if (videoPlayer == null)
                return;

            _VideoSetLoop(false);
            _SetAVSync(false);
            _VideoStop();
        }

        public string _FormattedAttributes()
        {
            string str = "None";
            if (VideoSourceType == VIDEO_SOURCE_AVPRO)
                str = $"AVPro, res={maxResolution}, ll={lowLatency}";
            else if (VideoSourceType == VIDEO_SOURCE_UNITY)
                str = $"Unity, res={maxResolution}";
            return str;
        }


        public override void OnVideoReady()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoReady, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Ready;
            if (videoMux)
                videoMux._OnVideoReady(id);
        }

        public override void OnVideoStart()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoStart, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Start;
            if (videoMux)
                videoMux._OnVideoStart(id);
        }

        public override void OnVideoEnd()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoEnd, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.End;
            if (videoMux)
                videoMux._OnVideoEnd(id);
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoError ({videoError}), FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Error;
            if (videoMux)
                videoMux._OnVideoError(id, videoError);
        }

        public override void OnVideoLoop()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoLoop, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Loop;
            if (videoMux)
                videoMux._OnVideoLoop(id);
        }

        public override void OnVideoPause()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoPause, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Pause;
            if (videoMux)
                videoMux._OnVideoPause(id);
        }

        public override void OnVideoPlay()
        {
            if (traceLogging) _DebugTrace($"Trace: Event OnVideoPlay, FC={Time.frameCount}");

            LastEvent = VideoSourceEvent.Play;
            if (!videoMux)
                videoMux._OnVideoPlay(id);
        }

        public void _VideoPlay()
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: Play (_VideoPlay, FC={Time.frameCount})");
            videoPlayer.Play();
            if (traceLogging) _DebugTrace("Trace:   Play -> Done");
        }

        public void _VideoPause()
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: Pause (_VideoPause, FC={Time.frameCount})");
            videoPlayer.Pause();
            if (traceLogging) _DebugTrace("Trace:   Pause -> Done");
        }

        public void _VideoStop()
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: Stop (_VideoStop, FC={Time.frameCount})");
            videoPlayer.Stop();
            if (traceLogging) _DebugTrace("Trace:   Stop -> Done");
        }

        public void _VideoStop(int frameDelay)
        {
            SendCustomEventDelayedFrames("_VideoStop", frameDelay);
        }

        public void _VideoLoadURL(VRCUrl url)
        {
#if UNITY_EDITOR
            if (VideoSourceType == VIDEO_SOURCE_AVPRO)
            {
                if (!videoMux || !videoMux.AVProInEditor)
                {
                    if (videoMux)
                        videoMux._OnVideoError(id, VideoErrorTXL.NoAVProInEditor);
                    return;
                }
            }
#endif

            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: LoadURL({url}) (_VideoLoadURL, FC={Time.frameCount})");
            videoPlayer.LoadURL(url);
            if (traceLogging) _DebugTrace("Trace:   LoadURL -> Done");
        }

        public void _VideoSetTime(float time)
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: SetTime({time}) (_VideoSetTime, FC={Time.frameCount})");
            videoPlayer.SetTime(time);
            if (traceLogging) _DebugTrace("Trace:   SetTime -> Done");
        }

        public void _VideoSetLoop(bool state)
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: Loop = {state} (_VideoSetLoop, FC={Time.frameCount})");
            videoPlayer.Loop = state;
            if (traceLogging) _DebugTrace("Trace:   Loop -> Done");
        }

        public void _SetAVSync(bool state)
        {
            if (!videoPlayer)
                return;

            if (traceLogging) _DebugTrace($"Trace: EnableAutomaticResync = {state} (_SetAVSync, FC={Time.frameCount})");
            videoPlayer.EnableAutomaticResync = state;
            if (traceLogging) _DebugTrace("Trace:   EnableAutomaticResync -> Done");
        }

        void _DebugLog(string message)
        {
            if (videoMux)
                videoMux._DownstreamDebugLog(this, message);
        }

        void _DebugTrace(string message)
        {
            if (traceLogging)
                _DebugLog(message);
        }

        public static string _VideoSourceEventName(VideoSourceEvent val)
        {
            if (val == VideoSourceEvent.End) return "End";
            if (val == VideoSourceEvent.Error) return "Error";
            if (val == VideoSourceEvent.Loop) return "Loop";
            if (val == VideoSourceEvent.Pause) return "Pause";
            if (val == VideoSourceEvent.Play) return "Play";
            if (val == VideoSourceEvent.Ready) return "Ready";
            if (val == VideoSourceEvent.Start) return "Start";

            return "Unknown";
        }
    }
}
