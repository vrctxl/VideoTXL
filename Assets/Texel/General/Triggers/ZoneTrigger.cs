
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/General/Zone Trigger")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ZoneTrigger : UdonSharpBehaviour
    {
        [Tooltip("The Udon Behavior to send messages to on enter and leave events")]
        public UdonBehaviour targetBehavior;
        [Tooltip("Whether colliders should only recognize the local player")]
        public bool localPlayerOnly = true;
        [Tooltip("The event message to send on a player trigger enter event.  Leave blank to do nothing.")]
        public string playerEnterEvent;
        [Tooltip("The event message to send on a player trigger leave event.  Leave blank to do nothing.")]
        public string playerLeaveEvent;
        [Tooltip("Variable in remote script to write player reference before calling an enter or leave event.  Leave blank to not set player reference.")]
        public string playerTargetVariable;

        bool hasPlayerEnter = false;
        bool hasPlayerLeave = false;
        bool hasTargetVariable = false;

        void Start()
        {
            hasPlayerEnter = Utilities.IsValid(targetBehavior) && playerEnterEvent != null && playerEnterEvent != "";
            hasPlayerLeave = Utilities.IsValid(targetBehavior) && playerLeaveEvent != null && playerLeaveEvent != "";
            hasTargetVariable = Utilities.IsValid(targetBehavior) && playerTargetVariable != null && playerTargetVariable != "";
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!hasPlayerEnter)
                return;
            if (localPlayerOnly && !player.isLocal)
                return;

            if (hasTargetVariable)
                targetBehavior.SetProgramVariable(playerTargetVariable, player);

            targetBehavior.SendCustomEvent(playerEnterEvent);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!hasPlayerLeave)
                return;
            if (localPlayerOnly && !player.isLocal)
                return;

            if (hasTargetVariable)
                targetBehavior.SetProgramVariable(playerTargetVariable, player);

            targetBehavior.SendCustomEvent(playerLeaveEvent);
        }
    }
}