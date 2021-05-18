
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Zone/Zone Exit Controller")]
    public class ZoneExitController : UdonSharpBehaviour
    {
        public ZoneController zoneController;

        private void Start()
        {
            Collider collider = GetComponent<Collider>();
            if (Utilities.IsValid(collider))
                zoneController._RegisterExitCollider(collider);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
                zoneController.ExitJoin();
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal)
                zoneController.ExitLeave();
        }
    }
}
