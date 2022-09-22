
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoLockAclHandler : UdonSharpBehaviour
    {
        public AccessControl acl;
        public SyncPlayer videoPlayer;

        [NonSerialized]
        public VRCPlayerApi playerArg;
        [NonSerialized]
        public int checkResult;

        void Start()
        {
            acl._RegsiterAccessHandler(this, "_CheckAccess", "playerArg", "checkResult");
            videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_LOCK_UPDATE, this, "_OnVideoLockUpdate");
        }

        public void _CheckAccess()
        {
            if (!videoPlayer.locked)
                checkResult = AccessControl.RESULT_ALLOW;
            else
                checkResult = AccessControl.RESULT_PASS;
        }

        public void _OnVideoLockUpdate()
        {
            acl._Validate();
        }
    }
}
