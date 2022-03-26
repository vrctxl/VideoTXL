
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Override Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideManager : UdonSharpBehaviour
    {
        public AudioOverrideZone defaultZone;
        public AudioOverrideZone[] overrideZones;

        bool waitForInit = true;
        int zoneCount = 0;
        VRCPlayerApi[] playerBuffer = new VRCPlayerApi[100];

        AudioOverrideZone cachedLocalZone;

        void Start()
        {
            if (Utilities.IsValid(overrideZones))
                zoneCount = overrideZones.Length;

            for (int i = 0; i < overrideZones.Length; i++)
            {
                if (Utilities.IsValid(overrideZones[i]))
                    overrideZones[i]._Register(this, i);
            }

            if (Utilities.IsValid(defaultZone))
                defaultZone._Register(this, -1);

            SendCustomEventDelayedSeconds("_RebuildLocal", 1f);
        }

        public void _PlayerEnterZone(AudioOverrideZone zone, VRCPlayerApi player)
        {
            if (waitForInit)
                return;

            //Debug.Log($"Player enter zone {zone._ZoneId()}");

            if (player.isLocal)
                _RebuildLocal();
            else
                _RebuildPlayer(player, cachedLocalZone);
        }

        public void _PlayerLeaveZone(AudioOverrideZone zone, VRCPlayerApi player)
        {
            if (waitForInit)
                return;

            //Debug.Log($"Player leave zone {zone._ZoneId()}");

            if (player.isLocal)
                _RebuildLocal();
            else
                _RebuildPlayer(player, cachedLocalZone);
        }

        public void _RebuildLocal()
        {
            waitForInit = false;

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return;

            cachedLocalZone = _FindActiveZone(player);
            //if (!Utilities.IsValid(cachedLocalZone))
            //    Debug.Log("Player not in zone");
            //else
            //    Debug.Log($"Player in zone {cachedLocalZone._ZoneId()}");

            if (!Utilities.IsValid(cachedLocalZone))
                cachedLocalZone = defaultZone;

            _RebuildAll(player, cachedLocalZone);
        }

        void _RebuildAll(VRCPlayerApi localPlayer, AudioOverrideZone localZone)
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            playerBuffer = VRCPlayerApi.GetPlayers(playerBuffer);

            for (int i = 0; i < playerCount; i++)
            {
                VRCPlayerApi player = playerBuffer[i];
                if (player == localPlayer)
                    continue;

                _RebuildPlayer(player, localZone);
            }
        }

        void _RebuildPlayer(VRCPlayerApi player, AudioOverrideZone localZone)
        {
            if (localZone._Apply(player))
                return;

            _ResetSettings(player);
        }

        public AudioOverrideZone _FindActiveZone(VRCPlayerApi player)
        {
            if (zoneCount <= 0)
                return null;

            for (int i = 0; i < overrideZones.Length; i++)
            {
                AudioOverrideZone zone = overrideZones[i];
                if (zone.membership._ContainsPlayer(player))
                    return zone;
            }

            return null;
        }

        void _ResetSettings(VRCPlayerApi player)
        {
            player.SetVoiceGain(15);
            player.SetVoiceDistanceNear(0);
            player.SetVoiceDistanceFar(25);
            player.SetVoiceLowpass(true);
        }
    }
}
