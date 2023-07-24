
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AccessControlDynamicUserList : AccessControlUserList
    {
        public SyncPlayerList syncedPlayerList;

        protected override void _Init()
        {
            base._Init();

            if (syncedPlayerList)
            {
                syncedPlayerList._Register(SyncPlayerList.EVENT_MEMBERSHIP_CHANGE, this, nameof(_OnMembershipChange));

                if (Networking.IsOwner(syncedPlayerList.gameObject))
                    syncedPlayerList._SetInitialList(userList);
            }
        }

        public override bool _ContainsName(string name)
        {
            Debug.Log($"ContainsName {name}");
            if (!syncedPlayerList)
                return false;

            return syncedPlayerList._ContainsPlayer(name);
        }

        public void _OnMembershipChange()
        {
            _UpdateHandlers(EVENT_REVALIDATE);
        }

        public bool _AddPlayer(VRCPlayerApi player)
        {
            if (syncedPlayerList)
                return syncedPlayerList._AddPlayer(player) > -1;

            return false;
        }

        public bool _RemovePlayer(VRCPlayerApi player)
        {
            if (syncedPlayerList)
                return syncedPlayerList._RemovePlayer(player);

            return false;
        }
    }
}
