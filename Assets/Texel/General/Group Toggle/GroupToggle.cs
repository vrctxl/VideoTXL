
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GroupToggle : EventBase
    {
        [Header("Access Control")]
        [Tooltip("Optional.  Enables default-on states for local user only if they have access via the referenced ACL at world load.")]
        public AccessControl accessControl;
        [Tooltip("Optional.  Enables default-on states when an ACL updates to give local user access that they did not have at world load.")]
        public bool initOnAccessUpadte = true;
        [Tooltip("Optional.  Disables the ability to toggle group to 'on' state if local user does not have access via the referenced ACL.")]
        public bool enforceOnToggle = true;

        [Header("Default State")]
        public bool defaultVR = true;
        public bool defaultDesktop = true;
        public bool defaultQuest = true;

        [Header("Objects")]
        public GameObject[] onStateObjects;
        public GameObject[] offStateObjects;

        [Header("Toggle Attributes")]
        public bool toggleGameObject = true;
        public bool toggleColliders = false;
        public bool toggleRenderers = false;

        bool state = false;
        bool inDefault = false;

        Collider[] onColliders;
        Collider[] offColliders;

        MeshRenderer[] onRenderers;
        MeshRenderer[] offRenderers;

        public const int EVENT_TOGGLED = 0;
        const int EVENT_COUNT = 1;

        private void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            base._Init();

            if (toggleColliders)
            {
                onColliders = _BuildColliderList(onStateObjects);
                offColliders = _BuildColliderList(offStateObjects);
            }

            if (toggleRenderers)
            {
                onRenderers = _BuildRendererList(onStateObjects);
                offRenderers = _BuildRendererList(offStateObjects);
            }

            state = _DefaultState();

            if (accessControl)
            {
                if (initOnAccessUpadte)
                    accessControl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_OnValidate));

                if (!accessControl._LocalHasAccess())
                    state = false;
            }

            _ToggleInternal(state);

            inDefault = true;
        }

        public void _OnValidate()
        {
            if (inDefault && accessControl._LocalHasAccess())
            {
                _ToggleInternal(_DefaultState());
                inDefault = true;
            }
        }

        bool _DefaultState()
        {
            bool defaultState = defaultDesktop;
            if (Networking.LocalPlayer.IsUserInVR())
                defaultState = defaultVR;

#if UNITY_ANDROID
            defaultState = defaultQuest;
#endif

            return defaultState;
        }

        Collider[] _BuildColliderList(GameObject[] objects)
        {
            int count = 0;
            Collider[][] clist = new Collider[objects.Length][];
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (Utilities.IsValid(obj))
                    clist[i] = obj.GetComponentsInChildren<Collider>();
                else
                    clist[i] = new Collider[0];

                count += clist[i].Length;
            }

            int index = 0;
            Collider[] colliders = new Collider[count];
            for (int i = 0; i < clist.Length; i++)
            {
                Collider[] sublist = clist[i];
                Array.Copy(sublist, 0, colliders, index, sublist.Length);
                index += sublist.Length;
            }

            return colliders;
        }

        MeshRenderer[] _BuildRendererList(GameObject[] objects)
        {
            int count = 0;
            MeshRenderer[] rlist = new MeshRenderer[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (Utilities.IsValid(obj))
                {
                    rlist[i] = obj.GetComponent<MeshRenderer>();
                    count += 1;
                }
            }

            int index = 0;
            MeshRenderer[] renderers = new MeshRenderer[count];
            for (int i = 0; i < rlist.Length; i++)
            {
                if (rlist[i])
                {
                    renderers[index] = rlist[i];
                    index += 1;
                }
            }

            return renderers;
        }

        public bool State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    _ToggleInternal(value);
                }
            }
        }

        public void _Toggle()
        {
            if (enforceOnToggle && accessControl && !accessControl._LocalHasAccess())
                return;

            State = !State;
        }

        public void _ToggleOn()
        {
            if (enforceOnToggle && accessControl && !accessControl._LocalHasAccess())
                return;

            State = true;
        }

        public void _ToggleOff()
        {
            if (enforceOnToggle && accessControl && !accessControl._LocalHasAccess())
                return;

            State = false;
        }

        void _ToggleInternal(bool val)
        {
            state = val;
            inDefault = false;

            if (toggleColliders)
            {
                foreach (var collider in onColliders)
                    collider.enabled = state;
                foreach (var collider in offColliders)
                    collider.enabled = !state;
            }

            if (toggleRenderers)
            {
                foreach (var renderer in onRenderers)
                    renderer.enabled = state;
                foreach (var renderer in offRenderers)
                    renderer.enabled = !state;
            }

            if (toggleGameObject)
            {
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

            _UpdateHandlers(EVENT_TOGGLED);
        }
    }
}
