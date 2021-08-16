
using BrokeredUpdates;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Album/Album")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Album : UdonSharpBehaviour
    {
        public int albumId;
        public VRCUrl url;
        public AlbumLoader loader;
        public BrokeredSync sync;
        public DebugLog debugLog;
        public Vector2 atlasIndex;

        Vector3 originalPosition;
        Quaternion originalRotation;

        VRC_Pickup pickup;

        void Start()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;

            pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetVector("_TextureST", new Vector4(0, 0, atlasIndex.x, atlasIndex.y));

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.SetPropertyBlock(props);

            if (Utilities.IsValid(loader))
                loader._Register(this);
        }

        public override void OnPickup()
        {
            DebugLog("Pickup");

            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            loader._PickupAlbum(this);
        }

        public override void OnDrop()
        {
            DebugLog("Drop");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Networking.IsOwner(gameObject))
                return;

            DebugLog($"Collision: me: {gameObject.GetInstanceID()}, them: {other.gameObject.GetInstanceID()}");
            if (other.gameObject == loader.gameObject)
                loader._LoadAlbum(this);
        }

        public void _Reset()
        {
            DebugLog($"Reset (held={pickup.IsHeld}, origPos={originalPosition}, origRot={originalRotation})");
            if (pickup.IsHeld)
                pickup.Drop();

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            transform.SetPositionAndRotation(originalPosition, originalRotation);
            sync._SendMasterMove();
        }

        public void _Display(Transform tf)
        {
            DebugLog("Display");

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (pickup.IsHeld)
                pickup.Drop();

            transform.SetPositionAndRotation(tf.position, tf.rotation);
            sync._SendMasterMove();
        }

        void DebugLog(string message)
        {
            Debug.Log("[Album] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("Album", message);
        }
    }
}
