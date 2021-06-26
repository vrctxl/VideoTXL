
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Access Control")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AccessControl : UdonSharpBehaviour
    {
        [Header("Optional Components")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Header("Access Options")]
        public bool allowInstanceOwner = true;
        public bool allowMaster = true;
        public bool allowWhitelist = false;
        public bool allowAnyone = false;

        [Header("Default Options")]
        [Tooltip("Write out debug info to VRChat log")]
        public bool debugLogging = false;

        [Header("Access Whitelist")]
        [Tooltip("A list of admin users who have access when allow whitelist is enabled")]
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

            DebugLog("Setting up access");
            if (allowInstanceOwner)
                DebugLog($"Instance Owner: {_localPlayerInstanceOwner}");
            if (allowMaster)
                DebugLog($"Instance Master: {_localPlayerMaster}");
            if (allowWhitelist)
                DebugLog($"Whitelist: {_localPlayerWhitelisted}");
            if (allowAnyone)
                DebugLog($"Anyone: True");
        }

        public bool _LocalWhitelisted()
        {
            return _localPlayerWhitelisted;
        }

        public bool _LocalHasAccess()
        {
            return _localCalculatedAccess || (allowMaster && Networking.LocalPlayer.isMaster);
        }

        void DebugLog(string message)
        {
            if (!debugLogging)
                Debug.Log("[Texel:AccessControl] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AccessControl", message);
        }
    }
}