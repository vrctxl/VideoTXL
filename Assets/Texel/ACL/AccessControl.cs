
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
        public bool restrictMasterIfOwnerPresent = false;
        public bool allowWhitelist = false;
        public bool allowAnyone = false;

        [Header("Default Options")]
        [Tooltip("Write out debug info to VRChat log")]
        public bool debugLogging = false;

        [Header("Access Whitelist")]
        [Tooltip("A list of admin users who have access when allow whitelist is enabled")]
        public string[] userWhitelist;

        const int RESULT_ALLOW = 1;
        const int RESULT_PASS = 0;
        const int RESULT_DENY = -1;

        bool _localPlayerWhitelisted = false;
        bool _localPlayerMaster = false;
        bool _localPlayerInstanceOwner = false;
        bool _localCalculatedAccess = false;

        bool _worldHasOwner = false;
        VRCPlayerApi[] _playerBuffer = new VRCPlayerApi[100];

        UdonBehaviour cachedAccessHandler;
        Component[] accessHandlers;
        string[] accessHandlerEvents;
        string[] accessHandlerParams;
        string[] accessHandlerResults;

        void Start()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                if (_PlayerWhitelisted(player))
                    _localPlayerWhitelisted = true;

                _localPlayerMaster = player.isMaster;
                _localPlayerInstanceOwner = player.isInstanceOwner;
            }

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

            _SearchInstanceOwner();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _SearchInstanceOwner();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _SearchInstanceOwner();
        }

        void _SearchInstanceOwner()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            _playerBuffer = VRCPlayerApi.GetPlayers(_playerBuffer);

            _worldHasOwner = false;
            for (int i = 0; i < playerCount; i++)
            {
                VRCPlayerApi player = _playerBuffer[i];
                if (Utilities.IsValid(player) && player.IsValid() && player.isInstanceOwner)
                {
                    _worldHasOwner = true;
                    break;
                }
            }
        }

        public bool _PlayerWhitelisted(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(userWhitelist))
                return false;

            string playerName = player.displayName;
            foreach (string user in userWhitelist)
            {
                if (playerName == user)
                    return true;
            }

            return false;
        }

        public bool _LocalWhitelisted()
        {
            return _localPlayerWhitelisted;
        }

        public bool _HasAccess(VRCPlayerApi player)
        {
            bool isMaster = Utilities.IsValid(player) ? player.isMaster : false;

            int handlerResult = _CheckAccessHandlerAccess(player);
            if (handlerResult == RESULT_DENY)
                return false;
            if (handlerResult == RESULT_ALLOW)
                return true;

            if (_localCalculatedAccess)
                return true;

            if (allowMaster && isMaster)
                return !restrictMasterIfOwnerPresent || !_worldHasOwner;

            return false;
        }

        public bool _LocalHasAccess()
        {
            return _HasAccess(Networking.LocalPlayer);
        }

        // One or more registered access handlers have a chance to force-allow or force-deny access for a player.
        // If a handler has no preference, it should return RESULT_PASS (0) to allow the next handler to make a decision
        // or let the local access control settings take effect.

        public void _RegsiterAccessHandler(Component handler, string eventName, string playerParamVar, string resultVar)
        {
            if (!Utilities.IsValid(accessHandlers))
                accessHandlers = new Component[0];

            foreach (Component c in accessHandlers)
            {
                if (c == handler)
                    return;
            }

            int count = accessHandlers.Length;
            Component[] newHandlers = new Component[count + 1];
            string[] newEvents = new string[count + 1];
            string[] newParams = new string[count + 1];
            string[] newReturns = new string[count + 1];
            for (int i = 0; i < count; i++)
            {
                newHandlers[i] = accessHandlers[i];
                newEvents[i] = accessHandlerEvents[i];
                newParams[i] = accessHandlerParams[i];
                newReturns[i] = accessHandlerResults[i];
            }

            newHandlers[count] = handler;
            newEvents[count] = eventName;
            newParams[count] = playerParamVar;
            newReturns[count] = resultVar;

            DebugLog($"Registered access handler {eventName}");

            accessHandlers = newHandlers;
            accessHandlerEvents = newEvents;
            accessHandlerParams = newParams;
            accessHandlerResults = newReturns;

            cachedAccessHandler = (UdonBehaviour)handler;
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
    }
}