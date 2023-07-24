
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using System;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CompoundZoneTrigger : ZoneTrigger
    {
        [Tooltip("How multiple colliders should be treated for triggering an enter event")]
        public int enterSetMode = SET_NONE;
        [Tooltip("How multiple colliders should be treated for triggering a leave event")]
        public int leaveSetMode = SET_NONE;
        [Tooltip("After sending an enter event, do not send another until leave has been triggered")]
        public bool latchUntilLeave;
        [Tooltip("After sending a leave event, do not send another until enter has been triggered")]
        public bool latchUntilEnter;
        [Tooltip("Force re-checking zone membership on player enter events.  May be needed in certain instances where you map can lose enter or leave events (such as stations within the zones).  You can save some performance by calling _RecalculateNextEvent yourself as needed.")]
        public bool forceColliderCheck = false;
        [Tooltip("Recalculate collider membership on start of frame")]
        public bool recalcCollidersOnStart = false;
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        [Tooltip("Write debug statements to VRChat log")]
        public bool vrcLog;

        [NonSerialized]
        public int colliderCount = 0;

        int triggerActiveCount = 0;
        bool enterLatched;
        bool leaveLatched;
        bool forceRecalc = false;
        bool pendingRecalc = false;

        Collider[] colliders;
        Collider[] validColliders;

        public const int SET_NONE = 0;
        public const int SET_UNION = 1;
        public const int SET_INTERSECT = 2;

        protected override void _Init()
        {
            base._Init();

            if (colliderCount == 0)
                _InitColliders();

            if (recalcCollidersOnStart)
                _Recalculate();
        }

        void _InitColliders()
        {
            colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (Utilities.IsValid(col) && col.enabled)
                    colliderCount += 1;
            }

            validColliders = new Collider[colliderCount];
            int i = 0;
            foreach (var col in colliders)
            {
                if (Utilities.IsValid(col) && col.enabled)
                {
                    validColliders[i] = col;
                    i++;
                }
            }
        }

        public void _EnableZone()
        {
            foreach (var col in validColliders)
                col.enabled = true;
        }

        public void _DisableZone()
        {
            foreach (var col in validColliders)
                col.enabled = false;

            int count = triggerActiveCount;
            for (int i = 0; i < count; i++)
                _PlayerTriggerExit(Networking.LocalPlayer);

            _PlayerTriggerReset();
        }

        public void _PlayerTriggerReset()
        {
            triggerActiveCount = 0;
            enterLatched = false;
            leaveLatched = false;
        }

        public override void _PlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!localPlayerOnly)
            {
                _SendPlayerEnter(player);
                return;
            }
            if (!player.isLocal)
                return;

            if ((forceColliderCheck || pendingRecalc) && triggerActiveCount >= 1 && !forceRecalc)
            {
                _Recalculate();
                return;
            }

            if (enterSetMode == SET_NONE)
                _SendPlayerEnter(player);

            if (enterSetMode == SET_UNION && triggerActiveCount == 0)
                _SendPlayerEnter(player);

            triggerActiveCount += 1;
            if (enterSetMode == SET_INTERSECT && triggerActiveCount == colliderCount)
            {
                if (!latchUntilLeave || !enterLatched)
                    _SendPlayerEnter(player);
                enterLatched = true;
            }

            if (triggerActiveCount > colliderCount)
                triggerActiveCount = colliderCount;
        }

        void _SendPlayerEnter(VRCPlayerApi player)
        {
            _UpdateHandlers(EVENT_PLAYER_ENTER, player);
            leaveLatched = false;
        }

        public override void _PlayerTriggerExit(VRCPlayerApi player)
        {
            if (!localPlayerOnly)
            {
                _SendPlayerLeave(player);
                return;
            }
            if (!player.isLocal)
                return;

            if (leaveSetMode == SET_NONE)
                _SendPlayerLeave(player);

            if (leaveSetMode == SET_INTERSECT && triggerActiveCount == colliderCount)
            {
                if (!latchUntilEnter || !leaveLatched)
                    _SendPlayerLeave(player);
                leaveLatched = true;
            }

            triggerActiveCount -= 1;
            if (leaveSetMode == SET_UNION && triggerActiveCount == 0)
                _SendPlayerLeave(player);

            if (triggerActiveCount < 0)
                triggerActiveCount = 0;
        }

        void _SendPlayerLeave(VRCPlayerApi player)
        {
            _UpdateHandlers(EVENT_PLAYER_LEAVE, player);
            enterLatched = false;
        }

        public void _Recalculate()
        {
            DebugLog("Recalculate");
            if (forceRecalc)
                return;

            colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (Utilities.IsValid(col) && col.enabled)
                {
                    col.enabled = false;
                    col.enabled = true;
                }
            }

            triggerActiveCount = 0;
            forceRecalc = true;
            pendingRecalc = false;

            SendCustomEventDelayedFrames("_FinishRecalc", 2);
        }

        public void _RecalculateNextEvent()
        {
            DebugLog("RecalculateNextEvent");
            pendingRecalc = true;
        }

        public void _FinishRecalc()
        {
            forceRecalc = false;
        }

        public override bool _LocalPlayerInZone()
        {
            if (!localPlayerOnly)
                return false;

            if (enterSetMode == SET_UNION)
                return triggerActiveCount > 0;
            if (enterSetMode == SET_INTERSECT)
                return triggerActiveCount == colliderCount || (latchUntilLeave && enterLatched);

            return false;
        }

        public void _LogEnter()
        {
            Debug.Log("Enter");
        }

        public void _LogLeave()
        {
            Debug.Log("Leave");
        }

        void DebugLog(string message)
        {
            if (vrcLog)
                Debug.Log("[Texel:CompZoneTrigger] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("CompZoneTrigger", message);
        }
    }
}
