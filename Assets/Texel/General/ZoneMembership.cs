
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/General/Zone Membership")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ZoneMembership : UdonSharpBehaviour
    {
        [NonSerialized]
        public VRCPlayerApi playerEventArg;

        int[] players = new int[100];
        int maxIndex = -1;

        int handlerCount = 0;
        Component[] targetBehaviors;
        string[] eventNames;
        string[] arrayReturns;

        void Start()
        {

        }

        public void _RegisterArrayUpdateHandler(UdonBehaviour target, string eventName, string arrayReturn)
        {
            if (!Utilities.IsValid(target) || eventName == "" || arrayReturn == "")
                return;

            targetBehaviors = (UdonBehaviour[])_AddElement(targetBehaviors, target, typeof(UdonBehaviour));
            eventNames = (string[])_AddElement(eventNames, eventName, typeof(string));
            arrayReturns = (string[])_AddElement(arrayReturns, arrayReturn, typeof(string));

            handlerCount += 1;
        }

        public void _PlayerTriggerEnter()
        {
            _AddPlayer(playerEventArg);
        }

        public void _PlayerTriggerExit()
        {
            _RemovePlayer(playerEventArg);
        }

        public int _AddPlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return -1;

            if (!player.IsValid())
            {
                _RemovePlayer(player);
                return -1;
            }

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                    return i;
            }

            maxIndex += 1;
            players[maxIndex] = id;

            return maxIndex;
        }

        public void _RemovePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                {
                    players[i] = players[maxIndex];
                    maxIndex -= 1;

                    for (int j = 0; j < handlerCount; j++)
                    {
                        UdonBehaviour target = (UdonBehaviour)targetBehaviors[j];
                        target.SendCustomEvent(eventNames[j]);
                        Array arr = (Array)target.GetProgramVariable(arrayReturns[j]);

                        if (Utilities.IsValid(arr) && arr.Length > maxIndex)
                            arr.SetValue(arr.GetValue(maxIndex), i);
                    }

                    return;
                }
            }
        }

        public int _PlayerCount()
        {
            return maxIndex + 1;
        }

        public VRCPlayerApi _GetPlayer(int index)
        {
            if (index < 0 || index > maxIndex)
                return null;

            int id = players[index];
            if (id == -1)
                return null;

            return VRCPlayerApi.GetPlayerById(id);
        }

        public int _GetPlayerIndex(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return -1;

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                    return i;
            }

            return -1;
        }

        public bool _ContainsPlayer(VRCPlayerApi player)
        {
            return _GetPlayerIndex(player) > -1;
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
