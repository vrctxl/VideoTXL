
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DynamicWhitelistGrant : EventBase
    {
        [Tooltip("ACL used to check who can approve/deny entries into the dynamic whitelist")]
        public AccessControl grantACL;
        [Tooltip("The dynamic list storing synced whitelist users")]
        public AccessControlDynamicUserList dynamicList;
        [Tooltip("Requester object that allows any player to request membership in the whitelist")]
        public DynamicWhitelistRequest request;
        [Tooltip("Allows users permitted by the Grant ACL to request being added to the dynamic whitelist")]
        public bool grantUsersCanRequest = false;

        [UdonSynced, FieldChangeCallback("CurrentRequest")]
        int syncCurrentRequest = -1;

        public const int EVENT_REQUEST_CHANGE = 0;
        public const int EVENT_COUNT = 1;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            if (Utilities.IsValid(request))
                request._Register(DynamicWhitelistRequest.EVENT_ACCESS_REQUEST, this, "_OnAccessRequest");
        }

        public int CurrentRequest
        {
            set
            {
                syncCurrentRequest = value;

                _UpdateHandlers(EVENT_REQUEST_CHANGE);
            }
            get { return syncCurrentRequest; }
        }

        public void _OnAccessRequest()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            CurrentRequest = request.CurrentRequest;
            RequestSerialization();
        }

        public void _Grant()
        {
            if (!Utilities.IsValid(request) || !_CheckAccess())
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (!Networking.IsOwner(dynamicList.syncedPlayerList.gameObject))
                Networking.SetOwner(Networking.LocalPlayer, dynamicList.syncedPlayerList.gameObject);

            int id = request.CurrentRequest;
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(id);
            if (Utilities.IsValid(player) && player.IsValid())
            {
                if (dynamicList._AddPlayer(player))
                    request._Clear();
            } else
                request._Clear();
        }

        public void _Deny()
        {
            if (!Utilities.IsValid(request) || !_CheckAccess())
                return;

            request._Clear();
        }

        bool _CheckAccess()
        {
            if (!Utilities.IsValid(grantACL))
                return true;
            return grantACL._LocalHasAccess();
        }
    }
}
