
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncPlayerList : EventBase
    {
        [Tooltip("The maximum number of players that can be accepted into the list")]
        public int maxPlayerCount = 100;
        [Tooltip("Moves players from the end of the list to fill any gaps left by removed players.  More compact and efficient, but player indexes can change.")]
        public bool autoCompact = true;

        [Header("Triggers")]
        [Tooltip("Optional zone trigger that adds players to list when zone is entered.  Zone trigger must not be local-only.")]
        public ZoneTrigger zoneEnterTrigger;
        [Tooltip("Optional zone trigger that removes players from list when zone is left.  Zone trigger must not be local-only.")]
        public ZoneTrigger zoneLeaveTrigger;
        [Tooltip("Fully remove a player from the list when they leave the world.  If left unchecked, a player's name will be kept in the list in case they rejoin.")]
        public bool removePlayersOnLeave = false;

        [Header("Access Control")]
        [Tooltip("Optional ACL to check if player is allowed to perform list change operations")]
        public AccessControl accessControl;
        [Tooltip("Whether list ownership can automatically be transferred to another player initiating a list change operation")]
        public bool allowOwnershipTransfer = false;

        [Header("Debug")]
        public DebugLog debugLog;
        [Tooltip("Log additional lower-level udon events")]
        public bool lowLevelDebug = false;

        [UdonSynced]
        int syncChangeCount = 0;
        [UdonSynced]
        int syncLockoutChangeCount = 0;
        [UdonSynced]
        int syncMaxIndex = -1;
        [UdonSynced]
        string[] syncPlayerNames = new string[0];
        [UdonSynced]
        int[] syncPlayerIds = new int[0];
        [UdonSynced]
        bool[] syncLockout = new bool[0];

        int prevChangeCount = 0;
        int prevLockoutChangeCount = 0;
        int prevMaxIndex = -1;

        [NonSerialized]
        public VRCPlayerApi playerArg;

        public const int EVENT_MEMBERSHIP_CHANGE = 0;
        public const int EVENT_LOCKOUT_CHANGE = 1;
        public const int EVENT_COUNT = 2;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            DebugLog("Init");

            syncPlayerNames = new string[10];
            syncPlayerIds = new int[10];
            syncLockout = new bool[10];

            for (int i = 0; i < syncPlayerNames.Length; i++)
                syncPlayerNames[i] = string.Empty;

            for (int i = 0; i < syncPlayerIds.Length; i++)
                syncPlayerIds[i] = -1;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            if (zoneEnterTrigger)
                zoneEnterTrigger._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, nameof(_OnPlayerEnter), nameof(playerArg));
            if (zoneLeaveTrigger)
                zoneLeaveTrigger._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, nameof(_OnPlayerLeave), nameof(playerArg));
        }

        public void _SetInitialList(string[] names)
        {
            if (!_AccessCheck())
                return;

            int nextSize = (names.Length / 10 + 1) * 10;

            syncPlayerNames = new string[nextSize];
            syncPlayerIds = new int[nextSize];
            syncLockout = new bool[nextSize];

            for (int i = 0; i < names.Length; i++)
                syncPlayerNames[i] = names[i];
            for (int i = names.Length; i < syncPlayerNames.Length; i++)
                syncPlayerNames[i] = string.Empty;

            for (int i = 0; i < syncPlayerIds.Length; i++)
                syncPlayerIds[i] = -1;

            syncMaxIndex = names.Length - 1;
            syncChangeCount += 1;
            syncLockoutChangeCount += 1;

            int playerCount = VRCPlayerApi.GetPlayerCount();
            VRCPlayerApi[] players = new VRCPlayerApi[playerCount];
            players = VRCPlayerApi.GetPlayers(players);

            // Resolve all valid players
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                string name = syncPlayerNames[i];
                for (int j = 0; j < players.Length; j++)
                {
                    VRCPlayerApi player = players[j];
                    if (player.displayName == name)
                    {
                        syncPlayerIds[i] = player.playerId;
                        break;
                    }
                }
            }

            RequestSerialization();
            _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);
            _UpdateHandlers(EVENT_LOCKOUT_CHANGE);
        }

        public int _AddPlayer(VRCPlayerApi player)
        {
            if (!_AccessCheck())
                return -1;
            if (!Utilities.IsValid(player))
                return -1;

            if (!player.IsValid())
            {
                _RemovePlayerChecked(player);
                return -1;
            }

            return _AddPlayerChecked(player);
        }

        int _AddPlayerChecked(VRCPlayerApi player)
        {
            if (autoCompact && syncMaxIndex == maxPlayerCount - 1)
                return -1;

            int id = player.playerId;
            int nextIndex = -1;
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                if (syncPlayerNames[i] == player.displayName)
                    syncPlayerIds[i] = player.playerId;
                if (syncPlayerIds[i] == id)
                    return i;

                if (nextIndex == -1 && syncPlayerNames[i] == null && !syncLockout[i])
                    nextIndex = i;
            }

            if (nextIndex == -1)
            {
                do
                {
                    if (syncMaxIndex == maxPlayerCount - 1)
                        return -1;

                    syncMaxIndex += 1;
                    nextIndex = syncMaxIndex;
                } while (syncLockout[syncMaxIndex]);
            }
            else if (syncMaxIndex < nextIndex)
                syncMaxIndex = nextIndex;

            int arrLength = syncPlayerIds.Length;
            if (syncMaxIndex >= arrLength)
            {
                syncPlayerIds = (int[])UtilityTxl.ArrayMinSize(syncPlayerIds, syncPlayerIds.Length + 10, typeof(int));
                syncPlayerNames = (string[])UtilityTxl.ArrayMinSize(syncPlayerNames, syncPlayerNames.Length + 10, typeof(string));
                syncLockout = (bool[])UtilityTxl.ArrayMinSize(syncLockout, syncLockout.Length + 10, typeof(bool));

                for (int i = arrLength; i < syncPlayerIds.Length; i++)
                    syncPlayerIds[i] = -1;
                for (int i = arrLength; i < syncPlayerNames.Length; i++)
                    syncPlayerNames[i] = string.Empty;
            }

            syncChangeCount += 1;
            syncPlayerIds[syncMaxIndex] = id;
            syncPlayerNames[syncMaxIndex] = player.displayName;
            syncLockout[syncMaxIndex] = false;

            RequestSerialization();
            _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);

            DebugLog($"Added player {syncPlayerNames[syncMaxIndex]} at {syncMaxIndex}");

            return syncMaxIndex;
        }

        public bool _RemovePlayer(VRCPlayerApi player)
        {
            if (!_AccessCheck())
                return false;
            if (!Utilities.IsValid(player))
                return false;

            return _RemovePlayerChecked(player);
        }

        bool _RemovePlayerChecked(VRCPlayerApi player)
        {
            int id = player.playerId;
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                if (syncPlayerIds[i] == id)
                    return _RemovePlayerAtIndexChecked(i);
            }

            return false;
        }

        public bool _RemovePlayerAtIndex(int index)
        {
            if (!_AccessCheck())
                return false;
            if (index < 0)
                return false;
            if (autoCompact && index >= syncMaxIndex)
                return false;
            if (!autoCompact && index >= maxPlayerCount)
                return false;

            return _RemovePlayerAtIndexChecked(index);
        }

        bool _RemovePlayerAtIndexChecked(int index)
        {
            if (syncPlayerIds[index] == -1 && syncPlayerNames[index] == null)
                return false;

            if (syncLockout[index])
                return false;

            if (autoCompact)
            {
                syncPlayerIds[index] = syncPlayerIds[syncMaxIndex];
                syncPlayerNames[index] = syncPlayerNames[syncMaxIndex];
                syncPlayerIds[syncMaxIndex] = -1;
                syncPlayerNames[syncMaxIndex] = null;
                syncMaxIndex -= 1;
            }
            else
            {
                syncPlayerIds[index] = -1;
                syncPlayerNames[index] = null;
            }

            syncChangeCount += 1;

            RequestSerialization();
            _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);

            DebugLog($"Remove player at index {index}");

            return true;
        }

        bool _InvalidatePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return false;

            int id = player.playerId;
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                if (syncPlayerIds[i] == id)
                {
                    syncPlayerIds[i] = -1;
                    syncChangeCount += 1;

                    RequestSerialization();
                    _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);

                    DebugLog($"Invalidate player at index {i}");

                    return true;
                }
            }

            return false;
        }

        bool _RevalidatePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return false;

            int id = player.playerId;
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                string displayName = player.displayName;
                if (syncPlayerNames[i] == displayName)
                {
                    // Don't overrwrite valid player with same display name (simulator support, mainly)
                    if (syncPlayerIds[i] > -1)
                    {
                        VRCPlayerApi playerCheck = VRCPlayerApi.GetPlayerById(syncPlayerIds[i]);
                        if (Utilities.IsValid(playerCheck))
                            return false;
                    }

                    // Otherwise rehydate the inactive player by display name
                    syncPlayerIds[i] = player.playerId;
                    syncChangeCount += 1;

                    RequestSerialization();
                    _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);

                    DebugLog($"Revalidate player at index {i}");
                }

                if (syncPlayerIds[i] == id)
                    return true;
            }

            return false;
        }

        public bool _SetLockout(int index, bool state)
        {
            if (!_AccessCheck())
                return false;
            if (index < 0)
                return false;
            if (autoCompact && index >= syncMaxIndex)
                return false;
            if (!autoCompact && index >= maxPlayerCount)
                return false;

            if (index >= syncLockout.Length)
                syncLockout = (bool[])UtilityTxl.ArrayMinSize(syncLockout, (index + 10) / 10 * 10, typeof(bool));

            if (syncLockout[index] == state)
                return true;

            syncLockout[index] = state;
            syncLockoutChangeCount += 1;

            RequestSerialization();
            _UpdateHandlers(EVENT_LOCKOUT_CHANGE);

            return true;
        }

        public bool _GetLockout(int index)
        {
            if (index < 0)
                return false;
            if (index >= syncLockout.Length)
                return false;

            return syncLockout[index];
        }

        public int _PlayerCount()
        {
            return syncMaxIndex + 1;
        }

        public VRCPlayerApi _GetPlayer(int index)
        {
            if (index < 0 || index > syncMaxIndex)
                return null;

            int id = syncPlayerIds[index];
            if (id == -1)
                return null;

            return VRCPlayerApi.GetPlayerById(id);
        }

        public string _GetPlayerName(int index)
        {
            if (index < 0 || index > syncMaxIndex)
                return null;

            return syncPlayerNames[index];
        }

        public bool _GetPlayerValid(int index)
        {
            if (index < 0 || index > syncMaxIndex)
                return false;

            return syncPlayerIds[index] != -1;
        }

        public int _GetPlayerIndex(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return -1;

            int id = player.playerId;
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                if (syncPlayerIds[i] == id)
                    return i;
            }

            return -1;
        }

        public int _GetPlayerIndex(string name)
        {
            for (int i = 0; i <= syncMaxIndex; i++)
            {
                if (syncPlayerNames[i] == name)
                    return i;
            }

            return -1;
        }

        public bool _ContainsPlayer(VRCPlayerApi player)
        {
            return _GetPlayerIndex(player) > -1;
        }

        public bool _ContainsPlayer(string name)
        {
            return _GetPlayerIndex(name) > -1;
        }

        public void _OnPlayerEnter()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            DebugLowLevel($"Player zone enter: {playerArg.displayName}");

            _AddPlayer(playerArg);
        }

        public void _OnPlayerLeave()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            DebugLowLevel($"Player zone leave: {playerArg.displayName}");

            _RemovePlayer(playerArg);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject))
                return;

            DebugLowLevel($"Player Left: {player.displayName}");

            if (removePlayersOnLeave)
                _RemovePlayer(player);
            else
                _InvalidatePlayer(player);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject))
                return;

            DebugLowLevel($"Player Joined: {player.displayName}");

            _RevalidatePlayer(player);
        }

        public override void OnDeserialization()
        {
            bool change = (syncChangeCount - prevChangeCount) > 0;

            prevChangeCount = syncChangeCount;
            prevMaxIndex = syncMaxIndex;
            if (change)
                _UpdateHandlers(EVENT_MEMBERSHIP_CHANGE);

            bool lockoutChange = (syncLockoutChangeCount - prevLockoutChangeCount) > 0;

            prevLockoutChangeCount = syncLockoutChangeCount;
            if (lockoutChange)
                _UpdateHandlers(EVENT_LOCKOUT_CHANGE);

            DebugLowLevel($"Deserialize: memberChange={change}, lockoutChange={lockoutChange}");
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            DebugLowLevel($"PostSerialize: {result.success}, {result.byteCount} bytes");
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (!accessControl)
                return true;

            bool requesterCheck = accessControl._HasAccess(requestingPlayer) || Networking.IsOwner(requestingPlayer, gameObject);
            bool requesteeCheck = accessControl._HasAccess(requestedOwner);

            DebugLowLevel($"Ownership check: requester={requesterCheck}, requestee={requesteeCheck}");

            return requesterCheck && requesteeCheck;
        }

        bool _AccessCheck()
        {
            if (accessControl && !accessControl._LocalHasAccess())
                return false;

            if (!Networking.IsOwner(gameObject))
            {
                if (!allowOwnershipTransfer)
                    return false;

                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            return true;
        }

        void DebugLog(string message)
        {
            if (debugLog)
                debugLog._Write("PlayerList", message);
        }

        void DebugLowLevel(string message)
        {
            if (debugLog && lowLevelDebug)
                debugLog._Write("PlayerList", message);
        }
    }
}
