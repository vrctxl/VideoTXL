
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ForwardingAclHandler : UdonSharpBehaviour
    {
        public AccessControl acl;
        public AccessControl[] forwardAcls;

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
            for (int i = 0; i < forwardAcls.Length; i++)
            {
                if (!Utilities.IsValid(forwardAcls[i]))
                    continue;

                if (forwardAcls[i]._HasAccess(playerArg))
                {
                    checkResult = RESULT_ALLOW;
                    return;
                }
            }

            checkResult = RESULT_PASS;
        }
    }
}
