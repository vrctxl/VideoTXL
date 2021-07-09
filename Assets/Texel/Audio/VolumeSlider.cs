
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Volume Slider")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VolumeSlider : UdonSharpBehaviour
    {
        public AudioSource audioSource;

        [Range(0, 1)]
        public float defaultVolume = 0.8f;

        [Header("Internal")]
        public Slider volumeSlider;
        public Image muteOnIcon;
        public Image muteOffIcon;

        void Start()
        {
            if (Utilities.IsValid(volumeSlider))
                volumeSlider.value = defaultVolume;

            _SetVolume(defaultVolume);
        }

        public void _SliderChanged()
        {
            if (Utilities.IsValid(volumeSlider))
                _SetVolume(volumeSlider.value);
        }

        public void _ToggleMute()
        {
            if (!Utilities.IsValid(audioSource))
                return;

            bool state = !audioSource.mute;
            audioSource.mute = state;

            if (Utilities.IsValid(muteOnIcon))
                muteOnIcon.enabled = state;
            if (Utilities.IsValid(muteOffIcon))
                muteOffIcon.enabled = !state;
        }

        void _SetVolume(float volume)
        {
            float expVolume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(volume * 5.757f) - 3.1623e-3f);
            if (Utilities.IsValid(audioSource))
                audioSource.volume = expVolume;
        }
    }
}