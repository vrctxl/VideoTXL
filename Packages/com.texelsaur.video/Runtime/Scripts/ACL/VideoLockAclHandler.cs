
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoLockAclHandler : AccessControlHandler
    {
        public SyncPlayer videoPlayer;

        [NonSerialized]
        public VRCPlayerApi playerArg;
        [NonSerialized]
        public int checkResult;

        protected override void _Init()
        {
            base._Init();

            if (videoPlayer)
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_LOCK_UPDATE, this, nameof(_OnVideoLockUpdate));
        }

        public override AccessHandlerResult _CheckAccess(VRCPlayerApi player)
        {
            if (!videoPlayer || !videoPlayer.locked)
                return AccessHandlerResult.Allow;
            else
                return AccessHandlerResult.Pass;
        }

        public void _OnVideoLockUpdate()
        {
            _UpdateHandlers(EVENT_REVALIDATE);
        }
    }
}
