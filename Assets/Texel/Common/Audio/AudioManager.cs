
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
        public SyncAudioManager syncAudioManager;
        public bool useSync = false;
        public VideoPlayerProxy dataProxy;
        public bool muteSourceForInactiveVideo = true;

        [Range(0, 1)]
        public float inputVolume = 1f;
        public bool inputMute = false;
        [Range(0, 1)]
        public float masterVolume = 0.9f;
        public bool masterMute = false;
        public bool master2D = false;

        public AudioSource[] channelAudio;
        public string[] channelNames;
        [Range(0, 1)]
        public float[] channelVolume;
        public bool[] channelMute;
        public bool[] channel2D;
        public AudioFadeZone[] channelFadeZone;
        public bool[] channelDisableFade2D;
        public bool[] channelMute2D;
        public bool[] channelSeparateVolume2D;
        [Range(0, 1)]
        public float[] channelVolume2D;

        float[] channelFade;
        float[] spatialBlend;
        AnimationCurve[] spatialCurve;

        bool initialized = false;
        Component[] audioControls;

        int channelCount = 0;
        bool hasSync = false;
        bool ovrMasterMute = false;
        bool ovrMasterVolume = false;
        bool ovrMaster2D = false;
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
            spatialBlend = new float[channelCount];
            spatialCurve = new AnimationCurve[channelCount];

            for (int i = 0; i < channelCount; i++)
            {
                channelFade[i] = 1;
                if (i < channelFadeZone.Length && Utilities.IsValid(channelFadeZone[i]))
                    channelFadeZone[i]._RegisterAudioManager(this, i);

                if (Utilities.IsValid(channelAudio[i])) {
                    spatialCurve[i] = channelAudio[i].GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                    if (!Utilities.IsValid(spatialCurve[i]))
                        spatialBlend[i] = channelAudio[i].spatialBlend;
                }
            }

            if (useSync && Utilities.IsValid(syncAudioManager))
            {
                hasSync = true;
                UdonBehaviour behavior = (UdonBehaviour)GetComponent(typeof(UdonBehaviour));
                syncAudioManager._Initialize(behavior, inputVolume, masterVolume, channelVolume, inputMute, masterMute, master2D, channelMute, channel2D, channelNames);
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

        public void _SetMasterMuted()
        {
            _SetMasterMute(true);
        }

        public void _SetMasterUnmuted()
        {
            _SetMasterMute(false);
        }

        public void _SetMasterMute(bool state)
        {
            ovrMasterMute = true;
            masterMute = state;

            _UpdateAll();
        }

        public void _SetMaster2D(bool state)
        {
            ovrMaster2D = true;
            master2D = state;

            _UpdateAll();
        }

        public void _SetChannelFade(int channel, float fade)
        {
            if (channel < 0 || channel >= channelCount)
                return;

            channelFade[channel] = Mathf.Clamp01(fade);

            _UpdateAudioChannel(channel);
        }

        public void _SetChannelMute(int channel, bool state)
        {
            if (channel < 0 || channel >= channelCount)
                return;

            channelMute[channel] = state;

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
            bool base2D = _Master2D();

            for (int i = 0; i < channelCount; i++)
                _UpdateAudioChannelWithBase(i, baseMute, baseVolume, base2D);
        }

        void _UpdateAudioChannel(int channel)
        {
            bool baseMute = _InputMute() || _MasterMute();
            float baseVolume = _InputVolume() * _MasterVolume();
            bool base2D = _Master2D();

            _UpdateAudioChannelWithBase(channel, baseMute, baseVolume, base2D);
        }

        // todo: private internal and cache some stuff
        void _UpdateAudioChannelWithBase(int channel, bool baseMute, float baseVolume, bool base2D)
        {
            AudioSource source = channelAudio[channel];
            if (!Utilities.IsValid(source))
                return;

            float rawVolume = baseVolume * _ChannelVolume(channel) * channelFade[channel];
            float expVolume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(rawVolume * 5.757f) - 3.1623e-3f);

            source.mute = baseMute || _ChannelMute(channel);
            source.volume = expVolume;

            bool audio2D = base2D || _Channel2D(channel);

            // Update 2D/3D audio
            if (audio2D)
            {
                spatialCurve[channel] = source.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                if (!Utilities.IsValid(spatialCurve[channel]))
                    spatialBlend[channel] = source.spatialBlend;
                source.spatialBlend = 0;
            } else
            {
                if (Utilities.IsValid(spatialCurve[channel]))
                    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialCurve[channel]);
                else
                    source.spatialBlend = spatialBlend[channel];
            }

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

        bool _Master2D()
        {
            return (hasSync && !ovrMaster2D) ? syncAudioManager.syncMaster2D : master2D;
        }

        bool _ChannelMute(int channel)
        {
            return (hasSync ? syncAudioManager.syncChannelMutes[channel] : channelMute[channel]) || videoMute;
        }

        float _ChannelVolume(int channel)
        {
            return hasSync ? syncAudioManager.syncChannelVolumes[channel] : channelVolume[channel];
        }

        bool _Channel2D(int channel)
        {
            return hasSync ? syncAudioManager.syncChannel2Ds[channel] : channel2D[channel];
        }
    }
}