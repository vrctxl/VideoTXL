
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
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
            syncPlayer._OnVideoError(videoError);
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
