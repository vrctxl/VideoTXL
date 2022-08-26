
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Player Override List")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioPlayerOverrideList : UdonSharpBehaviour
    {
        [Tooltip("Automatically register hooks into a pickup trigger to bind to a player")]
        public PickupTrigger pickupTrigger;

        public AudioOverrideZone[] zones;
        public AudioOverrideSettings[] profiles;

        public DebugLog debugLog;
        public bool vrcLogging = false;

        [UdonSynced, FieldChangeCallback("BoundPlayerID")]
        int syncBoundPlayerID = -1;

        [NonSerialized]
        public VRCPlayerApi playerArg;

        void Start()
        {
            if (Utilities.IsValid(pickupTrigger))
            {
                pickupTrigger._RegisterTriggerOn(this, "_Pickup");
                pickupTrigger._RegisterTriggerOff(this, "_Drop");
            }
        }

        public void _Pickup()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }

        public void _Drop()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = -1;
            RequestSerialization();
        }

        public int BoundPlayerID
        {
            set
            {
                VRCPlayerApi oldPlayer = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(oldPlayer))
                    _RemovePlayer(oldPlayer);

                syncBoundPlayerID = value;
                DebugLog($"Setting override list bound player ID = {value}");

                VRCPlayerApi newPlayer = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (Utilities.IsValid(newPlayer))
                    _AddPlayer(newPlayer);

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
