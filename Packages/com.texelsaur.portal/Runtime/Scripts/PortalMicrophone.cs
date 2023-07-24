
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PortalMicrophone : UdonSharpBehaviour
    {
        public PickupTrigger microphone;

        public AudioOverrideZone botZone;
        public AudioOverrideZone portalZone;
        public AudioOverrideSettings suppressionSettings;

        public bool suppressForAll = true;

        AudioOverrideSettings defaultSettings;
        AudioOverrideSettings defaultPortalLocalSettings;

        [UdonSynced, FieldChangeCallback("MicActive")]
        bool syncMicActive;

        void Start()
        {
            microphone._Register(PickupTrigger.EVENT_TRIGGER_ON, this, "_OnTriggerOn");
            microphone._Register(PickupTrigger.EVENT_TRIGGER_OFF, this, "_OnTriggerOff");

            defaultSettings = botZone._GetLinkedZoneSettings(portalZone);
            defaultPortalLocalSettings = portalZone._GetLocalSettings();
        }

        public bool MicActive
        {
            get { return syncMicActive; }
            set
            {
                syncMicActive = value;

                _SetPortalSuppressed(value);
            }
        }

        public void _OnTriggerOn()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            MicActive = true;
            RequestSerialization();
        }

        public void _OnTriggerOff()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            MicActive = false;
            RequestSerialization();
        }

        void _SetPortalSuppressed(bool state)
        {
            if (state)
                botZone._SetLinkedZoneSettings(portalZone, suppressionSettings);
            else
                botZone._SetLinkedZoneSettings(portalZone, defaultSettings);

            if (suppressForAll)
            {
                if (state)
                    portalZone._SetLocalSettings(suppressionSettings);
                else
                    portalZone._SetLocalSettings(defaultPortalLocalSettings);
            }
        }
    }
}
