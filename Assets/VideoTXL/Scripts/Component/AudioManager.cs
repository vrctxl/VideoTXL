
using Texel;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Component/Audio Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioManager : UdonSharpBehaviour
    {
        [Header("Optional Components")]
        [Tooltip("A proxy for dispatching video-related events to this object")]
        public VideoPlayerProxy dataProxy;
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Header("Default Options")]
        [Range(0, 1)] [Tooltip("Default volume level of base audio sources.  Overlay sources are scaled relative to this.")]
        public float volume = 0.85f;
        [Tooltip("Whether audio sources are muted by default")]
        public bool muted = false;
        [Tooltip("Whether base sources are reconfigured to a full 2D blend by default.  When enabled, overlay sources are also muted.")]
        public bool audio2D = false;
        [Tooltip("Disable audio sources when video is not actively playing.  Not recommended for sources attached to AVPro.")]
        public bool disableUnusedSources = false;

        [Header("Audio Sources")]
        public AudioSource videoAudioSource;
        public AudioSource streamAudioSourceBase;
        public AudioSource[] streamAudioSourceOverlay;

        [Header("Audio Fade")]
        public ZoneController fadeZone;

        GameObject[] volumeControls;

        AnimationCurve storedVideoAudioBlend;
        AnimationCurve storedStreamAudioBlend;
        float scale2D = 1.05f;
        float scale3D = 3.5f;

        
        Bounds innerBox;
        Bounds outerBox;
        float zoneFadeScale = 1;

        bool sourcesEnabled = true;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;
        const int PLAYER_STATE_PAUSED = 4;

        private void Start()
        {
            if (Utilities.IsValid(dataProxy))
                dataProxy._RegisterEventHandler(gameObject, "_VideoStateUpdate");

            if (!Utilities.IsValid(volumeControls))
                volumeControls = new GameObject[0];

            ApplyMute(muted, true);
            ApplyAudio2D(audio2D, true);
            ApplyVolumeFromSlider(volume);

            SendCustomEventDelayedFrames("_InitializeControls", 1);

            _VideoStop();

            if (Utilities.IsValid(fadeZone))
                SendCustomEventDelayedFrames("_InitInterpolateZoneFadeLoop", 1);
        }

        public void _RegisterControls(GameObject controls)
        {
            if (!Utilities.IsValid(controls))
                return;

            if (!Utilities.IsValid(volumeControls))
                volumeControls = new GameObject[0];

            foreach (GameObject c in volumeControls)
            {
                if (c == controls)
                    return;
            }

            GameObject[] newControls = new GameObject[volumeControls.Length + 1];
            for (int i = 0; i < volumeControls.Length; i++)
                newControls[i] = volumeControls[i];

            newControls[volumeControls.Length] = controls;
            volumeControls = newControls;
        }

        public void _InitializeControls()
        {
            UpdateControls();
        }

        public void _ApplyVolume(float value)
        {
            if (volume == value)
                return;

            volume = value;
            ApplyVolumeFromSlider(volume);
            UpdateControls();
        }

        public void _ToggleMute()
        {
            DebugLog("mute toggled");
            ApplyMute(!muted, false);
            UpdateControls();
        }

        public void _ToggleAudio2D()
        {
            DebugLog("audio 2D toggled");
            ApplyAudio2D(!audio2D, false);
            UpdateControls();
        }

        public void _VideoStateUpdate()
        {
            switch (dataProxy.playerState)
            {
                case PLAYER_STATE_PLAYING:
                case PLAYER_STATE_LOADING:
                case PLAYER_STATE_PAUSED:
                    if (!sourcesEnabled)
                        _VideoStart();
                    break;
                default:
                    if (sourcesEnabled)
                        _VideoStop();
                    break;
            }
        }

        public void _VideoStart()
        {
            if (!disableUnusedSources)
                return;
            if (sourcesEnabled)
                return;

            sourcesEnabled = true;

            DebugLog("enable audio sources");
            if (Utilities.IsValid(videoAudioSource))
            {
                videoAudioSource.enabled = true;
                videoAudioSource.mute = false;
            }
            if (Utilities.IsValid(streamAudioSourceBase))
            {
                streamAudioSourceBase.enabled = true;
                streamAudioSourceBase.mute = false;
            }
            foreach (AudioSource source in streamAudioSourceOverlay)
            {
                if (Utilities.IsValid(source))
                {
                    source.enabled = true;
                    source.mute = false;
                }
            }

            ApplyVolumeFromSlider(volume);
        }

        public void _VideoStop()
        {
            if (!disableUnusedSources)
                return;
            if (!sourcesEnabled)
                return;

            sourcesEnabled = false;

            DebugLog("disable audio sources");
            if (Utilities.IsValid(videoAudioSource))
                videoAudioSource.mute = true;
            if (Utilities.IsValid(streamAudioSourceBase))
                streamAudioSourceBase.mute = true;
            foreach (AudioSource source in streamAudioSourceOverlay)
            {
                if (Utilities.IsValid(source))
                    source.mute = true;
            }

            SendCustomEventDelayedFrames("_VideoStopDelay", 1);
        }

        public void _VideoStopDelay()
        {
            if (Utilities.IsValid(videoAudioSource))
                videoAudioSource.enabled = false;
            if (Utilities.IsValid(streamAudioSourceBase))
                streamAudioSourceBase.enabled = false;
            foreach (AudioSource source in streamAudioSourceOverlay)
            {
                if (Utilities.IsValid(source))
                    source.enabled = false;
            }
        }

        public void _InitInterpolateZoneFadeLoop()
        {
            if (!Utilities.IsValid(fadeZone.enterCollider) || !Utilities.IsValid(fadeZone.exitCollider))
                return;

            innerBox = fadeZone.enterCollider.bounds;
            outerBox = fadeZone.exitCollider.bounds;

            _InterpolateZoneFadeLoop();
        }

        public void _InterpolateZoneFadeLoop()
        {
            InterpolateZoneFade();
            SendCustomEventDelayedSeconds("_InterpolateZoneFadeLoop", 0.25f);
        }

        private void InterpolateZoneFade()
        {
            if (!fadeZone.inExitZone)
                zoneFadeScale = 0;
            else if (fadeZone.inEnterZone)
                zoneFadeScale = 1;
            else
            {
                Vector3 location = Networking.LocalPlayer.GetPosition();
                Vector3 innerPoint = innerBox.ClosestPoint(location);
                Vector3 dirVector = innerPoint - location;
                dirVector.Normalize();
                Ray ray = new Ray(location, dirVector);
                float hitDist;
                outerBox.IntersectRay(ray, out hitDist);
                Vector3 outerPoint = ray.GetPoint(hitDist);
                float zoneDist = Vector3.Distance(innerPoint, outerPoint);
                float playerDist = Vector3.Distance(location, innerPoint);
                zoneFadeScale = (zoneDist - playerDist) / zoneDist;
                ApplyVolumeFromSlider(volume);
            }
        }

        private void UpdateControls()
        {
            foreach (var panel in volumeControls)
            {
                if (Utilities.IsValid(panel))
                {
                    UdonBehaviour script = (UdonBehaviour)panel.GetComponent(typeof(UdonBehaviour));
                    if (Utilities.IsValid(script))
                        script.SendCustomEvent("_VolumeControllerUpdate");
                }
            }
        }

        private void ApplyMute(bool state, bool init)
        {
            if (muted == state && !init)
                return;

            muted = state;
            ApplyVolumeFromSlider(volume);
        }

        private void ApplyAudio2D(bool state, bool init)
        {
            if (audio2D == state && !init)
                return;

            audio2D = state;

            if (audio2D)
            {
                if (Utilities.IsValid(videoAudioSource))
                {
                    storedVideoAudioBlend = videoAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                    videoAudioSource.spatialBlend = 0;
                }

                if (Utilities.IsValid(streamAudioSourceBase))
                {
                    storedStreamAudioBlend = streamAudioSourceBase.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                    streamAudioSourceBase.spatialBlend = 0;
                }

                foreach (AudioSource source in streamAudioSourceOverlay)
                {
                    if (Utilities.IsValid(source))
                        source.mute = true;
                }
            }
            else
            {
                if (Utilities.IsValid(videoAudioSource) && Utilities.IsValid(storedVideoAudioBlend))
                    videoAudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, storedVideoAudioBlend);
                if (Utilities.IsValid(streamAudioSourceBase) && Utilities.IsValid(storedStreamAudioBlend))
                    streamAudioSourceBase.SetCustomCurve(AudioSourceCurveType.SpatialBlend, storedStreamAudioBlend);

                foreach (AudioSource source in streamAudioSourceOverlay)
                {
                    if (Utilities.IsValid(source))
                        source.mute = false;
                }
            }

            ApplyVolumeFromSlider(volume);
        }

        private void ApplyVolumeFromSlider(float position)
        {
            float applyVolume = position;
            if (muted)
                applyVolume = 0;

            // https://www.dr-lex.be/info-stuff/volumecontrols.html#ideal thanks TCL for help with finding and understanding this
            // Using the 50dB dynamic range constants
            float audioVolume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(applyVolume * 5.757f) - 3.1623e-3f);
            // float audioVolume = Mathf.Clamp01(0.173702f * Mathf.Log(applyVolume * 316.226f));
            // float audioVolume = applyVolume;

            float baseScale = scale2D;
            if (audio2D)
                baseScale = 1;

            if (Utilities.IsValid(videoAudioSource))
                videoAudioSource.volume = audioVolume * zoneFadeScale;
            if (Utilities.IsValid(streamAudioSourceBase))
                streamAudioSourceBase.volume = (audioVolume / baseScale) * zoneFadeScale;
            
            foreach (AudioSource source in streamAudioSourceOverlay)
            {
                if (Utilities.IsValid(source))
                    source.volume = (audioVolume / scale3D) * zoneFadeScale;
            }
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:AudioManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioManager", message);
        }
    }
}
