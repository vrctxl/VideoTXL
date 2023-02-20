
using System;
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
        [Header("Access Control")]
        public AccessControl accessControl;

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

        Collider[] onColliders;
        Collider[] offColliders;

        MeshRenderer[] onRenderers;
        MeshRenderer[] offRenderers;

        Component[] handlers;
        int handlerCount = 0;
        string[] handlerEvents;

        void Start()
        {
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

            bool defaultState = defaultDesktop;
            if (Networking.LocalPlayer.IsUserInVR())
                defaultState = defaultVR;

#if UNITY_ANDROID
            defaultState = defaultQuest;
#endif

            state = defaultState;
            if (accessControl && !accessControl._LocalHasAccess())
                state = false;

            _ToggleInternal(state);
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
            State = !State;
        }

        public void _ToggleOn()
        {
            State = true;
        }

        public void _ToggleOff()
        {
            State = false;
        }

        void _ToggleInternal(bool val)
        {
            state = val;

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

            _UpdateHandlers();
        }

        public void _Regsiter(Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            for (int i = 0; i < handlerCount; i++)
            {
                if (handlers[i] == handler)
                    return;
            }

            handlers = (Component[])_AddElement(handlers, handler, typeof(Component));
            handlerEvents = (string[])_AddElement(handlerEvents, eventName, typeof(string));

            handlerCount += 1;
        }

        void _UpdateHandlers()
        {
            for (int i = 0; i < handlerCount; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[i];
                script.SendCustomEvent(handlerEvents[i]);
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
