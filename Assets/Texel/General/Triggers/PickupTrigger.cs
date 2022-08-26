
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

        const int eventCount = 4;
        const int PICKUP_EVENT = 0;
        const int DROP_EVENT = 1;
        const int TRIGGER_ON_EVENT = 2;
        const int TRIGGER_OFF_EVENT = 3;

        int[] handlerCount;
        Component[][] handlers;
        string[][] handlerEvents;

        bool hasAccessControl = false;
        bool triggerDown = false;
        bool triggered = false;
        bool init = false;

        void Start()
        {
            _EnsureInit();
        }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;

            _Init();
        }

        void _Init()
        {
            handlerCount = new int[eventCount];
            handlers = new Component[eventCount][];
            handlerEvents = new string[eventCount][];

            for (int i = 0; i < eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
            }

            hasAccessControl = Utilities.IsValid(accessControl);

            if (hasAccessControl)
            {
                accessControl._RegisterValidateHandler(this, "_ValidateACL");
                _ValidateACL();
            }
        }

        public override void OnPickup()
        {
            if (hasAccessControl && !accessControl._LocalHasAccess())
                return;

            _UpdateHandlers(PICKUP_EVENT);
 
            if (triggerOnUse)
                return;

            _TriggerOn();
        }

        public override void OnDrop()
        {
            triggerDown = false;
            if (hasAccessControl && !accessControl._LocalHasAccess())
                return;

            _UpdateHandlers(DROP_EVENT);

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
            _UpdateHandlers(TRIGGER_ON_EVENT);
        }

        void _TriggerOff()
        {
            triggered = false;
            _UpdateHandlers(TRIGGER_OFF_EVENT);
        }

        public void _RegisterPickup(Component handler, string eventName)
        {
            _Register(PICKUP_EVENT, handler, eventName);
        }

        public void _RegisterDrop(Component handler, string eventName)
        {
            _Register(DROP_EVENT, handler, eventName);
        }

        public void _RegisterTriggerOn(Component handler, string eventName)
        {
            _Register(TRIGGER_ON_EVENT, handler, eventName);
        }

        public void _RegisterTriggerOff(Component handler, string eventName)
        {
            _Register(TRIGGER_OFF_EVENT, handler, eventName);
        }

        void _Register(int eventIndex, Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            _EnsureInit();

            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                if (handlers[eventIndex][i] == handler)
                    return;
            }

            handlers[eventIndex] = (Component[])_AddElement(handlers[eventIndex], handler, typeof(Component));
            handlerEvents[eventIndex] = (string[])_AddElement(handlerEvents[eventIndex], eventName, typeof(string));

            handlerCount[eventIndex] += 1;
        }

        void _UpdateHandlers(int eventIndex)
        {
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
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
