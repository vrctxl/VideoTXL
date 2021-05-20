
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AccessControl : UdonSharpBehaviour
{
    public bool allowInstanceOwner;
    public bool allowMaster;
    public bool allowWhitelist;
    public bool allowAnyone;

    [Tooltip("A list of admin users who have access")]
    public string[] userWhitelist;

    bool _localPlayerWhitelisted = false;
    bool _localPlayerMaster = false;
    bool _localPlayerInstanceOwner = false;
    bool _localCalculatedAccess = false;

    void Start()
    {
        if (Utilities.IsValid(userWhitelist))
        {
            string playerName = Networking.LocalPlayer.displayName;
            foreach (string user in userWhitelist)
            {
                if (playerName == user)
                    _localPlayerWhitelisted = true;
            }
        }

        _localPlayerMaster = Networking.LocalPlayer.isMaster;
        _localPlayerInstanceOwner = Networking.LocalPlayer.isInstanceOwner;

        if (allowInstanceOwner && _localPlayerInstanceOwner)
            _localCalculatedAccess = true;
        if (allowWhitelist && _localPlayerWhitelisted)
            _localCalculatedAccess = true;
        if (allowAnyone)
            _localCalculatedAccess = true;

        Debug.Log($"[VideoTXL:AccessControl] Setting up access");
        if (allowInstanceOwner)
            Debug.Log($"[VideoTXL:AccessControl] Instance Owner: {_localPlayerInstanceOwner}");
        if (allowMaster)
            Debug.Log($"[VideoTXL:AccessControl] Instance Master: {_localPlayerMaster}");
        if (allowWhitelist)
            Debug.Log($"[VideoTXL:AccessControl] Whitelist: {_localPlayerWhitelisted}");
        if (allowAnyone)
            Debug.Log($"[VideoTXL:AccessControl] Anyone: True");
    }

    public bool _LocalWhitelisted()
    {
        return _localPlayerWhitelisted;
    }

    public bool _LocalHasAccess()
    {
        return _localCalculatedAccess || (allowMaster && Networking.LocalPlayer.isMaster);
    }
}
