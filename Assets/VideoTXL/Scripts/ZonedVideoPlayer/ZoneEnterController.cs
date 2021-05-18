
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Zone/Zone Enter Controller")]
    public class ZoneEnterController : UdonSharpBehaviour
    {
        public ZoneController zoneController;

        private void Start()
        {
            Collider collider = GetComponent<Collider>();
            if (Utilities.IsValid(collider))
                zoneController._RegisterEnterCollider(collider);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
                zoneController.EnterJoin();
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal)
                zoneController.EnterLeave();
        }
    }
}
