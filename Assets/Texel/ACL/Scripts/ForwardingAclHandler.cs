
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class ForwardingAclHandler : UdonSharpBehaviour
    {
        public AccessControl acl;
        public AccessControl[] forwardAcls;

        [NonSerialized]
        public VRCPlayerApi playerArg;
        [NonSerialized]
        public int checkResult;

        void Start()
        {
            acl._RegsiterAccessHandler(this, "_CheckAccess", "playerArg", "checkResult");
        }

        public void _CheckAccess()
        {
            for (int i = 0; i < forwardAcls.Length; i++)
            {
                if (!Utilities.IsValid(forwardAcls[i]))
                    continue;

                if (forwardAcls[i]._HasAccess(playerArg))
                {
                    checkResult = AccessControl.RESULT_ALLOW;
                    return;
                }
            }

            checkResult = AccessControl.RESULT_PASS;
        }
    }
}
