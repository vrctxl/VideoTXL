
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Override Zone")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideZone : UdonSharpBehaviour
    {
        public ZoneMembership membership;
        public ZoneTrigger zone;

        public AudioOverrideSettings localZoneSettings;
        public bool localZoneEnabled = true;
        public AudioOverrideZone[] linkedZones;
        public AudioOverrideSettings[] linkedZoneSettings;
        public bool[] linkedZoneEnabled;
        public AudioOverrideSettings defaultSettings;
        public bool defaultEnabled = true;

        public DebugLog debugLog;
        public bool vrcLogging = false;

        [NonSerialized]
        public VRCPlayerApi playerArg;

        AudioOverrideManager manager;
        int managedZoneId = -1;
        bool hasManager = false;
        bool hasMembership = false;

        bool hasLocal = false;
        int linkCount = 0;
        bool hasDefault = false;

        int[] playerOverrides;
        int maxOverrideIndex = -1;
        AudioOverrideSettings[] playerOverrideSettings;
        bool[] playerOverrideEnabled;

        void Start()
        {
            if (Utilities.IsValid(zone))
            {
                zone._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_PlayerEnter", "playerArg");
                zone._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, "_PlayerLeave", "playerArg");
            }

            if (Utilities.IsValid(linkedZones))
                linkCount = linkedZones.Length;

            hasMembership = Utilities.IsValid(membership);
            hasLocal = Utilities.IsValid(localZoneSettings);
            hasDefault = Utilities.IsValid(defaultSettings);

            playerOverrides = new int[100];
            playerOverrideSettings = new AudioOverrideSettings[100];
            playerOverrideEnabled = new bool[100];
        }

        public void _Register(AudioOverrideManager overrideManager, int zoneId)
        {
            manager = overrideManager;
            managedZoneId = zoneId;
            hasManager = Utilities.IsValid(manager);
        }

        public int _ZoneId()
        {
            return managedZoneId;
        }

        public void _SetLocalActive(bool state)
        {
            if (localZoneEnabled != state)
            {
                localZoneEnabled = state;
                DebugLog($"Set local zone {gameObject.name} active={state}");

                if (hasManager)
                {
                    manager._RebuildLocal();
                    manager._UpdateZoneData();
                }
            }
        }

        public AudioOverrideSettings _GetLocalSettings()
        {
            return localZoneSettings;
        }

        public void _SetLocalSettings(AudioOverrideSettings profile)
        {
            if (localZoneSettings == profile)
                return;

            localZoneSettings = profile;
            DebugLog($"Set local zone settings {gameObject.name} {(Utilities.IsValid(profile) ? profile.ToString() : "none")}");

            if (hasManager)
            {
                manager._RebuildLocal();
                manager._UpdateZoneData();
            }
        }

        public void _SetDefaultActive(bool state)
        {
            if (defaultEnabled != state)
            {
                defaultEnabled = state;
                DebugLog($"Set default zone {gameObject.name} active={state}");

                if (hasManager)
                {
                    manager._RebuildLocal();
                    manager._UpdateZoneData();
                }
            }
        }

        public AudioOverrideSettings _GetDefaultSettings()
        {
            return defaultSettings;
        }

        public void _SetDefaultSettings(AudioOverrideSettings profile)
        {
            if (defaultSettings == profile)
                return;

            defaultSettings = profile;
            DebugLog($"Set default zone settings {gameObject.name} {(Utilities.IsValid(profile) ? profile.ToString() : "none")}");

            if (hasManager)
            {
                manager._RebuildLocal();
                manager._UpdateZoneData();
            }
        }

        public bool _GetLinkedZoneActive(AudioOverrideZone zone)
        {
            for (int i = 0; i < linkedZones.Length; i++)
            {
                if (zone == linkedZones[i])
                {
                    return linkedZoneEnabled[i];
                }
            }
            return false;
        }

        public void _SetLinkedZoneActive(AudioOverrideZone zone, bool state)
        {
            for (int i = 0; i < linkedZones.Length; i++)
            {
                if (zone == linkedZones[i])
                {
                    if (linkedZoneEnabled[i] != state)
                    {
                        linkedZoneEnabled[i] = state;
                        DebugLog($"Set linked zone {gameObject.name} <-- {zone.name} active={state}");

                        if (hasManager)
                        {
                            manager._RebuildLocal();
                            manager._UpdateZoneData();
                        }
                    }
                    break;
                }
            }
        }

        public AudioOverrideSettings _GetLinkedZoneSettings(AudioOverrideZone zone)
        {
            for (int i = 0; i < linkedZones.Length; i++)
            {
                if (zone == linkedZones[i])
                {
                    return linkedZoneSettings[i];
                }
            }
            return null;
        }

        public void _SetLinkedZoneSettings(AudioOverrideZone zone, AudioOverrideSettings profile)
        {
            for (int i = 0; i < linkedZones.Length; i++)
            {
                if (zone == linkedZones[i])
                {
                    if (linkedZoneSettings[i] != profile)
                    {
                        if (linkedZoneSettings[i] == profile)
                            continue;

                        linkedZoneSettings[i] = profile;
                        DebugLog($"Set linked zone settings {gameObject.name} from {zone} to {(Utilities.IsValid(profile) ? profile.ToString() : "none")}");

                        if (hasManager)
                        {
                            manager._RebuildLocal();
                            manager._UpdateZoneData();
                        }
                    }
                    break;
                }
            }
        }

        public void _PlayerEnter()
        {
            if (hasMembership)
                membership._AddPlayer(playerArg);
            if (hasManager)
            {
                manager._PlayerEnterZone(this, playerArg);
                manager._UpdateZoneData();
            }
        }

        public void _PlayerLeave()
        {
            if (hasMembership)
                membership._RemovePlayer(playerArg);
            if (hasManager)
            {
                manager._PlayerLeaveZone(this, playerArg);
                manager._UpdateZoneData();
            }
        }

        public bool _ContainsPlayer(VRCPlayerApi player)
        {
            if (!hasMembership)
                return true;

            return membership._ContainsPlayer(player);
        }

        public bool _Apply(VRCPlayerApi player)
        {
            AudioOverrideSettings overrideSettings = _GetPlayerOverride(player);
            if (Utilities.IsValid(overrideSettings))
            {
                overrideSettings._Apply(player);
                DebugAO(player, $"{name} [override]", overrideSettings.name);
                return true;
            }

            //Debug.Log($"{name} hasLocal={hasLocal} enabled={localZoneEnabled} contains={_ContainsPlayer(player)}");
            if (hasLocal && localZoneEnabled && _ContainsPlayer(player))
            {
                if (localZoneSettings)
                {
                    localZoneSettings._Apply(player);
                    DebugAO(player, $"{name} [local]", localZoneSettings.name);
                }
                else
                    DebugAO(player, $"{name} [local]", "[invalid]");

                return true;
            }

            for (int i = 0; i < linkCount; i++)
            {
                AudioOverrideZone zone = linkedZones[i];
                bool zoneEnabled = linkedZoneEnabled[i];
                //Debug.Log($"{name} link={zone.name} enabled={zoneEnabled} contains={zone._ContainsPlayer(player)}");
                if (zoneEnabled && zone._ContainsPlayer(player))
                {
                    if (linkedZoneSettings[i])
                    {
                        linkedZoneSettings[i]._Apply(player);
                        DebugAO(player, $"{name} <- {zone.name}", linkedZoneSettings[i].name);
                    }
                    else
                        DebugAO(player, $"{name} <- {zone.name}", "[invalid]");

                    return true;
                }
            }

            //Debug.Log($"{name} hasDefault={hasDefault} enabled={defaultEnabled}");
            if (hasDefault && defaultEnabled)
            {
                if (defaultSettings)
                {
                    defaultSettings._Apply(player);
                    DebugAO(player, $"{name} [default]", defaultSettings.name);
                }
                else
                    DebugAO(player, $"{name} [default]", "[invalid]");

                return true;
            }

            return false;
        }

        public void _AddPlayerOverride(VRCPlayerApi player, AudioOverrideSettings settings, bool enabled)
        {
            if (!Utilities.IsValid(player))
                return;

            DebugLog($"Add player {player.displayName} to zone {managedZoneId} override");

            if (!player.IsValid())
            {
                _RemovePlayerOverride(player);
                return;
            }

            int id = player.playerId;
            for (int i = 0; i <= maxOverrideIndex; i++)
            {
                if (playerOverrides[i] == id)
                    return;
            }

            maxOverrideIndex += 1;
            playerOverrides[maxOverrideIndex] = id;
            playerOverrideSettings[maxOverrideIndex] = settings;
            playerOverrideEnabled[maxOverrideIndex] = enabled;

            if (hasManager)
                manager._RebuildLocal();
        }

        public void _RemovePlayerOverride(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            DebugLog($"Remove player {player.displayName} from zone {managedZoneId} override");

            int id = player.playerId;
            for (int i = 0; i <= maxOverrideIndex; i++)
            {
                if (playerOverrides[i] == id)
                {
                    playerOverrides[i] = playerOverrides[maxOverrideIndex];
                    playerOverrideSettings[i] = playerOverrideSettings[maxOverrideIndex];
                    playerOverrideEnabled[i] = playerOverrideEnabled[maxOverrideIndex];
                    maxOverrideIndex -= 1;

                    if (hasManager)
                        manager._RebuildLocal();

                    return;
                }
            }
        }

        public AudioOverrideSettings _GetPlayerOverride(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return null;

            int id = player.playerId;
            for (int i = 0; i <= maxOverrideIndex; i++)
            {
                if (playerOverrides[i] == id)
                {
                    if (!playerOverrideEnabled[i])
                        return null;
                    return playerOverrideSettings[i];
                }
            }

            return null;
        }

        public void _RebuildLocal()
        {
            if (hasManager)
                manager._RebuildLocal();
        }

        void DebugAO(VRCPlayerApi player, string zone, string profile)
        {
            if (!hasManager || !manager.debugState)
                return;

            manager.debugState._UpdatePlayer(player, zone, profile);
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
