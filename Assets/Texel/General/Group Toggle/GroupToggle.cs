
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/State/Group Toggle")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GroupToggle : UdonSharpBehaviour
    {
        public bool defaultState = false;

        public GameObject[] onStateObjects;
        public GameObject[] offStateObjects;

        bool state = false;

        void Start()
        {
            _ToggleInternal(defaultState);
        }

        public void _Toggle()
        {
            _ToggleInternal(!state);
        }

        public void _ToggleOn()
        {
            _ToggleInternal(true);
        }

        public void _ToggleOff()
        {
            _ToggleInternal(false);
        }

        void _ToggleInternal(bool val)
        {
            state = val;

            if (Utilities.IsValid(onStateObjects))
            {
                foreach (var obj in onStateObjects)
                {
                    if (Utilities.IsValid(obj))
                        obj.SetActive(state);
                }
            }

            if (Utilities.IsValid(offStateObjects))
            {
                foreach (var obj in offStateObjects)
                {
                    if (Utilities.IsValid(obj))
                        obj.SetActive(!state);
                }
            }
        }
    }
}
