
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/VideoTXL/Video Wrapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VideoWrapper : UdonSharpBehaviour
    {
        public SyncPlayer syncPlayer;
        public string sourceName = "";

        public bool lowLatency = false;
        public int maxResolution = 720;

        public const short VIDEO_SOURCE_NONE = 0;
        public const short VIDEO_SOURCE_AVPRO = 1;
        public const short VIDEO_SOURCE_UNITY = 2;

        public short VideoSource { get; private set; }

        void _AutoDetect()
        {
            VRCAVProVideoPlayer avp = GetComponent<VRCAVProVideoPlayer>();
            if (avp != null)
            {
                VideoSource = VIDEO_SOURCE_AVPRO;
                return;
            }

            VRCUnityVideoPlayer unity = GetComponent<VRCUnityVideoPlayer>();
            if (unity != null)
            {
                VideoSource = VIDEO_SOURCE_UNITY;
                return;
            }

            VideoSource = VIDEO_SOURCE_NONE;
        }


        public override void OnVideoReady()
        {
            _DebugLog("Video ready");
            syncPlayer._OnVideoReady();
        }

        public override void OnVideoStart()
        {
            _DebugLog("Video start");
            syncPlayer._OnVideoStart();
        }

        public override void OnVideoEnd()
        {
            _DebugLog("Video end");
            syncPlayer._OnVideoEnd();
        }

        public override void OnVideoError(VideoError videoError)
        {
            _DebugLog($"Video error: {videoError}");
            syncPlayer._OnVideoError();
        }

        public override void OnVideoLoop()
        {
            _DebugLog("Video loop");
            syncPlayer._OnVideoLoop();
        }

        public override void OnVideoPause()
        {
            _DebugLog("Video pause");
            //syncPlayer.OnVideoPause();
        }

        public override void OnVideoPlay()
        {
            _DebugLog("Video play");
            //syncPlayer.OnVideoPlay();
        }

        void _DebugLog(string message)
        {
            if (syncPlayer.debugLogging)
                Debug.Log($"[VideoTXL:{sourceName}] " + message);
            if (Utilities.IsValid(syncPlayer.debugLog))
                syncPlayer.debugLog._Write(sourceName, message);
        }
    }
}
