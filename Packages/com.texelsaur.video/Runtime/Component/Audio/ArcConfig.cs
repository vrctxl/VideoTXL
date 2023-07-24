
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ArcConfig : UdonSharpBehaviour
    {
        public Transform roomCenter;
        public float radius = 10;

        void Start()
        {

        }
    }
}
