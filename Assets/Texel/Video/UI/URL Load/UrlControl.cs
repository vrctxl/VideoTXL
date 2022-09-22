
using UdonSharp;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlControl : UdonSharpBehaviour
    {
        public TXLVideoPlayer videoPlayer;

        public VRCUrl url;

        public void _Trigger()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._ChangeUrl(url);

            LocalPlayer localPlayer = (LocalPlayer)videoPlayer;
            if (localPlayer && videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                localPlayer._TriggerPlay();
        }

        public override void Interact()
        {
            _Trigger();
        }
    }
}
