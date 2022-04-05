
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PickupTrigger : UdonSharpBehaviour
    {
        [Tooltip("Trigger on use down/up instead of pickup/drop")]
        public bool triggerOnUse;

        [Tooltip("Optional ACL to check if pickup is usable for player")]
        public AccessControl accessControl;

        int handlerCount = 0;
        Component[] targetBehaviors;
        string[] pickupEvents;
        string[] dropEvents;
        string[] playerArgs;

        bool hasAccessControl = false;
        bool triggerDown = false;
        bool triggered = false;

        void Start()
        {
            hasAccessControl = Utilities.IsValid(accessControl);

            if (hasAccessControl) {
                accessControl._RegisterValidateHandler(this, "_ValidateACL");
                _ValidateACL();
            }
        }

        public override void OnPickup()
        {
            if (hasAccessControl && !accessControl._LocalHasAccess())
                return;
            if (triggerOnUse)
                return;

            _TriggerOn();
        }

        public override void OnDrop()
        {
            triggerDown = false;
            if (hasAccessControl && !accessControl._LocalHasAccess())
                return;
            if (triggerOnUse)
                return;

            _TriggerOff();
        }

        public override void OnPickupUseDown()
        {
            triggerDown = true;
            if (hasAccessControl && !accessControl._LocalHasAccess())
                return;

            if (!triggerOnUse)
                return;

            _TriggerOn();
        }

        public override void OnPickupUseUp()
        {
            triggerDown = false;
            if (hasAccessControl && !accessControl._LocalHasAccess())
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
                bool grant = accessControl._LocalHasAccess();
                pickup.pickupable = grant;
                Debug.Log(grant);

                // If player is holding object but no longer has access, drop it
                if (!grant)
                    _Drop();
            }
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
            for (int i = 0; i < handlerCount; i++)
            {
                UdonBehaviour target = (UdonBehaviour)targetBehaviors[i];
                string evt = pickupEvents[i];
                if (!Utilities.IsValid(evt))
                    continue;

                string arg = playerArgs[i];
                if (Utilities.IsValid(arg))
                    target.SetProgramVariable(arg, Networking.LocalPlayer);

                target.SendCustomEvent(evt);
            }
        }

        void _TriggerOff()
        {
            triggered = false;
            for (int i = 0; i < handlerCount; i++)
            {
                UdonBehaviour target = (UdonBehaviour)targetBehaviors[i];
                string evt = dropEvents[i];
                if (!Utilities.IsValid(evt))
                    continue;

                string arg = playerArgs[i];
                if (Utilities.IsValid(arg))
                    target.SetProgramVariable(arg, Networking.LocalPlayer);

                target.SendCustomEvent(evt);
            }
        }

        public void _Register(UdonBehaviour target, string pickupEvent, string dropEvent, string playerArg)
        {
            if (!Utilities.IsValid(target))
                return;

            targetBehaviors = (UdonBehaviour[])_AddElement(targetBehaviors, target, typeof(UdonBehaviour));
            pickupEvents = (string[])_AddElement(pickupEvents, pickupEvent, typeof(string));
            dropEvents = (string[])_AddElement(dropEvents, dropEvent, typeof(string));
            playerArgs = (string[])_AddElement(playerArgs, playerArg, typeof(string));

            handlerCount += 1;
        }

        Array _AddElement(Array arr, object elem, Type type)
        {
            Array newArr;
            int count = 0;

            if (Utilities.IsValid(arr))
            {
                count = arr.Length;
                newArr = Array.CreateInstance(type, count + 1);
                Array.Copy(arr, newArr, count);
            }
            else
                newArr = Array.CreateInstance(type, 1);

            newArr.SetValue(elem, count);
            return newArr;
        }
    }
}
