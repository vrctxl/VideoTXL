using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using System;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Player Override List")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioPlayerOverrideList : EventBase
    {
        [Tooltip("Automatically register hooks into a pickup trigger to bind to a player")]
        public PickupTrigger pickupTrigger;

        public const int EVENT_BOUND_PLAYER_CHANGED = 0;
        public const int EVENT_ENABLED_CHANGED = 1;
        const int EVENT_COUNT = 2;

        public AudioOverrideZone[] zones;
        public AudioOverrideSettings[] profiles;

        public DebugLog debugLog;
        public bool vrcLogging = false;

        [UdonSynced, FieldChangeCallback("Enabled")]
        bool syncEnabled = true;
        [UdonSynced, FieldChangeCallback("BoundPlayerID")]
        int syncBoundPlayerID = -1;

        [NonSerialized]
        public VRCPlayerApi playerArg;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            if (Utilities.IsValid(pickupTrigger))
            {
                pickupTrigger._Register(PickupTrigger.EVENT_TRIGGER_ON, this, "_OnTriggerOn");
                pickupTrigger._Register(PickupTrigger.EVENT_TRIGGER_OFF, this, "_OnTriggerOff");
            }
        }

        public void _OnTriggerOn()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }

        public void _OnTriggerOff()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = -1;
            RequestSerialization();
        }

        public void _SetEnabled(bool state)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            Enabled = state;
            RequestSerialization();
        }

        public bool Enabled
        {
            set
            {
                if (syncEnabled == value)
                    return;

                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(player))
                {
                    if (value)
                        _AddPlayer(player);
                    else
                        _RemovePlayer(player);
                }

                syncEnabled = value;

                _UpdateHandlers(EVENT_ENABLED_CHANGED);
            }
            get { return syncEnabled; }
        }

        public int BoundPlayerID
        {
            set
            {
                int previous = syncBoundPlayerID;

                VRCPlayerApi oldPlayer = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(oldPlayer))
                    _RemovePlayer(oldPlayer);

                syncBoundPlayerID = value;
                DebugLog($"Setting override list bound player ID = {value}");

                VRCPlayerApi newPlayer = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(newPlayer) && Enabled)
                    _AddPlayer(newPlayer);

                if (previous != value)
                    _UpdateHandlers(EVENT_BOUND_PLAYER_CHANGED);
            }
            get { return syncBoundPlayerID; }
        }

        public void _AddPlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            for (int i = 0; i < zones.Length; i++)
            {
                AudioOverrideZone zone = zones[i];
                if (!Utilities.IsValid(zone))
                    continue;

                AudioOverrideSettings profile = profiles[i];
                if (!Utilities.IsValid(profile))
                    continue;

                zone._AddPlayerOverride(player, profile);
            }
        }

        public void _RemovePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            for (int i = 0; i < zones.Length; i++)
            {
                AudioOverrideZone zone = zones[i];
                if (!Utilities.IsValid(zone))
                    continue;

                zone._RemovePlayerOverride(player);
            }
        }

        void DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[Texel:AudioOverride] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioOverride", message);
        }
    }
}
