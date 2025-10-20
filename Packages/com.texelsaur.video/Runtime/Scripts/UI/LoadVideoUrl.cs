
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LoadVideoUrl : UdonSharpBehaviour
    {
        public TXLVideoPlayer videoPlayer;
        public VRCUrl url;

        public void _Load()
        {
            if (videoPlayer && url != null)
                videoPlayer._ChangeUrl(url);
        }
    }
}
