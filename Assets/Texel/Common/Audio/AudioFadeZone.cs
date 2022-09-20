
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Fade Zone")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioFadeZone : UdonSharpBehaviour
    {
        [Header("Optional Components")]
        [Tooltip("An audio source to update directly.  Does not need to be specified if the Audio Fade Zone is attached to an Audio Manager")]
        public AudioSource audioSource;

        [Header("Configuration")]
        [Tooltip("An inner trigger collider, within which volume is set to the upper bound")]
        public Collider innerZone;

        [Tooltip("An outer trigger collider, beyond which volume is set to the lower bound")]
        public Collider outerZone;

        [Tooltip("The volume level within the inner zone")]
        [Range(0, 1)]
        public float upperBound = 1;
        [Tooltip("The volume level outside the outer zone")]
        [Range(0, 1)]
        public float lowerBound = 0;

        [Header("Performance")]
        [Tooltip("Interval in seconds that player position is checked for correct fade within the zones")]
        public float updateRate = 0.25f;
        [Tooltip("Force re-checking zone membership on player enter events.  May be needed in certain instances where you map can lose enter or leave events (such as stations within the zones).  You can save some performance by calling _RecalculateNextEvent yourself as needed.")]
        public bool forceColliderCheck = true;

        [Header("Debug")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        [Tooltip("Write debug statements to VRChat log")]
        public bool vrcLog;

        AudioManager audioManager;
        int audioChannel;

        bool hasAudioSource = false;
        bool hasAudioManager = false;
        bool forceRecalc = false;
        bool pendingRecalc = false;

        int triggerCount = 0;
        float zoneFadeScale;

        void Start()
        {
            hasAudioSource = Utilities.IsValid(audioSource);

            _UpdateFade();
            _InitInterpolateZoneFadeLoop();
        }

        public void _RegisterAudioManager(AudioManager manager, int channel)
        {
            if (!Utilities.IsValid(manager))
                return;

            audioManager = manager;
            audioChannel = channel;
            hasAudioManager = true;

            _UpdateFade();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;

            if ((forceColliderCheck || pendingRecalc) && triggerCount >= 1 && !forceRecalc)
            {
                _Recalculate();
                return;
            }

            triggerCount += 1;
            DebugLog($"Trigger enter (count={triggerCount})");
        }

        

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;

            triggerCount -= 1;
            DebugLog($"Trigger exit (count={triggerCount})");
        }

        public void _Recalculate()
        {
            DebugLog("Recalculate");
            if (forceRecalc)
                return;

            innerZone.enabled = false;
            outerZone.enabled = false;

            innerZone.enabled = true;
            outerZone.enabled = true;

            triggerCount = 0;
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

        void _InitInterpolateZoneFadeLoop()
        {
            if (!Utilities.IsValid(innerZone) || !Utilities.IsValid(outerZone))
                return;

            innerZone.enabled = false;
            outerZone.enabled = false;

            innerZone.enabled = true;
            outerZone.enabled = true;

            _InterpolateZoneFadeLoop();
        }

        public void _InterpolateZoneFadeLoop()
        {
            _InterpolateZoneFade();
            SendCustomEventDelayedSeconds("_InterpolateZoneFadeLoop", updateRate);
        }

        void _InterpolateZoneFade()
        {
            if (forceRecalc)
                return;

            float lastFade = zoneFadeScale;
            if (triggerCount == 0)
                zoneFadeScale = lowerBound;
            else if (triggerCount >= 2)
                zoneFadeScale = upperBound;
            else
            {
                VRCPlayerApi player = Networking.LocalPlayer;
                if (Utilities.IsValid(player))
                {
                    Vector3 location = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                    Vector3 innerPoint = innerZone.ClosestPoint(location);
                    Vector3 dirVector = location - innerPoint;
                    dirVector = Vector3.Normalize(dirVector);
                    if (dirVector.magnitude < .98)
                        zoneFadeScale = upperBound;
                    else
                    {
                        float length = outerZone.bounds.size.magnitude;
                        Ray ray = new Ray(location, dirVector);
                        ray.origin = ray.GetPoint(length);
                        ray.direction = -ray.direction;

                        RaycastHit hit;
                        outerZone.Raycast(ray, out hit, length * 2);
                        Vector3 outerPoint = hit.point;

                        Vector3 locPoint = _NearestPoint(innerPoint, outerPoint, location);

                        float zoneDist = Vector3.Distance(innerPoint, outerPoint);
                        float playerDistOuter = Vector3.Distance(locPoint, outerPoint);
                        zoneFadeScale = Mathf.Lerp(lowerBound, upperBound, playerDistOuter / zoneDist);
                    }
                }
            }

            if (lastFade != zoneFadeScale)
                _UpdateFade();
        }

        Vector3 _NearestPoint(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 heading = end - start;
            float mag = heading.magnitude;
            heading = Vector3.Normalize(heading);

            Vector3 lhs = point - start;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0, mag);

            return start + heading * dotP;
        }

        void _UpdateFade()
        {
            if (hasAudioSource)
                audioSource.volume = zoneFadeScale;

            if (hasAudioManager)
                audioManager._SetChannelFade(audioChannel, zoneFadeScale);
        }

        void DebugLog(string message)
        {
            if (vrcLog)
                Debug.Log("[Texel:AudioFadeZone] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioFadeZone", message);
        }
    }
}