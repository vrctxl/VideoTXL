
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    [AddComponentMenu("Texel/VideoTXL/Video Wrapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VideoWrapper : UdonSharpBehaviour
    {
        public SyncPlayer syncPlayer;

        public override void OnVideoReady()
        {
            syncPlayer.OnVideoReady();
        }

        public override void OnVideoStart()
        {
            syncPlayer.OnVideoStart();
        }

        public override void OnVideoEnd()
        {
            syncPlayer.OnVideoEnd();
        }

        public override void OnVideoError(VideoError videoError)
        {
            syncPlayer.OnVideoError(videoError);
        }

        public override void OnVideoLoop()
        {
            syncPlayer.OnVideoLoop();
        }

        public override void OnVideoPause()
        {
            //syncPlayer.OnVideoPause();
        }

        public override void OnVideoPlay()
        {
            //syncPlayer.OnVideoPlay();
        }
    }
}
