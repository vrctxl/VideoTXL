
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioManager : UdonSharpBehaviour
    {
        [Header("Optional Components")]
        [Tooltip("A sync attachment to AudioManager that will share audio settings with all players, allowing some to be locally overridden.")]
        public SyncAudioManager syncAudioManager;
        [Tooltip("A proxy for dispatching video-related events to this object")]
        public VideoPlayerProxy dataProxy;
        [Tooltip("Mute audio when video source is not actively playing")]
        public bool muteSourceForInactiveVideo = true;

        [Header("Default Options")]
        [Range(0, 1)]
        public float inputVolume = 1f;
        public bool inputMute = false;
        [Range(0, 1)]
        public float masterVolume = 0.9f;
        public bool masterMute = false;

        [Header("Audio Channels")]
        public AudioSource[] channelAudio;
        public string[] channelNames;
        [Range(0, 1)]
        public float[] channelVolume;
        public bool[] channelMute;
        public AudioFadeZone[] channelFadeZone;

        float[] channelFade;

        bool initialized = false;
        Component[] audioControls;

        int channelCount = 0;
        bool hasSync = false;
        bool ovrMasterMute = false;
        bool ovrMasterVolume = false;
        bool videoMute = false;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        void Start()
        {
            if (Utilities.IsValid(dataProxy))
                dataProxy._RegisterEventHandler(this, "_VideoStateUpdate");
            if (!Utilities.IsValid(audioControls))
                audioControls = new Component[0];

            channelCount = channelAudio.Length;
            channelFade = new float[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                channelFade[i] = 1;
                if (i < channelFadeZone.Length && Utilities.IsValid(channelFadeZone[i]))
                    channelFadeZone[i]._RegisterAudioManager(this, i);
            }

            if (Utilities.IsValid(syncAudioManager))
            {
                hasSync = true;
                UdonBehaviour behavior = (UdonBehaviour)GetComponent(typeof(UdonBehaviour));
                syncAudioManager._Initialize(behavior, inputVolume, masterVolume, channelVolume, inputMute, masterMute, channelMute, channelNames);
            }

            initialized = true;
            _UpdateAll();
        }

        public void _RegisterControls(Component controls)
        {
            if (!Utilities.IsValid(controls))
                return;

            if (!Utilities.IsValid(audioControls))
                audioControls = new Component[0];

            foreach (Component c in audioControls)
            {
                if (c == controls)
                    return;
            }

            Component[] newControls = new Component[audioControls.Length + 1];
            for (int i = 0; i < audioControls.Length; i++)
                newControls[i] = audioControls[i];

            newControls[audioControls.Length] = controls;
            audioControls = newControls;

            if (initialized)
                _UpdateAudioControl(controls);
        }

        public void _VideoStateUpdate()
        {
            if (!muteSourceForInactiveVideo)
                return;

            switch (dataProxy.playerState)
            {
                case PLAYER_STATE_PLAYING:
                    videoMute = false;
                    _UpdateAudioSources();
                    break;
                default:
                    videoMute = true;
                    _UpdateAudioSources();
                    break;
            }
        }

        public void _SetMasterVolume(float value)
        {
            ovrMasterVolume = true;
            masterVolume = value;

            _UpdateAll();
        }

        public void _SetMasterMute(bool state)
        {
            ovrMasterMute = true;
            masterMute = state;

            _UpdateAll();
        }

        public void _SetChannelFade(int channel, float fade)
        {
            if (channel < 0 || channel >= channelCount)
                return;

            channelFade[channel] = Mathf.Clamp01(fade);

            _UpdateAudioChannel(channel);
        }

        public void _SyncUpdate()
        {
            _UpdateAll();
        }

        void _UpdateAll()
        {
            _UpdateAudioSources();
            _UpdateAudioControls();
        }

        void _UpdateAudioSources()
        {
            bool baseMute = _InputMute() || _MasterMute();
            float baseVolume = _InputVolume() * _MasterVolume();

            for (int i = 0; i < channelCount; i++)
                _UpdateAudioChannelWithBase(i, baseMute, baseVolume);
        }

        void _UpdateAudioChannel(int channel)
        {
            bool baseMute = _InputMute() || _MasterMute();
            float baseVolume = _InputVolume() * _MasterVolume();

            _UpdateAudioChannelWithBase(channel, baseMute, baseVolume);
        }

        // todo: private internal and cache some stuff
        void _UpdateAudioChannelWithBase(int channel, bool baseMute, float baseVolume)
        {
            AudioSource source = channelAudio[channel];
            if (!Utilities.IsValid(source))
                return;

            float rawVolume = baseVolume * _ChannelVolume(channel) * channelFade[channel];
            float expVolume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(rawVolume * 5.757f) - 3.1623e-3f);

            source.mute = baseMute || _ChannelMute(channel);
            source.volume = expVolume;
        }

        void _UpdateAudioControls()
        {
            foreach (var control in audioControls)
                _UpdateAudioControl(control);
        }

        void _UpdateAudioControl(Component control)
        {
            if (!Utilities.IsValid(control))
                return;

            UdonBehaviour script = (UdonBehaviour)control;
            if (Utilities.IsValid(script))
                script.SendCustomEvent("_AudioManagerUpdate");
        }

        bool _InputMute()
        {
            return hasSync ? syncAudioManager.syncInputMute : inputMute;
        }

        float _InputVolume()
        {
            return hasSync ? syncAudioManager.syncInputVolume : inputVolume;
        }

        bool _MasterMute()
        {
            return ((hasSync && !ovrMasterMute) ? syncAudioManager.syncMasterMute : masterMute) || videoMute;
        }

        float _MasterVolume()
        {
            return (hasSync && !ovrMasterVolume) ? syncAudioManager.syncMasterVolume : masterVolume;
        }

        bool _ChannelMute(int channel)
        {
            return (hasSync ? syncAudioManager.syncChannelMutes[channel] : channelMute[channel]) || videoMute;
        }

        float _ChannelVolume(int channel)
        {
            return hasSync ? syncAudioManager.syncChannelVolumes[channel] : channelVolume[channel];
        }
    }
}