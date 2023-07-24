
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/General/Interact Trigger")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class InteractTrigger : UdonSharpBehaviour
    {
        [Tooltip("The Udon Behavior to send messages to on enter and leave events")]
        public UdonBehaviour targetBehavior;
        [Tooltip("The event message to send on a player interact event.")]
        public string interactEvent;

        bool hasInteract = false;

        void Start()
        {
            hasInteract = Utilities.IsValid(targetBehavior) && interactEvent != null && interactEvent != "";
        }

        public override void Interact()
        {
            if (!hasInteract)
                return;

            targetBehavior.SendCustomEvent(interactEvent);
        }
    }
}