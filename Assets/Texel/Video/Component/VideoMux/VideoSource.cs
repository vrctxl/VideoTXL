﻿
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSource : UdonSharpBehaviour
    {
        public VideoMux videoMux;
        public MeshRenderer captureRenderer;
        public AudioSource[] audioSources;
        public string[] audioSourceChannels;

        public bool lowLatency = false;
        public int maxResolution = 720;

        int id = 0;
        BaseVRCVideoPlayer videoPlayer;

        public const short VIDEO_SOURCE_NONE = 0;
        public const short VIDEO_SOURCE_AVPRO = 1;
        public const short VIDEO_SOURCE_UNITY = 2;

        public const short LOW_LATENCY_UNKNOWN = 0;
        public const short LOW_LATENCY_DISABLE = 1;
        public const short LOW_LATENCY_ENABLE = 2;

        public short VideoSourceType { get; private set; }

        public int ID
        {
            get { return id; }
        }

        public BaseVRCVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
        }

        public void _Register(VideoMux mux, int muxId)
        {
            videoMux = mux;
            id = muxId;

            _AutoDetect();
            _InitVideoPlayer();
        }

        void _AutoDetect()
        {
            // The type-based lookup was actually finding and incorrectly casting VRCUnityVideoPlayer components ._.
            VRCAVProVideoPlayer avp = (VRCAVProVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            if (avp != null)
            {
                videoPlayer = avp;
                VideoSourceType = VIDEO_SOURCE_AVPRO;
                return;
            }

            VRCUnityVideoPlayer unity = (VRCUnityVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (unity != null)
            {
                videoPlayer = unity;
                VideoSourceType = VIDEO_SOURCE_UNITY;
                return;
            }

            VideoSourceType = VIDEO_SOURCE_NONE;
        }

        void _InitVideoPlayer()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.Loop = false;
            videoPlayer.EnableAutomaticResync = false;
            videoPlayer.Stop();
        }


        public override void OnVideoReady()
        {
            videoMux._OnVideoReady(id);
        }

        public override void OnVideoStart()
        {
            videoMux._OnVideoStart(id);
        }

        public override void OnVideoEnd()
        {
            videoMux._OnVideoEnd(id);
        }

        public override void OnVideoError(VideoError videoError)
        {
            videoMux._OnVideoError(id, videoError);
        }

        public override void OnVideoLoop()
        {
            videoMux._OnVideoLoop(id);
        }

        public override void OnVideoPause()
        {
            videoMux._OnVideoPause(id);
        }

        public override void OnVideoPlay()
        {
            videoMux._OnVideoPlay(id);
        }

        public void _VideoPlay()
        {
            if (videoPlayer != null)
                videoPlayer.Play();
        }

        public void _VideoPause()
        {
            if (videoPlayer != null)
                videoPlayer.Pause();
        }

        public void _VideoStop()
        {
            if (videoPlayer != null)
                videoPlayer.Stop();
        }

        public void _VideoStop(int frameDelay)
        {
            SendCustomEventDelayedFrames("_VideoStop", frameDelay);
        }

        public void _VideoLoadURL(VRCUrl url)
        {
            if (videoPlayer != null)
                videoPlayer.LoadURL(url);
        }

        public void _VideoSetTime(float time)
        {
            if (videoPlayer != null)
                videoPlayer.SetTime(time);
        }

        public void _SetAVSync(bool state)
        {
            if (videoPlayer != null)
                videoPlayer.EnableAutomaticResync = state;
        }
    }
}