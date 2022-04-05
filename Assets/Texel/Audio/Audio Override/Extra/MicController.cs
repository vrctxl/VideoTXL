
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MicController : UdonSharpBehaviour
    {
        public PickupTrigger microphone;
        public Collider microphoneCollider;

        public AudioOverrideZone baseZone;
        public AudioOverrideZone aoeZone;
        public AudioOverrideZone[] targetZones;

        [Header("UI")]
        public Material buttonOnMat;
        public Material buttonOffMat;

        public MeshRenderer zoneButton;
        public MeshRenderer respawnButton;
        public MeshRenderer lockedButton;
        public MeshRenderer aoeButton;
        public MeshRenderer grabBututon;
        public MeshRenderer pttButton;

        [UdonSynced, FieldChangeCallback("PTTEnabled")]
        bool syncPTT = false;
        [UdonSynced, FieldChangeCallback("GrabEnabled")]
        bool syncGrab = true;
        [UdonSynced, FieldChangeCallback("ZoneEnabled")]
        bool syncZone = false;
        [UdonSynced, FieldChangeCallback("AOEEnabled")]
        bool syncAOE = false;

        Vector3 startLocation;
        Quaternion startRotation;

        void Start()
        {
            if (Utilities.IsValid(microphone))
            {
                startLocation = microphone.transform.position;
                startRotation = microphone.transform.rotation;

                microphone._Register((UdonBehaviour)(Component)this, "_Pickup", "_Drop", null);

                if (Networking.IsOwner(gameObject) && microphone.TriggerOnUse)
                {
                    PTTEnabled = true;
                    RequestSerialization();
                }

                if (Utilities.IsValid(microphone.accessControl))
                {
                    microphone.accessControl._RegisterValidateHandler(this, "_ValidateAccess");
                    _ValidateAccess();
                }
            }

            _SetButton(grabBututon, true);
        }

        public bool PTTEnabled
        {
            set
            {
                syncPTT = value;

                if (Utilities.IsValid(microphone))
                    microphone.TriggerOnUse = value;

                _SetButton(pttButton, syncPTT);
            }
            get { return syncPTT; }
        }

        public bool GrabEnabled
        {
            set
            {
                syncGrab = value;

                if (Utilities.IsValid(microphone) && !syncGrab)
                    microphone._Drop();
                if (Utilities.IsValid(microphoneCollider))
                    microphoneCollider.enabled = syncGrab;

                _SetButton(grabBututon, syncGrab);
            }
            get { return syncGrab; }
        }

        public bool ZoneEnabled
        {
            set
            {
                syncZone = value;

                if (Utilities.IsValid(baseZone))
                {
                    foreach (var zone in targetZones)
                    {
                        if (Utilities.IsValid(zone))
                            zone._SetLinkedZoneActive(baseZone, syncZone);
                    }
                }

                _SetButton(zoneButton, syncZone);
            }
            get { return syncZone; }
        }

        public bool AOEEnabled
        {
            set
            {
                syncAOE = value;

                if (Utilities.IsValid(aoeZone))
                {
                    bool active = syncAOE && Utilities.IsValid(microphone) && microphone.IsTriggered;
                    foreach (var zone in targetZones)
                    {
                        if (Utilities.IsValid(zone))
                            zone._SetLinkedZoneActive(aoeZone, active);
                    }
                }

                _SetButton(aoeButton, syncAOE);
            }
            get { return syncAOE; }
        }

        public void _Pickup()
        {
            if (AOEEnabled && Utilities.IsValid(aoeZone))
            {
                foreach (var zone in targetZones)
                {
                    if (Utilities.IsValid(zone))
                        zone._SetLinkedZoneActive(aoeZone, true);
                }
            }
        }

        public void _Drop()
        {
            if (AOEEnabled && Utilities.IsValid(aoeZone))
            {
                foreach (var zone in targetZones)
                {
                    if (Utilities.IsValid(zone))
                        zone._SetLinkedZoneActive(aoeZone, false);
                }
            }
        }

        public void _ValidateAccess()
        {
            _SetButton(lockedButton, !microphone.accessControl._LocalHasAccess());
        }

        public void _ToggleZone()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            ZoneEnabled = !ZoneEnabled;
            RequestSerialization();
        }

        public void _ToggleAOE()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            AOEEnabled = !AOEEnabled;
            RequestSerialization();
        }

        public void _ToggleGrab()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            GrabEnabled = !GrabEnabled;
            RequestSerialization();
        }

        public void _TogglePTT()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            PTTEnabled = !PTTEnabled;
            RequestSerialization();
        }

        public void _Respawn()
        {
            if (!_AccessCheck())
                return;

            if (Utilities.IsValid(microphone))
            {
                microphone._Drop();
                Networking.SetOwner(Networking.LocalPlayer, microphone.gameObject);
                microphone.transform.SetPositionAndRotation(startLocation, startRotation);
            }

            _SetButton(respawnButton, true);
            SendCustomEventDelayedSeconds("_ResetRespawn", 0.5f);
        }

        public void _ResetRespawn()
        {
            _SetButton(respawnButton, false);
        }

        bool _AccessCheck()
        {
            if (!Utilities.IsValid(microphone))
                return true;

            AccessControl acl = microphone.accessControl;
            if (!Utilities.IsValid(acl))
                return true;

            return acl._LocalHasAccess();
        }

        void _SetButton(MeshRenderer mesh, bool state)
        {
            _SetMaterial(mesh, state ? buttonOnMat : buttonOffMat);
        }

        void _SetMaterial(MeshRenderer mesh, Material mat)
        {
            Material[] shared = mesh.sharedMaterials;
            shared[0] = mat;
            mesh.sharedMaterials = shared;
        }
    }
}
