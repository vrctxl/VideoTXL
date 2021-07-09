
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/General/Zone Trigger Proxy")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ZoneTriggerProxy : UdonSharpBehaviour
    {
        [Tooltip("After sending an enter event, do not send another until leave has been triggered")]
        public bool latchUntilLeave;
        [Tooltip("After sending a leave event, do not send another until enter has been triggered")]
        public bool latchUntilEnter;

        [Header("Target")]
        [Tooltip("The Udon Behavior to send messages to on enter and leave events")]
        public UdonBehaviour targetBehavior;
        [Tooltip("The event message to send on a player trigger enter event.  Leave blank to do nothing.")]
        public string playerEnterEvent;
        [Tooltip("The event message to send on a player trigger leave event.  Leave blank to do nothing.")]
        public string playerLeaveEvent;

        bool hasPlayerEnter = false;
        bool hasPlayerLeave = false;
        bool enterLatched = false;
        bool leaveLatched = false;

        void Start()
        {
            _Validate();
        }

        public void _Bind(UdonBehaviour behavior, string enterEvent, string leaveEvent)
        {
            targetBehavior = behavior;
            playerEnterEvent = enterEvent;
            playerLeaveEvent = leaveEvent;
            _Validate();
        }

        public void _PlayerTriggerEnter()
        {
            if (hasPlayerEnter && !enterLatched)
                targetBehavior.SendCustomEvent(playerEnterEvent);

            enterLatched = true;
            leaveLatched = false;
        }

        public void _PlayerTriggerLeave()
        {
            if (hasPlayerLeave && !leaveLatched)
                targetBehavior.SendCustomEvent(playerLeaveEvent);

            leaveLatched = true;
            enterLatched = false;
        }

        void _Validate()
        {
            hasPlayerEnter = Utilities.IsValid(targetBehavior) && playerEnterEvent != null && playerEnterEvent != "";
            hasPlayerLeave = Utilities.IsValid(targetBehavior) && playerLeaveEvent != null && playerLeaveEvent != "";
        }
    }
}