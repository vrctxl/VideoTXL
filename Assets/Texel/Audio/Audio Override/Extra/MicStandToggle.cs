
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MicStandToggle : UdonSharpBehaviour
    {
        public PickupTrigger microphone;
        public Collider micStandCollider;

        [UdonSynced, FieldChangeCallback("MicRemoved")]
        bool _syncMicRemoved = false;

        Vector3 initialMicPos;
        Quaternion initialMicRot;

        void Start()
        {
            if (Utilities.IsValid(microphone))
            {
                initialMicPos = microphone.transform.position;
                initialMicRot = microphone.transform.rotation;

                microphone._RegisterPickup(this, "_OnPickup");
                microphone._RegisterDrop(this, "_OnDrop");

                if (Utilities.IsValid(microphone.accessControl))
                    microphone.accessControl._RegisterValidateHandler(this, "_OnValidateAccess");

                _UpdateTrigger();
            }
        }

        public bool MicRemoved
        {
            get { return _syncMicRemoved; }
            set
            {
                _syncMicRemoved = value;

                _UpdateTrigger();
            }
        }

        public void _OnPickup()
        {
            if (!_AccessCheck())
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            MicRemoved = true;
            RequestSerialization();
            Debug.Log("XXX");
        }

        public void _OnDrop()
        {

        }

        public void _OnValidateAccess()
        {
            _UpdateTrigger();
        }

        public void _Respawn()
        {
            if (!_AccessCheck())
                return;

            if (Utilities.IsValid(microphone))
            {
                microphone._Drop();
                Networking.SetOwner(Networking.LocalPlayer, microphone.gameObject);
                microphone.transform.SetPositionAndRotation(initialMicPos, initialMicRot);
            }

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            MicRemoved = false;
            RequestSerialization();
        }

        public override void Interact()
        {
            _Respawn();
        }

        void _UpdateTrigger()
        {
            micStandCollider.enabled = _AccessCheck() && MicRemoved;
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
    }
}
