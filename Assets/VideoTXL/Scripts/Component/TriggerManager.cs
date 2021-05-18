
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Component/Trigger Manager")]
    public class TriggerManager : UdonSharpBehaviour
    {
        public bool startOnWorldEnter = false;
        public bool startOnZoneEnter = true;
        public bool startByControl = true;

        public bool stopOnZoneExit = true;
        public bool stopByControl = true;

        UdonBehaviour _videoPlayer;
        int _zoneCount = 0;
        bool _activeByWorld = false;
        bool _activeByZone = false;
        bool _activeByControl = false;

        public bool _IsTriggerActive()
        {
            return _activeByWorld || _activeByZone || _activeByControl;
        }

        public void _RegisterPlayer(UdonBehaviour player)
        {
            _videoPlayer = player;
            if (startOnWorldEnter)
            {
                _activeByWorld = true;
                TriggerPlayerStart();
            }
        }

        public void _ZoneEnter()
        {
            _zoneCount += 1;
            Debug.Log("[VideoTXL:TriggerManager] Enter zone, depth: " + _zoneCount);

            if (_zoneCount <= 1 && startOnZoneEnter)
            {
                _activeByZone = true;
                TriggerPlayerStart();
            }
        }

        public void _ZoneExit()
        {
            _zoneCount -= 1;
            Debug.Log("[VideoTXL:TriggerManager] Exit zone, depth: " + _zoneCount);

            if (_zoneCount <= 0)
            {
                _zoneCount = 0;
                if (stopOnZoneExit)
                {
                    _activeByZone = false;
                    _activeByWorld = false;
                    TriggerPlayerStop();
                }
            }
        }

        public void _ControlStart()
        {
            if (startByControl && !_activeByControl)
            {
                _activeByControl = true;
                TriggerPlayerStart();
            }
        }

        public void _ControlStop()
        {
            if (stopByControl && _activeByControl)
            {
                _activeByControl = false;
                _activeByWorld = false;
                TriggerPlayerStop();
            }
        }

        void TriggerPlayerStart()
        {
            if (_IsTriggerActive() && _videoPlayer != null)
                _videoPlayer.SendCustomEvent("_TriggerPlay");
        }

        void TriggerPlayerStop()
        {
            if (!_IsTriggerActive() && _videoPlayer != null)
                _videoPlayer.SendCustomEvent("_TriggerStop");
        }
    }
}