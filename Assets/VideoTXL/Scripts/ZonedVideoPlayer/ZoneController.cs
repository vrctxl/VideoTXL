
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Zone/Zone Controller")]
    public class ZoneController : UdonSharpBehaviour
    {
        public TriggerManager triggerManager;

        private bool inZone;
        private bool valid;

        [System.NonSerialized]
        public Collider enterCollider;
        [System.NonSerialized]
        public Collider exitCollider;

        [System.NonSerialized]
        public bool inEnterZone;
        [System.NonSerialized]
        public bool inExitZone;

        private void Start()
        {
            if (triggerManager != null)
                valid = true;
            else
                Debug.Log("[VideoTXL:ZoneController] Trigger manager not set");
        }

        public void _RegisterEnterCollider(Collider collider)
        {
            enterCollider = collider;
        }

        public void _RegisterExitCollider(Collider collider)
        {
            exitCollider = collider;
        }

        public void EnterJoin()
        {
            if (!inZone && valid)
                triggerManager._ZoneEnter();
            inEnterZone = true;
            inZone = true;
        }

        public void EnterLeave()
        {
            inEnterZone = false;
        }

        public void ExitJoin()
        {
            inExitZone = true;
        }

        public void ExitLeave()
        {
            if (inZone && valid)
                triggerManager._ZoneExit();
            inExitZone = false;
            inZone = false;
        }
    }
}
