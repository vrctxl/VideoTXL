
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public class EventBase : UdonSharpBehaviour
    {
        protected int[] handlerCount;
        protected Component[][] handlers;
        protected string[][] handlerEvents;
        protected string[][] handlerArg1;
        protected string[][] handlerArg2;

        protected int eventCount = 0;

        bool init = false;

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;
            _Init();
        }

        protected virtual void _Init() { }

        protected void _InitHandlers(int count)
        {
            eventCount = count;

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
        }

        public void _Register(int eventIndex, Component handler, string eventName, params string[] args)
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

        protected void _UpdateHandlers(int eventIndex)
        {
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
        }

        protected Array _AddElement(Array arr, object elem, Type type)
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
