
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DynamicWhitelistRequest : EventBase
    {
        [Tooltip("Disallows any further requests until the current request is granted or denied")]
        public bool latchRequest = true;

        [UdonSynced, FieldChangeCallback("CurrentRequest")]
        int syncCurrentRequest = -1;

        public const int EVENT_ACCESS_REQUEST = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount { get => EVENT_COUNT; }

        public int CurrentRequest
        {
            set
            {
                syncCurrentRequest = value;
                _UpdateHandlers(EVENT_ACCESS_REQUEST);
            }
            get { return syncCurrentRequest; }
        }

        public void _Request()
        {
            if (latchRequest && syncCurrentRequest > -1)
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            CurrentRequest = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }

        public void _Clear()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            CurrentRequest = -1;
            RequestSerialization();
        }
    }
}
