
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BasicChair : UdonSharpBehaviour
    {
        public Transform location;

        Vector3 offset;

        void Start()
        {
            if (Utilities.IsValid(location))
            {
                Quaternion offsetRot = location.rotation * Quaternion.Inverse(transform.rotation);
                offset = offsetRot * transform.localPosition;

                Quaternion locationRot = location.rotation * transform.localRotation;
                transform.SetPositionAndRotation(location.position + offset, locationRot);
            }
        }

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }
    }
}
