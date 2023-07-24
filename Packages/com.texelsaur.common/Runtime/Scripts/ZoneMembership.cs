
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ZoneMembership : UdonSharpBehaviour
    {
        [Tooltip("Optional zone that the membership list will hook into for player leave events")]
        public ZoneTrigger zone;
        [Tooltip("Optional zone that the membership list will hook into for player enter events")]
        public ZoneTrigger triggerZone;
        [Tooltip("Update membership in response to world join and leave events")]
        public bool worldEvents = false;

        [NonSerialized]
        public VRCPlayerApi playerEventArg;

        int[] players = new int[100];
        int maxIndex = -1;
        bool init = false;

        //int handlerCount = 0;
        //Component[] targetBehaviors;
        //string[] eventNames;
        //string[] arrayReturns;

        int[] handlerCount;
        Component[][] handlers;
        string[][] handlerEvents;
        string[][] handlerArg1;
        string[][] handlerArg2;

        const int eventCount = 4;
        const int PLAYER_ADD_EVENT = 0;
        const int PLAYER_REMOVE_EVENT = 1;
        const int MEMBERSHIP_CHANGE_EVENT = 2;
        const int ARRAY_UPDATE_EVENT = 3;

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
            handlerArg1 = new string[eventCount][];
            handlerArg2 = new string[eventCount][];

            for (int i = 0; i < eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
                handlerArg1[i] = new string[0];
                handlerArg2[i] = new string[0];
            }

            if (Utilities.IsValid(zone))
            {
                if (!Utilities.IsValid(triggerZone) || zone == triggerZone) {
                    zone._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_PlayerTriggerEnter", "playerEventArg");
                    zone._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, "_PlayerTriggerExit", "playerEventArg");
                }
                else
                {
                    zone._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, "_PlayerTriggerExit", "playerEventArg");
                    triggerZone._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, "_PlayerTriggerEnter", "playerEventArg");
                }
            }
        }

        /* public void _RegisterArrayUpdateHandler(UdonBehaviour target, string eventName, string arrayReturn)
        {
            if (!Utilities.IsValid(target) || eventName == "" || arrayReturn == "")
                return;

            targetBehaviors = (UdonBehaviour[])_AddElement(targetBehaviors, target, typeof(UdonBehaviour));
            eventNames = (string[])_AddElement(eventNames, eventName, typeof(string));
            arrayReturns = (string[])_AddElement(arrayReturns, arrayReturn, typeof(string));

            handlerCount += 1;
        } */

        /* public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (worldEvents)
                _AddPlayer(player);
        } */

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (worldEvents)
                _RemovePlayer(player);
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

            _UpdateHandlersPlayer(PLAYER_ADD_EVENT, player);
            _UpdateHandlers(MEMBERSHIP_CHANGE_EVENT);

            return maxIndex;
        }

        public bool _RemovePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return false;

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                {
                    players[i] = players[maxIndex];
                    maxIndex -= 1;

                    _UpdateHandlersPlayer(PLAYER_REMOVE_EVENT, player);
                    _UpdateHandlers(MEMBERSHIP_CHANGE_EVENT);

                    if (i != maxIndex)
                        _UpdateHandlersArrayChange(ARRAY_UPDATE_EVENT, maxIndex, i);

                    /* for (int j = 0; j < handlerCount; j++)
                    {
                        UdonBehaviour target = (UdonBehaviour)targetBehaviors[j];
                        target.SendCustomEvent(eventNames[j]);
                        Array arr = (Array)target.GetProgramVariable(arrayReturns[j]);

                        if (Utilities.IsValid(arr) && arr.Length > maxIndex)
                            arr.SetValue(arr.GetValue(maxIndex), i);
                    } */

                    return true;
                }
            }

            return false;
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

        public void _RegisterAddPlayer(Component handler, string eventName, string playerArg)
        {
            _Register(PLAYER_ADD_EVENT, handler, eventName, new string[] { playerArg });
        }

        public void _RegisterRemovePlayer(Component handler, string eventName, string playerArg)
        {
            _Register(PLAYER_REMOVE_EVENT, handler, eventName, new string[] { playerArg });
        }

        public void _RegisterMembershipChange(Component handler, string eventName)
        {
            _Register(MEMBERSHIP_CHANGE_EVENT, handler, eventName, null);
        }

        public void _RegisterArrayUpdate(Component handler, string eventName, string oldIndexArg, string newIndexArg)
        {
            _Register(ARRAY_UPDATE_EVENT, handler, eventName, new string[] { oldIndexArg, newIndexArg });
        }

        void _Register(int eventIndex, Component handler, string eventName, string[] args)
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

            if (Utilities.IsValid(args) && args.Length >= 1)
                handlerArg1[eventIndex] = (string[])_AddElement(handlerArg1[eventIndex], args[0], typeof(string));
            if (Utilities.IsValid(args) && args.Length >= 2)
                handlerArg1[eventIndex] = (string[])_AddElement(handlerArg1[eventIndex], args[1], typeof(string));

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

        void _UpdateHandlersPlayer(int eventIndex, VRCPlayerApi player)
        {
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                if (Utilities.IsValid(handlerArg1[eventIndex]))
                    script.SetProgramVariable(handlerArg1[eventIndex][i], player);
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
        }

        void _UpdateHandlersArrayChange(int eventIndex, int oldIndex, int newIndex)
        {
            for (int j = 0; j < handlerCount[eventIndex]; j++)
            {
                UdonBehaviour target = (UdonBehaviour)handlers[eventIndex][j];
                if (Utilities.IsValid(handlerArg1[eventIndex]))
                    target.SetProgramVariable(handlerArg1[eventIndex][j], oldIndex);
                if (Utilities.IsValid(handlerArg2[eventIndex]))
                    target.SetProgramVariable(handlerArg1[eventIndex][j], newIndex);
                target.SendCustomEvent(handlerEvents[eventIndex][j]);
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
