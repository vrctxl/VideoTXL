
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum AudioChannelTrack
    {
        STEREO,
        LEFT,
        RIGHT,
        THREE,
        FOUR,
        FIVE,
        SIX,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioChannel : EventBase
    {
        public string channelName;
        [Tooltip("The base volume level of this channel.  May be scaled back by mster volume and other influences.")]
        [Range(0, 1)]
        public float volume = 1;
        [Tooltip("Keeps volume locked to the value above, ignoring master volume, fade, and other influences.")]
        public bool lockVolume = false;
        [Tooltip("Whether this channel should be muted by default.")]
        public bool mute;
        [Tooltip("An optional fade zone to dynamically scale volume based on position/distance.")]
        public AudioFadeZone fadeZone;
        [Tooltip("AVPro: which audio track to output on the audio source.")]
        public AudioChannelTrack track;
        [Tooltip("Whether this channel should be used as the AudioLink source.  If no channel is selected, the first channel in the group will be used.")]
        public bool audioLinkSource = false;
        [Tooltip("An audio source that serves as a reference for copying settings to the live audio source used by the video player.")]
        public AudioSource audioSourceTemplate;

        [HideInInspector]
        public float fade = 1;

        bool active;

        AudioManager manager;
        AudioSource boundSource;

        public const int EVENT_VOUME_UPDATE = 0;
        public const int EVENT_MUTE_UPDATE = 1;
        public const int EVENT_FADE_UPDATE = 2;
        public const int EVENT_COUNT = 3;

        void Start()
        {
            _EnsureInit();

            if (fadeZone)
            {
                fadeZone._Register(AudioFadeZone.EVENT_FADE_UPDATE, this, "_OnFadeUpdate");
                fadeZone._SetActive(active);
            }
        }

        protected override int EventCount { get => EVENT_COUNT; }

        public void _SetAudioManager(AudioManager manager)
        {
            this.manager = manager;

            _UpdateAudioSource();
        }

        public void _BindSource(AudioSource source)
        {
            boundSource = source;

            _UpdateAudioSource();
        }

        public void _UpdateAudioSource()
        {
            if (!boundSource)
                return;

            float baseVolume = 1;
            bool baseMute = false;

            if (manager)
            {
                baseVolume = manager._BaseVolume();
                baseMute = manager._BaseMute();
            }

            float rawVolume = baseVolume * volume * fade;
            if (lockVolume)
                boundSource.volume = volume;
            else
                boundSource.volume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(rawVolume * 5.757f) - 3.1623e-3f);

            boundSource.mute = baseMute || mute;
        }

        public void _OnFadeUpdate()
        {
            fade = fadeZone.Fade;

            _UpdateAudioSource();
        }

        public void _SetActive(bool state)
        {
            if (active != state)
            {
                active = state;
                if (fadeZone)
                    fadeZone._SetActive(state);
            }
        }
    }
}
