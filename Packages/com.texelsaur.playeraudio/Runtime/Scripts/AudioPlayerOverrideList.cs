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
        const int EVENT_COUNT = 1;

        public AudioOverrideZone[] zones;
        public AudioOverrideSettings[] profiles;
        public bool[] zoneEnabled;

        public DebugLog debugLog;
        public bool vrcLogging = false;

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

        public bool _GetZoneActive(AudioOverrideZone zone)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zone == zones[i])
                {
                    return zoneEnabled[i];
                }
            }
            return false;
        }

        public void _SetZoneActive(bool state)
        {
            for (int i = 0; i < zones.Length; i++)
                _SetZoneActive(i, state);
        }

        public void _SetZoneActive(AudioOverrideZone zone, bool state)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zone == zones[i])
                {
                    _SetZoneActive(i, state);
                    break;
                }
            }
        }

        void _SetZoneActive(int zoneIndex, bool state)
        {
            AudioOverrideZone zone = zones[zoneIndex];
            if (zone && zoneEnabled[zoneIndex] != state)
            {
                zoneEnabled[zoneIndex] = state;

                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(player))
                    _AddPlayerZone(player, zoneIndex);

                zone._RebuildLocal();
            }
        }

        public AudioOverrideSettings _GetZoneSettings(AudioOverrideZone zone)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zone == zones[i])
                {
                    return profiles[i];
                }
            }
            return null;
        }

        public void _SetZoneSettings(AudioOverrideZone zone, AudioOverrideSettings profile)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zone == zones[i])
                {
                    if (profiles[i] != profile)
                    {
                        if (profiles[i] == profile)
                            continue;

                        profiles[i] = profile;

                        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                        if (Utilities.IsValid(player))
                            _AddPlayerZone(player, i);

                        DebugLog($"Set linked zone settings {gameObject.name} from {zone} to {(Utilities.IsValid(profile) ? profile.ToString() : "none")}");
                        zone._RebuildLocal();
                    }
                    break;
                }
            }
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
                if (Utilities.IsValid(newPlayer))
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
                _AddPlayerZone(player, i);
        }

        void _AddPlayerZone(VRCPlayerApi player, int zoneIndex)
        {
            AudioOverrideZone zone = zones[zoneIndex];
            if (!Utilities.IsValid(zone))
                return;

            AudioOverrideSettings profile = profiles[zoneIndex];
            if (!Utilities.IsValid(profile))
                return;

            zone._AddPlayerOverride(player, profile, zoneEnabled[zoneIndex]);
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
