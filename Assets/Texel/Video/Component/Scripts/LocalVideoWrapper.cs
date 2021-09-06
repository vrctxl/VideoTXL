
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

        public override void OnVideoReady()
        {
            localPlayer.OnVideoReady();
        }

        public override void OnVideoStart()
        {
            localPlayer.OnVideoStart();
        }

        public override void OnVideoEnd()
        {
            localPlayer.OnVideoEnd();
        }

        public override void OnVideoError(VideoError videoError)
        {
            localPlayer.OnVideoError(videoError);
        }

        public override void OnVideoLoop()
        {
            //localPlayer.OnVideoLoop();
        }

        public override void OnVideoPause()
        {
            //localPlayer.OnVideoPause();
        }

        public override void OnVideoPlay()
        {
            //localPlayer.OnVideoPlay();
        }
    }
}
