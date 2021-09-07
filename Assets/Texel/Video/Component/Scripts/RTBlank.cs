
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Video/RT Blank")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class RTBlank : UdonSharpBehaviour
    {
        public VideoPlayerProxy dataProxy;
        public Camera blankingCamera;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        void Start()
        {
            if (Utilities.IsValid(dataProxy))
                dataProxy._RegisterEventHandler(this, "_VideoStateUpdate");

            blankingCamera.enabled = false;
        }

        public void _VideoStateUpdate()
        {
            switch (dataProxy.playerState)
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
