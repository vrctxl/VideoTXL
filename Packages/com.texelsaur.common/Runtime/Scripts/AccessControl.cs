
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class AccessControl : EventBase
    {
        [Header("Optional Components")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        public DebugState debugState;

        [Header("Access Options")]
        public bool allowInstanceOwner = true;
        public bool allowMaster = true;
        public bool restrictMasterIfOwnerPresent = false;
        public bool allowWhitelist = false;
        public bool allowAnyone = false;

        [Header("Default Options")]
        [Tooltip("Whether ACL is enforced.  When not enforced, access is always given.")]
        public bool enforce = true;
        [Tooltip("Write out debug info to VRChat log")]
        public bool debugLogging = false;

        [Header("Access Whitelist")]
        [Tooltip("A list of admin users who have access when allow whitelist is enabled")]
        public string[] userWhitelist;
        [Tooltip("A list of user sources to check for whitelisted players")]
        public AccessControlUserSource[] whitelistSources;

        public const int RESULT_ALLOW = 1;
        public const int RESULT_PASS = 0;
        public const int RESULT_DENY = -1;

        bool _localPlayerWhitelisted = false;
        bool _localPlayerMaster = false;
        bool _localPlayerInstanceOwner = false;
        bool _localCalculatedAccess = false;

        bool _worldHasOwner = false;
        VRCPlayerApi[] _playerBuffer = new VRCPlayerApi[100];

        VRCPlayerApi foundMaster = null;
        VRCPlayerApi foundInstanceOwner = null;
        int foundMasterCount = 0;
        int foundInstanceOwnerCount = 0;

        UdonBehaviour cachedAccessHandler;
        Component[] accessHandlers;
        int accessHandlerCount = 0;
        string[] accessHandlerEvents;
        string[] accessHandlerParams;
        string[] accessHandlerResults;

        public const int EVENT_VALIDATE = 0;
        public const int EVENT_ENFORCE_UPDATE = 1;
        public const int EVENT_COUNT = 2;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                if (_PlayerWhitelisted(player))
                    _localPlayerWhitelisted = true;

                _localPlayerMaster = player.isMaster;
                _localPlayerInstanceOwner = player.isInstanceOwner;
            }

            if (Utilities.IsValid(debugState))
                debugState._Regsiter(this, "_UpdateDebugState", "AccessControl");

            DebugLog("Setting up access");
            if (allowInstanceOwner)
                DebugLog($"Instance Owner: {_localPlayerInstanceOwner}");
            if (allowMaster)
                DebugLog($"Instance Master: {_localPlayerMaster}");
            if (allowWhitelist)
                DebugLog($"Whitelist: {_localPlayerWhitelisted}");
            if (allowAnyone)
                DebugLog($"Anyone: True");

            _SearchInstanceOwner();
            _CalculateLocalAccess();

            if (Utilities.IsValid(whitelistSources))
            {
                foreach (AccessControlUserSource source in whitelistSources)
                {
                    if (Utilities.IsValid(source))
                        source._Register(AccessControlUserSource.EVENT_REVALIDATE, this, nameof(_RefreshWhitelistCheck));
                }
            }
        }

        public void _AddUserSource(AccessControlUserSource source)
        {
            if (!source)
                return;

            foreach (var existing in whitelistSources)
            {
                if (existing == source)
                    return;
            }

            whitelistSources = (AccessControlUserSource[])UtilityTxl.ArrayAddElement(whitelistSources, source, source.GetType());
            source._Register(AccessControlUserSource.EVENT_REVALIDATE, this, nameof(_RefreshWhitelistCheck));
        }

        void _CalculateLocalAccess()
        {
            _localCalculatedAccess = false;

            if (allowInstanceOwner && _localPlayerInstanceOwner)
                _localCalculatedAccess = true;
            if (allowWhitelist && _localPlayerWhitelisted)
                _localCalculatedAccess = true;
            if (allowAnyone)
                _localCalculatedAccess = true;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _SearchInstanceOwner();
            _CalculateLocalAccess();

            _UpdateHandlers(EVENT_VALIDATE);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _SearchInstanceOwner();
            _CalculateLocalAccess();

            _UpdateHandlers(EVENT_VALIDATE);
        }

        void _SearchInstanceOwner()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            _playerBuffer = VRCPlayerApi.GetPlayers(_playerBuffer);

            _worldHasOwner = false;
            foundInstanceOwner = null;
            foundInstanceOwnerCount = 0;
            foundMaster = null;
            foundMasterCount = 0;

            for (int i = 0; i < playerCount; i++)
            {
                VRCPlayerApi player = _playerBuffer[i];
                if (!Utilities.IsValid(player) || !player.IsValid())
                    continue;

                if (player.isInstanceOwner)
                {
                    foundInstanceOwner = player;
                    foundInstanceOwnerCount += 1;
                }

                if (player.isMaster)
                {
                    foundMaster = player;
                    foundMasterCount += 1;
                }
            }

            if (foundInstanceOwnerCount > 0)
                _worldHasOwner = true;
        }

        public void _Enforce(bool state)
        {
            enforce = state;

            _UpdateHandlers(EVENT_VALIDATE);
            _UpdateHandlers(EVENT_ENFORCE_UPDATE);
        }

        public void _RefreshWhitelistCheck()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                _localPlayerWhitelisted = _PlayerWhitelisted(player);
                _CalculateLocalAccess();
            }

            DebugLog($"Refresh whitelist local={_localPlayerWhitelisted}");
            _UpdateHandlers(EVENT_VALIDATE);
        }

        public bool _PlayerWhitelisted(VRCPlayerApi player)
        {
            string playerName = player.displayName;
            if (Utilities.IsValid(userWhitelist))
            {
                foreach (string user in userWhitelist)
                {
                    if (playerName == user)
                        return true;
                }
            }

            if (Utilities.IsValid(whitelistSources))
            {
                foreach (AccessControlUserSource source in whitelistSources)
                {
                    if (!Utilities.IsValid(source))
                        continue;

                    if (source._ContainsName(playerName))
                        return true;
                }
            }

            return false;
        }

        public bool _LocalWhitelisted()
        {
            return _localPlayerWhitelisted;
        }

        public bool _HasAccess(VRCPlayerApi player)
        {
            if (!enforce)
                return true;

            if (player == Networking.LocalPlayer)
                return _LocalHasAccess();

            if (!Utilities.IsValid(player))
                return false;

            int handlerResult = _CheckAccessHandlerAccess(player);
            if (handlerResult == RESULT_DENY)
                return false;
            if (handlerResult == RESULT_ALLOW)
                return true;

            if (allowAnyone)
                return true;
            if (allowInstanceOwner && player.isInstanceOwner)
                return true;
            if (allowMaster && player.isMaster && (!restrictMasterIfOwnerPresent || !_worldHasOwner))
                return true;
            if (allowWhitelist && _PlayerWhitelisted(player))
                return true;

            return false;
        }

        public bool _LocalHasAccess()
        {
            if (!enforce)
                return true;

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return false;

            int handlerResult = _CheckAccessHandlerAccess(player);
            if (handlerResult == RESULT_DENY)
                return false;
            if (handlerResult == RESULT_ALLOW)
                return true;

            if (_localCalculatedAccess)
                return true;
            if (allowMaster && player.isMaster && (!restrictMasterIfOwnerPresent || !_worldHasOwner))
                return true;

            return false;
        }

        // One or more registered access handlers have a chance to force-allow or force-deny access for a player.
        // If a handler has no preference, it should return RESULT_PASS (0) to allow the next handler to make a decision
        // or let the local access control settings take effect.

        public void _RegsiterAccessHandler(Component handler, string eventName, string playerParamVar, string resultVar)
        {
            if (!Utilities.IsValid(handler))
                return;

            for (int i = 0; i < accessHandlerCount; i++)
            {
                if (accessHandlers[i] == handler)
                    return;
            }

            accessHandlers = (Component[])UtilityTxl.ArrayAddElement(accessHandlers, handler, typeof(Component));
            accessHandlerEvents = (string[])UtilityTxl.ArrayAddElement(accessHandlerEvents, eventName, typeof(string));
            accessHandlerParams = (string[])UtilityTxl.ArrayAddElement(accessHandlerParams, playerParamVar, typeof(string));
            accessHandlerResults = (string[])UtilityTxl.ArrayAddElement(accessHandlerResults, resultVar, typeof(string));

            DebugLog($"Registered access handler {eventName}");

            cachedAccessHandler = (UdonBehaviour)handler;

            accessHandlerCount += 1;
        }

        [Obsolete("Use _Register(AccessControl.EVENT_VALIDATE, ...)")]
        public void _RegisterValidateHandler(Component handler, string eventName)
        {
            _Register(EVENT_VALIDATE, handler, eventName);
        }

        public void _Validate()
        {
            _UpdateHandlers(EVENT_VALIDATE);
        }

        int _CheckAccessHandlerAccess(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(accessHandlers))
                return RESULT_PASS;

            int handlerCount = accessHandlers.Length;
            if (handlerCount == 0)
                return RESULT_PASS;

            if (handlerCount == 1)
            {
                cachedAccessHandler.SetProgramVariable(accessHandlerParams[0], player);
                cachedAccessHandler.SendCustomEvent(accessHandlerEvents[0]);
                return (int)cachedAccessHandler.GetProgramVariable(accessHandlerResults[0]);
            }

            for (int i = 0; i < handlerCount; i++)
            {
                UdonBehaviour script = (UdonBehaviour)accessHandlers[i];
                if (!Utilities.IsValid(script))
                    continue;

                script.SetProgramVariable(accessHandlerParams[i], player);
                script.SendCustomEvent(accessHandlerEvents[i]);
                int result = (int)script.GetProgramVariable(accessHandlerResults[i]);
                if (result == RESULT_PASS)
                    continue;

                return result;
            }

            return RESULT_PASS;
        }

        void DebugLog(string message)
        {
            if (!debugLogging)
                Debug.Log("[Texel:AccessControl] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AccessControl", message);
        }

        public void _UpdateDebugState()
        {
            debugState._SetValue("localMaster", _localPlayerMaster.ToString());
            debugState._SetValue("localInstanceOwner", _localPlayerInstanceOwner.ToString());
            debugState._SetValue("localWhitelisted", _localPlayerWhitelisted.ToString());
            debugState._SetValue("localCalculated", _localCalculatedAccess.ToString());
            debugState._SetValue("allowMaster", allowMaster.ToString());
            debugState._SetValue("allowInstanceOwner", allowInstanceOwner.ToString());
            debugState._SetValue("allowWhitelist", allowWhitelist.ToString());
            debugState._SetValue("allowAnyone", allowAnyone.ToString());
            debugState._SetValue("restrictMaster", restrictMasterIfOwnerPresent.ToString());
            debugState._SetValue("enforce", enforce.ToString());
            debugState._SetValue("instanceOwner", Utilities.IsValid(foundInstanceOwner) ? foundInstanceOwner.displayName : "--");
            debugState._SetValue("instanceOwnerCount", foundInstanceOwnerCount.ToString());
            debugState._SetValue("master", Utilities.IsValid(foundMaster) ? foundMaster.displayName : "--");
            debugState._SetValue("masterCount", foundMasterCount.ToString());
        }
    }
}