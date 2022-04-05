
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/VideoTXL/Local Video Wrapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class LocalVideoWrapper : UdonSharpBehaviour
    {
        public LocalPlayer localPlayer;
        public string sourceName = "";

        public override void OnVideoReady()
        {
            _DebugLog("Video ready");
            localPlayer._OnVideoReady();
        }

        public override void OnVideoStart()
        {
            _DebugLog("Video start");
            localPlayer._OnVideoStart();
        }

        public override void OnVideoEnd()
        {
            _DebugLog("Video end");
            localPlayer._OnVideoEnd();
        }

        public override void OnVideoError(VideoError videoError)
        {
            _DebugLog($"Video error: {videoError}");
            localPlayer._OnVideoError(videoError);
        }

        public override void OnVideoLoop()
        {
            _DebugLog("Video loop");
            localPlayer._OnVideoLoop();
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
            if (localPlayer.debugLogging)
                Debug.Log($"[VideoTXL:{sourceName}] " + message);
            if (Utilities.IsValid(localPlayer.debugLog))
                localPlayer.debugLog._Write(sourceName, message);
        }
    }
}
