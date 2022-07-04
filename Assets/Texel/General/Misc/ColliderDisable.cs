
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ColliderDisable : UdonSharpBehaviour
    {
        void Start()
        {
            Collider c = gameObject.GetComponent<Collider>();
            if (Utilities.IsValid(c))
                c.isTrigger = true;
        }
    }
}
