
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Component/Volume Controller")]
    public class VolumeController : UdonSharpBehaviour
    {
        
        public float volume = 0.85f;
        public bool muted = false;
        public bool audio2D = false;
        [Tooltip("Disable audio sources when video is not actively playing")]
        public bool disableUnusedSources = true;

        public AudioSource videoAudioSource;
        public AudioSource streamAudioSourceBase;
        public AudioSource[] streamAudioSourceOverlay;

        GameObject[] volumeControls;

        AnimationCurve storedVideoAudioBlend;
        AnimationCurve storedStreamAudioBlend;
        float scale2D = 1.05f;
        float scale3D = 3.5f;

        public ZoneController fadeZone;
        Bounds innerBox;
        Bounds outerBox;
        float zoneFadeScale = 1;

        private void Start()
        {
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

            Debug.Log("[VideoTXL:VolumeController] registering new controls set");
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
            Debug.Log("[VideoTXL:VolumeController] mute toggled");
            ApplyMute(!muted, false);
            UpdateControls();
        }

        public void _ToggleAudio2D()
        {
            Debug.Log("[VideoTXL:VolumeController] audio 2D toggled");
            ApplyAudio2D(!audio2D, false);
            UpdateControls();
        }

        public void _VideoStart()
        {
            if (!disableUnusedSources)
                return;

            Debug.Log("[VideoTXL:VolumeController] enable audio sources");
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

            Debug.Log("[VideoTXL:VolumeController] disable audio sources");
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
    }
}
