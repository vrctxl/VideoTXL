using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

namespace Texel
{
    [AddComponentMenu("Texel/State/Material Swapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MaterialSwapper : UdonSharpBehaviour
    {
        [Header("Default State")]
        public bool defaultVR = true;
        public bool defaultDesktop = true;
        public bool defaultQuest = true;

        public MeshRenderer[] meshList;
        public Material[] materialList;
        public int[] indexList;

        Material[] originalMaterialList;

        bool applied = false;

        Component[] handlers;
        int handlerCount = 0;
        string[] handlerEvents;

        private void Start()
        {
            originalMaterialList = new Material[meshList.Length];
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                if (!Utilities.IsValid(materialList[i]))
                {
                    meshList[i] = null;
                    continue;
                }

                Material[] materials = meshList[i].sharedMaterials;
                if (indexList[i] < 0 || indexList[i] >= materials.Length)
                {
                    meshList[i] = null;
                    continue;
                }

                originalMaterialList[i] = materials[indexList[i]];
            }

#if UNITY_ANDROID
            if (defaultQuest)
                SendCustomEventDelayedFrames("_Apply", 1);
#else
            if (Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR())
            {
                if (defaultVR)
                    SendCustomEventDelayedFrames("_Apply", 1);
            }
            else if (defaultDesktop)
                SendCustomEventDelayedFrames("_Apply", 1);
#endif
        }

        public bool IsApplied
        {
            get { return applied; }
        }

        public void _Reset()
        {
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                Material[] materials = meshList[i].sharedMaterials;
                materials[indexList[i]] = originalMaterialList[i];
                meshList[i].sharedMaterials = materials;
            }

            applied = false;
            _UpdateHandlers();
        }

        public void _Apply()
        {
            for (int i = 0; i < meshList.Length; i++)
            {
                if (!Utilities.IsValid(meshList[i]))
                    continue;

                Material[] materials = meshList[i].sharedMaterials;
                materials[indexList[i]] = materialList[i];
                meshList[i].sharedMaterials = materials;
            }

            applied = true;
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
