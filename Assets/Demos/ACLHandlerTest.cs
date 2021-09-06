
using System;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ACLHandlerTest : UdonSharpBehaviour
{
    public AccessControl acl;
    public GameObject obj;

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
        checkResult = RESULT_PASS;
    }
}
