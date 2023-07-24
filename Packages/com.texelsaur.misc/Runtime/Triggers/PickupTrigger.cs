
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PickupTrigger : EventBase
    {
        [Tooltip("Trigger on use down/up instead of pickup/drop")]
        public bool triggerOnUse;

        [Tooltip("Optional ACL to check if pickup is usable for player")]
        public AccessControl accessControl;
        public bool enforceACL = true;

        public const int EVENT_PICKUP = 0;
        public const int EVENT_DROP = 1;
        public const int EVENT_TRIGGER_ON = 2;
        public const int EVENT_TRIGGER_OFF = 3;
        const int EVENT_COUNT = 4;

        bool hasAccessControl = false;
        bool triggerDown = false;
        bool triggered = false;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            hasAccessControl = Utilities.IsValid(accessControl);

            if (hasAccessControl)
            {
                accessControl._Register(AccessControl.EVENT_VALIDATE, this, "_ValidateACL");
                _ValidateACL();
            }
        }

        public override void OnPickup()
        {
            if (!_HasAccess())
                return;

            _UpdateHandlers(EVENT_PICKUP);

            if (triggerOnUse)
                return;

            _TriggerOn();
        }

        public override void OnDrop()
        {
            triggerDown = false;
            if (!_HasAccess())
                return;

            _UpdateHandlers(EVENT_DROP);

            if (triggerOnUse)
                return;

            _TriggerOff();
        }

        public override void OnPickupUseDown()
        {
            triggerDown = true;
            if (!_HasAccess())
                return;

            if (!triggerOnUse)
                return;

            _TriggerOn();
        }

        public override void OnPickupUseUp()
        {
            triggerDown = false;
            if (!_HasAccess())
                return;
            if (!triggerOnUse)
                return;

            _TriggerOff();
        }

        public bool TriggerOnUse
        {
            get { return triggerOnUse; }
            set
            {
                triggerOnUse = value;
                _ValidateState();
            }
        }

        public bool IsTriggered
        {
            get { return triggered; }
        }

        public void _Drop()
        {
            VRC_Pickup pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
            if (!Utilities.IsValid(pickup))
                return;

            VRCPlayerApi holdingPlayer = pickup.currentPlayer;
            if (Utilities.IsValid(holdingPlayer) && holdingPlayer.playerId == Networking.LocalPlayer.playerId)
            {
                pickup.Drop();
                if (triggered)
                    _TriggerOff();
            }
        }

        public void _ValidateACL()
        {
            if (!hasAccessControl)
                return;

            VRC_Pickup pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
            if (Utilities.IsValid(pickup))
            {
                bool grant = _HasAccess();
                pickup.pickupable = grant;
                Debug.Log(grant);

                // If player is holding object but no longer has access, drop it
                if (!grant)
                    _Drop();
            }
        }

        bool _HasAccess()
        {
            if (!enforceACL)
                return true;
            if (!hasAccessControl)
                return true;
            return accessControl._LocalHasAccess();
        }

        public void _ValidateState()
        {
            VRC_Pickup pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
            if (!Utilities.IsValid(pickup))
                return;

            VRCPlayerApi holdingPlayer = pickup.currentPlayer;
            if (!Utilities.IsValid(holdingPlayer) || holdingPlayer.playerId != Networking.LocalPlayer.playerId)
                return;

            if (triggerOnUse && !triggerDown && triggered)
                _TriggerOff();
            else if (!triggerOnUse && !triggered)
                _TriggerOn();
        }

        void _TriggerOn()
        {
            triggered = true;
            _UpdateHandlers(EVENT_TRIGGER_ON);
        }

        void _TriggerOff()
        {
            triggered = false;
            _UpdateHandlers(EVENT_TRIGGER_OFF);
        }
    }
}
