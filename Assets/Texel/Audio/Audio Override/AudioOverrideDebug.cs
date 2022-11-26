
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioOverrideDebug : UdonSharpBehaviour
    {
        [Header("UI")]
        public Text[] titleText;
        public Text[] localText;
        public Text[] nameCol;
        public Text[] zoneCol;
        public Text[] profileCol;
        public Text[] zoneNameCol;
        public Text[] zoneProfileCol;

        int playerCount;
        VRCPlayerApi[] players;

        string[] names = new string[100];
        string[] zones = new string[100];
        string[] profiles = new string[100];
        string[] zoneNames = new string[0];
        string[] zoneProfiles = new string[0];

        string[] nameBuffer = new string[0];
        string[] zoneBuffer = new string[0];
        string[] profileBuffer = new string[0];

        bool queuedUpdate = false;
        bool queuedZoneUpdate = false;

        AudioOverrideManager manager;

        void Start()
        {
            players = new VRCPlayerApi[100];
        }

        public void _UpdateLocal(AudioOverrideZone zone)
        {
            string local = "Local Zone: ";
            if (!zone)
                local += "[none]";
            else
            {
                local += zone.name;
                if (zone.localZoneEnabled)
                    local += " [L]";
                if (zone.defaultEnabled)
                    local += " [D]";
            }

            foreach (Text text in localText)
                text.text = local;
        }

        public void _UpdatePlayer(VRCPlayerApi player, string zone, string profile)
        {
            player.SetPlayerTag("TXL_AO_Zone", zone);
            player.SetPlayerTag("TXL_AO_Profile", profile);

            if (!queuedUpdate)
            {
                queuedUpdate = true;
                SendCustomEventDelayedFrames("_Update", 1);
            }
        }

        public void _SetManager(AudioOverrideManager manager)
        {
            this.manager = manager;
        }

        public void _UpdateZoneData()
        {
            if (!queuedZoneUpdate)
            {
                queuedZoneUpdate = true;
                SendCustomEventDelayedFrames("_CommitUpdateZoneData", 1);
            }
        }

        public void _CommitUpdateZoneData()
        {
            queuedZoneUpdate = false;
            if (!manager)
                return;

            int lineCount = _GetZoneDataCount(manager);
            if (lineCount != zoneNames.Length)
            {
                zoneNames = new string[lineCount];
                zoneProfiles = new string[lineCount];
            }

            int index = _AddZoneToData(manager.defaultZone, 0);
            foreach (AudioOverrideZone zone in manager.overrideZones)
                index = _AddZoneToData(zone, index);

            string joinedNames = string.Join("\n", zoneNames);
            string joinedProfiles = string.Join("\n", zoneProfiles);

            for (int i = 0; i < nameCol.Length; i++)
            {
                zoneNameCol[i].text = joinedNames;
                zoneProfileCol[i].text = joinedProfiles;
            }
        }

        int _AddZoneToData(AudioOverrideZone zone, int index)
        {
            zoneNames[index] = zone.name;
            if (zone.membership)
                zoneNames[index] += $" (M={zone.membership._PlayerCount()})";
            zoneProfiles[index] = "";
            index += 1;

            zoneNames[index] = $"  {(zone.localZoneEnabled ? ' ' : 'X')} [LOCAL]";
            zoneProfiles[index] = zone.localZoneSettings ? zone.localZoneSettings.name : "";
            index += 1;

            zoneNames[index] = $"  {(zone.defaultEnabled ? ' ' : 'X')} [DEFAULT]";
            zoneProfiles[index] = zone.defaultSettings ? zone.defaultSettings.name : "";
            index += 1;

            for (int i = 0; i < zone.linkedZones.Length; i++)
            {
                zoneNames[index] = $"  {(zone.linkedZoneEnabled[i] ? ' ' : 'X')} <-- {zone.linkedZones[i].name}";
                zoneProfiles[index] = zone.linkedZoneSettings[i] ? zone.linkedZoneSettings[i].name : "";
                index += 1;
            }

            return index;
        }

        int _GetZoneDataCount(AudioOverrideManager manager)
        {
            int count = 0;
            if (manager.defaultZone)
                count += 3 + manager.defaultZone.linkedZones.Length;

            foreach (AudioOverrideZone zone in manager.overrideZones)
                count += 3 + zone.linkedZones.Length;

            return count;
        }

        public void _Update()
        {
            queuedUpdate = false;

            playerCount = VRCPlayerApi.GetPlayerCount();
            players = VRCPlayerApi.GetPlayers(players);

            for (int i = 0; i < playerCount; i++)
            {
                VRCPlayerApi player = players[i];
                if (player == null || !player.IsValid())
                {
                    names[i] = "";
                    zones[i] = "";
                    profiles[i] = "";
                    continue;
                }

                names[i] = player.displayName;
                zones[i] = player.GetPlayerTag("TXL_AO_Zone");
                profiles[i] = player.GetPlayerTag("TXL_AO_Profile");
            }

            if (nameBuffer.Length != playerCount)
            {
                nameBuffer = new string[playerCount];
                zoneBuffer = new string[playerCount];
                profileBuffer = new string[playerCount];
            }

            Array.Copy(names, nameBuffer, playerCount);
            Array.Copy(zones, zoneBuffer, playerCount);
            Array.Copy(profiles, profileBuffer, playerCount);

            string joinedNames = string.Join("\n", nameBuffer);
            string joinedZones = string.Join("\n", zoneBuffer);
            string joinedProfiles = string.Join("\n", profileBuffer);

            for (int i = 0; i < nameCol.Length; i++)
            {
                nameCol[i].text = joinedNames;
                zoneCol[i].text = joinedZones;
                profileCol[i].text = joinedProfiles;
            }
        }
    }
}