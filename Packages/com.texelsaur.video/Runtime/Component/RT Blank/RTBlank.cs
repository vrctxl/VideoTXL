
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RTBlank : UdonSharpBehaviour
    {
        public TXLVideoPlayer videoPlayer;
        public Camera blankingCamera;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        void Start()
        {
            if (videoPlayer)
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_OnVideoStateUpdate");

            blankingCamera.enabled = false;
        }

        public void _OnVideoStateUpdate()
        {
            switch (videoPlayer.playerState)
            {
                case PLAYER_STATE_PLAYING:
                    break;
                default:
                    blankingCamera.enabled = true;
                    SendCustomEventDelayedFrames("_DisableCamera", 3);
                    break;
            }
        }

        public void _DisableCamera()
        {
            blankingCamera.enabled = false;
        }
    }
}
