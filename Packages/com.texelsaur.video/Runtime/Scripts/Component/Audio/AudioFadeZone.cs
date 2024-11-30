
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioFadeZone : EventBase
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

        [Tooltip("Volume approaches its target value at a linear rate determined by the delay.  The larger the delay, the slower volume will be to change.")]
        public float fadeDelay = 2;

        [Header("Performance")]
        [Tooltip("Whether the fade zone is active by default")]
        public bool active = true;
        [Tooltip("Interval in seconds that player position is checked for correct fade within the zones")]
        public float updateRate = 0.2f;
        [Tooltip("Force re-checking zone membership on player enter events.  May be needed in certain instances where you map can lose enter or leave events (such as stations within the zones).  You can save some performance by calling _RecalculateNextEvent yourself as needed.")]
        public bool forceColliderCheck = true;

        [Header("Experimental")]
        public Transform[] audioSourceLocations;
        public float[] innerRadius;
        public float[] outerRadius;

        [Header("Debug")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        [Tooltip("Write debug statements to VRChat log")]
        public bool vrcLog;

        //AudioManager audioManager;
        //int audioChannel;

        bool simpleCollider = false;
        Vector3 simpleOrigin;
        float simpleInnerRadius = 0;
        float simpleOuterRadius = 0;

        bool legacyCollider = true;
        bool hasAudioSource = false;
        //bool hasAudioManager = false;
        bool forceRecalc = false;
        bool pendingRecalc = false;
        bool finishRecalcQueued = false;

        int triggerCount = 0;
        float targetVolume;
        float currentVolume;

        bool fadeInit = false;

        public const int EVENT_FADE_UPDATE = 0;
        public const int EVENT_COUNT = 1;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            hasAudioSource = Utilities.IsValid(audioSource);

            if (!innerZone || !outerZone || audioSourceLocations.Length > 0)
                legacyCollider = false;

            // Try converting colocated spheres to audio source location
            if (innerZone && outerZone && audioSourceLocations.Length == 0)
            {
                SphereCollider innerSphere = (SphereCollider)innerZone;
                SphereCollider outerSphere = (SphereCollider)outerZone;

                if (innerZone.GetType() == typeof(SphereCollider) && outerZone.GetType() == typeof(SphereCollider) && innerSphere.center == outerSphere.center)
                {
                    DebugLog("Converting colliders to simple spherical interpolation");
                    legacyCollider = false;
                    simpleCollider = true;
                    simpleInnerRadius = innerSphere.radius;
                    simpleOuterRadius = outerSphere.radius;
                    simpleOrigin = innerSphere.center;

                    innerSphere.enabled = false;
                    outerSphere.enabled = false;
                    innerZone = null;
                    outerZone = null;
                }
            }

            if (active)
            {
                _UpdateFade();
                _InitInterpolateZoneFadeLoop();
            }
        }

        public float Fade
        {
            get { return currentVolume; }
        }

        /*
        public void _RegisterAudioManager(AudioManager manager, int channel)
        {
            if (!Utilities.IsValid(manager))
                return;

            audioManager = manager;
            audioChannel = channel;
            hasAudioManager = true;

            _UpdateFade();
        }
        */

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!legacyCollider || !player.isLocal)
                return;

            if ((forceColliderCheck || pendingRecalc) && triggerCount >= 1 && !forceRecalc)
            {
                SendCustomEventDelayedFrames(nameof(_Recalculate), 1);
                return;
            }

            if (forceRecalc && !finishRecalcQueued)
            {
                finishRecalcQueued = true;
                SendCustomEventDelayedFrames(nameof(_FinishRecalc), 2);
            }

            triggerCount += 1;
            DebugLog($"Trigger enter (count={triggerCount})");
        }

        

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!legacyCollider || !player.isLocal)
                return;

            triggerCount -= 1;
            DebugLog($"Trigger exit (count={triggerCount})");
        }

        public void _Recalculate()
        {
            if (!legacyCollider)
                return;

            DebugLog("Recalculate");
            if (forceRecalc)
                return;

            triggerCount = 0;
            forceRecalc = true;
            pendingRecalc = false;

            innerZone.enabled = false;
            outerZone.enabled = false;

            innerZone.enabled = true;
            outerZone.enabled = true;
        }

        public void _RecalculateNextEvent()
        {
            DebugLog("RecalculateNextEvent");
            pendingRecalc = true;
        }

        public void _FinishRecalc()
        {
            DebugLog("FinishRecalculate");
            finishRecalcQueued = false;
            forceRecalc = false;
        }

        void _InitInterpolateZoneFadeLoop()
        {
            if (!active)
                return;

            if (legacyCollider)
            {
                innerZone.enabled = false;
                outerZone.enabled = false;

                innerZone.enabled = true;
                outerZone.enabled = true;
            }

            _InterpolateZoneFadeLoop();
        }

        public void _InterpolateZoneFadeLoop()
        {
            if (!active)
                return;

            _InterpolateZoneFade();
            SendCustomEventDelayedSeconds("_InterpolateZoneFadeLoop", updateRate);
        }

        public void _SetActive(bool state)
        {
            if (active != state) {
                active = state;
                if (active)
                {
                    _UpdateFade();
                    _InitInterpolateZoneFadeLoop();
                }
            }
        }

        void _InterpolateZoneFade()
        {
            if (forceRecalc)
                return;

            float lastFade = targetVolume;
            if (simpleCollider)
            {
                VRCPlayerApi player = Networking.LocalPlayer;
                if (Utilities.IsValid(player))
                {
                    float calcVolume = 0;
                    Vector3 location = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

                    float dist = Vector3.Distance(location, transform.position + simpleOrigin);
                    if (dist < simpleInnerRadius)
                        calcVolume = Mathf.Max(calcVolume, upperBound);
                    else if (dist > simpleOuterRadius)
                        calcVolume = Mathf.Max(calcVolume, lowerBound);
                    else
                    {
                        float factor = (dist - simpleInnerRadius) / (simpleOuterRadius - simpleInnerRadius);
                        calcVolume = Mathf.Max(calcVolume, Mathf.Lerp(upperBound, lowerBound, factor));
                    }

                    targetVolume = calcVolume;
                }
            }
            else if (!legacyCollider)
            {

                VRCPlayerApi player = Networking.LocalPlayer;
                if (Utilities.IsValid(player))
                {
                    float calcVolume = 0;
                    Vector3 location = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

                    for (int i = 0; i < audioSourceLocations.Length; i++)
                    {
                        Transform t = audioSourceLocations[i];
                        if (!t)
                            continue;

                        float dist = Vector3.Distance(location, t.position);
                        if (dist < innerRadius[i])
                            calcVolume = Mathf.Max(calcVolume, upperBound);
                        else if (dist > outerRadius[i])
                            calcVolume = Mathf.Max(calcVolume, lowerBound);
                        else
                        {
                            float factor = (dist - innerRadius[i]) / (outerRadius[i] - innerRadius[i]);
                            calcVolume = Mathf.Max(calcVolume, Mathf.Lerp(upperBound, lowerBound, factor));
                        }
                    }

                    targetVolume = calcVolume;
                }
            }
            else
            {
                if (triggerCount == 0)
                    targetVolume = lowerBound;
                else if (triggerCount >= 2)
                    targetVolume = upperBound;
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
                            targetVolume = upperBound;
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
                            targetVolume = Mathf.Lerp(lowerBound, upperBound, playerDistOuter / zoneDist);
                        }
                    }
                }
            }

            if (lastFade != targetVolume || !fadeInit)
            {
                _Fade();
                _UpdateFade();
            }

            fadeInit = true;
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
                audioSource.volume = currentVolume;

            _UpdateHandlers(EVENT_FADE_UPDATE);

            //if (hasAudioManager)
            //    audioManager._SetChannelFade(audioChannel, zoneFadeScale);
        }

        bool fadeScheduled = false;

        void _Fade()
        {
            if (fadeScheduled)
                return;

            fadeScheduled = true;
            _FadeInner();
        }

        public void _FadeInner()
        {
            if (_StepTarget())
            {
                SendCustomEventDelayedFrames("_FadeInner", 1);
                _UpdateFade();
            }
            else
                fadeScheduled = false;
        }

        bool _StepTarget()
        {
            if (fadeDelay <= 0)
                currentVolume = targetVolume;

            if (currentVolume == targetVolume)
                return false;

            float step = (1f / fadeDelay) * Time.deltaTime;
            if (currentVolume > targetVolume)
                step = 0 - step;

            currentVolume = Mathf.Clamp01(currentVolume + step);

            if (step > 0 && currentVolume > targetVolume)
            {
                currentVolume = targetVolume;
                return false;
            } else if (step < 0 && currentVolume < targetVolume)
            {
                currentVolume = targetVolume;
                return false;
            }

            return true;
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