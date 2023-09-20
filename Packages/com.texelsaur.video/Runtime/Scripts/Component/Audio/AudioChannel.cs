
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
        SEVEN,
        EIGHT,
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
        [Tooltip("Preserves channel's mute state, ignoring master mute.")]
        public bool lockMute = false;
        //[Tooltip("Preserves channel's spatial audio settings, ignoring master 2D audio.")]
        //public bool lockSpatial = false;
        [Tooltip("Whether this channel should be muted by default.")]
        public bool mute;
        [Tooltip("An optional fade zone to dynamically scale volume based on position/distance.")]
        public AudioFadeZone fadeZone;
        [Tooltip("AVPro: which audio track to output on the audio source.")]
        public AudioChannelTrack track;
        [Tooltip("Whether this channel should be used as the AudioLink source.  If no channel is selected, the first channel in the group will be used.")]
        public bool audioLinkSource = false;
        [Tooltip("Whether this channel should be used as the VRSL Audio DMX source.  If no channel is selected, audio will be unlinked from VRSL.")]
        public bool vrslAudioDMXSource = false;
        [Tooltip("An audio source that serves as a reference for copying settings to the live audio source used by the video player.")]
        public AudioSource audioSourceTemplate;

        [HideInInspector]
        public float fade = 1;

        bool active;
        float spatialBlend;
        AnimationCurve spatialCurve;

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
            if (!boundSource)
                return;

            spatialBlend = source.spatialBlend;
            spatialCurve = source.GetCustomCurve(AudioSourceCurveType.SpatialBlend);

            _UpdateAudioSource();

            _UpdateHighPassFilter(source);
            _UpdateLowPassFilter(source);
            _UpdateDistortionFilter(source);
            _UpdateEchoFilter(source);
            _UpdateChorusFilter(source);
        }

        void _UpdateHighPassFilter(AudioSource source)
        {
            AudioHighPassFilter filter = source.GetComponent<AudioHighPassFilter>();
            if (filter)
            {
                filter.enabled = false;
                AudioHighPassFilter refFilter = GetComponent<AudioHighPassFilter>();
                if (refFilter)
                {
                    filter.enabled = refFilter.enabled;
                    filter.cutoffFrequency = refFilter.cutoffFrequency;
                    filter.highpassResonanceQ = refFilter.highpassResonanceQ;
                }
            }
        }

        void _UpdateLowPassFilter(AudioSource source)
        {
            AudioLowPassFilter filter = source.GetComponent<AudioLowPassFilter>();
            if (filter)
            {
                filter.enabled = false;
                AudioLowPassFilter refFilter = GetComponent<AudioLowPassFilter>();
                if (refFilter)
                {
                    filter.enabled = refFilter.enabled;
                    filter.cutoffFrequency = refFilter.cutoffFrequency;
                    filter.lowpassResonanceQ = refFilter.lowpassResonanceQ;
                }
            }
        }

        void _UpdateDistortionFilter(AudioSource source)
        {
            AudioDistortionFilter filter = source.GetComponent<AudioDistortionFilter>();
            if (filter)
            {
                filter.enabled = false;
                AudioDistortionFilter refFilter = GetComponent<AudioDistortionFilter>();
                if (refFilter)
                {
                    filter.enabled = refFilter.enabled;
                    filter.distortionLevel = refFilter.distortionLevel;
                }
            }
        }

        void _UpdateEchoFilter(AudioSource source)
        {
            AudioEchoFilter filter = source.GetComponent<AudioEchoFilter>();
            if (filter)
            {
                filter.enabled = false;
                AudioEchoFilter refFilter = GetComponent<AudioEchoFilter>();
                if (refFilter)
                {
                    filter.enabled = refFilter.enabled;
                    filter.decayRatio = refFilter.decayRatio;
                    filter.delay = refFilter.delay;
                    filter.dryMix = refFilter.dryMix;
                    filter.wetMix = refFilter.wetMix;
                }
            }
        }

        void _UpdateChorusFilter(AudioSource source)
        {
            AudioChorusFilter filter = source.GetComponent<AudioChorusFilter>();
            if (filter)
            {
                filter.enabled = false;
                AudioChorusFilter refFilter = GetComponent<AudioChorusFilter>();
                if (refFilter)
                {
                    filter.enabled = refFilter.enabled;
                    filter.delay = refFilter.delay;
                    filter.depth = refFilter.depth;
                    filter.dryMix = refFilter.dryMix;
                    filter.rate = refFilter.rate;
                    filter.wetMix1 = refFilter.wetMix1;
                    filter.wetMix2 = refFilter.wetMix2;
                    filter.wetMix3 = refFilter.wetMix3;
                }
            }
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

            float rawVolume = baseVolume * volume;
            if (fadeZone && fadeZone.active)
                rawVolume *= fade;

            if (lockVolume)
                boundSource.volume = volume;
            else
                boundSource.volume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(rawVolume * 5.757f) - 3.1623e-3f);

            boundSource.mute = baseMute || mute;

            // Update 2D/3D audio
            /*if (base2D)
            {
                spatialCurve = boundSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                if (!Utilities.IsValid(spatialCurve))
                    spatialBlend = boundSource.spatialBlend;
                boundSource.spatialBlend = 0;
            }
            else
            {*/
                if (Utilities.IsValid(spatialCurve))
                    boundSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialCurve);
                else
                    boundSource.spatialBlend = spatialBlend;
            //}

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
