
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/General/Collider Render")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ColliderRender : UdonSharpBehaviour
    {
        public Collider collider;

        public MeshRenderer boxRender;
        public MeshRenderer sphereRender;
        public MeshRenderer capsuleRender;

        void Start()
        {
            boxRender.enabled = false;
            sphereRender.enabled = false;
            capsuleRender.enabled = false;

            BoxCollider box = (BoxCollider)collider;
            if (Utilities.IsValid(box))
            {
                boxRender.enabled = true;
                boxRender.transform.position = box.bounds.center;
                boxRender.transform.localScale = box.bounds.size;
            }

            SphereCollider sphere = (SphereCollider)collider;
            if (Utilities.IsValid(sphere))
            {
                sphereRender.enabled = true;
                sphereRender.transform.position = sphere.bounds.center;
                sphereRender.transform.localScale = sphere.bounds.size;
            }

            CapsuleCollider capsule = (CapsuleCollider)collider;
            if (Utilities.IsValid(capsule))
            {
                capsuleRender.enabled = true;
                capsuleRender.transform.position = sphere.bounds.center;
                capsuleRender.transform.localScale = sphere.bounds.size;
            }
        }
    }
}
