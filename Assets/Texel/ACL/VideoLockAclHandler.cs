
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VideoLockAclHandler : UdonSharpBehaviour
    {
        public AccessControl acl;
        public SyncPlayer videoPlayer;

        [NonSerialized]
        public VRCPlayerApi playerArg;
        [NonSerialized]
        public int checkResult;

        const int RESULT_ALLOW = 1;
        const int RESULT_PASS = 0;
        const int RESULT_DENY = -1;

        void Start()
        {
            acl._RegsiterAccessHandler(this, "_CheckAccess", "playerArg", "checkResult");
        }

        public void _CheckAccess()
        {
            if (!videoPlayer.locked)
                checkResult = RESULT_ALLOW;
            else
                checkResult = RESULT_PASS;
        }
    }
}
