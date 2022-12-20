
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioManager : EventBase
    {
        //public SyncAudioManager syncAudioManager;
        //public bool useSync = false;
        public TXLVideoPlayer videoPlayer;
        public bool muteSourceForInactiveVideo = true;

        public UdonBehaviour audioLinkSystem;
        //public string audioLinkChannel;

        [Range(0, 1)]
        public float inputVolume = 1f;
        public bool inputMute = false;
        [Range(0, 1)]
        public float masterVolume = 0.9f;
        public bool masterMute = false;
        //public bool master2D = false;

        public AudioChannelGroup[] channelGroups;

        public bool debugLogging = true;
        public DebugLog debugLog;

        //public AudioSource[] channelAudio;
        //public string[] channelNames;
        //public AudioChannel[] channelData;

        //[Range(0, 1)]
        //public float[] channelVolume;
        //public bool[] channelMute;
        //public bool[] channel2D;
        //public AudioFadeZone[] channelFadeZone;
        //public bool[] channelDisableFade2D;
        //public bool[] channelMute2D;
        //public bool[] channelSeparateVolume2D;
        //[Range(0, 1)]
        //public float[] channelVolume2D;

        AudioChannelGroup selectedChannelGroup;
        VideoSource selectedVideoSource;
        VideoSourceAudioGroup activeAudioGroup;

        public const int EVENT_MASTER_VOLUME_UPDATE = 0;
        public const int EVENT_MASTER_MUTE_UPDATE = 1;
        //public const int EVENT_MASTER_2D_UPDATE = 2;
        //public const int EVENT_CHANNEL_VOLUME_UPDATE = 3;
        //public const int EVENT_CHANNEL_MUTE_UPDATE = 4;
        //public const int EVENT_CHANNEL_2D_UPDATE = 5;
        //public const int EVENT_CHANNEL_SOURCE_UPDATE = 6;
        const int EVENT_COUNT = 7;

        //float[] channelFade;
        //float[] spatialBlend;
        //AnimationCurve[] spatialCurve;

        bool initialized = false;
        Component[] audioControls;

        int channelCount = 0;
        //bool hasSync = false;
        //bool ovrMasterMute = false;
        //bool ovrMasterVolume = false;
        //bool ovrMaster2D = false;
        bool videoMute = false;

        const int UNITY = 0;
        const int AVPRO = 1;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            if (!Utilities.IsValid(audioControls))
                audioControls = new Component[0];

            foreach (AudioChannelGroup group in channelGroups)
            {
                if (group.unityChannel)
                    group.unityChannel._SetAudioManager(this);
                foreach (AudioChannel channel in group.avproChannels)
                {
                    if (channel)
                        channel._SetAudioManager(this);
                }
            }

            foreach (AudioChannelGroup group in channelGroups)
            {
                if (group)
                {
                    _SelectChannelGroup(group);
                    break;
                }
            }

            //channelCount = channelAudio.Length;
            //channelFade = new float[channelCount];
            //spatialBlend = new float[channelCount];
            //spatialCurve = new AnimationCurve[channelCount];

            /*for (int i = 0; i < channelCount; i++)
            {
                channelFade[i] = 1;
                if (i < channelFadeZone.Length && Utilities.IsValid(channelFadeZone[i]))
                    channelFadeZone[i]._RegisterAudioManager(this, i);

                if (Utilities.IsValid(channelAudio[i]))
                {
                    spatialCurve[i] = channelAudio[i].GetCustomCurve(AudioSourceCurveType.SpatialBlend);
                    if (!Utilities.IsValid(spatialCurve[i]))
                        spatialBlend[i] = channelAudio[i].spatialBlend;
                }
            }*/

            /*if (useSync && Utilities.IsValid(syncAudioManager))
            {
                hasSync = true;
                UdonBehaviour behavior = (UdonBehaviour)GetComponent(typeof(UdonBehaviour));
                syncAudioManager._Initialize(behavior, inputVolume, masterVolume, channelVolume, inputMute, masterMute, channelMute, channelNames);
            }*/

            initialized = true;

            _UpdateAll();
            _UpdateAudioLink();

            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_OnVideoStateUpdate");
                if (videoPlayer.videoMux)
                    videoPlayer.videoMux._Register(VideoMux.SOURCE_CHANGE_EVENT, this, "_OnVideoSourceChange");
            }
        }

        public void _SelectChannelGroup(AudioChannelGroup group)
        {
            if (group == selectedChannelGroup)
                return;

            selectedChannelGroup = group;
            _DebugLog($"Selected Audio Channel Group {group.groupName}");

            _UpdateActiveAudioGroup();
        }

        public void _SelectVideoSource(VideoSource source)
        {
            if (selectedVideoSource == source)
                return;

            selectedVideoSource = source;

            _UpdateActiveAudioGroup();
        }

        void _UpdateActiveAudioGroup()
        {
            _SetVideoAudioGroupState(activeAudioGroup, false);

            channelCount = 0;
            activeAudioGroup = null;

            if (!selectedChannelGroup || !selectedVideoSource)
            {
                _UpdateAll();
                _UpdateAudioLink(null);
                return;
            }

            switch (selectedVideoSource.VideoSourceType)
            {
                case VideoSource.VIDEO_SOURCE_UNITY:
                    channelCount = 1;
                    break;
                case VideoSource.VIDEO_SOURCE_AVPRO:
                    channelCount = selectedChannelGroup.avproChannels.Length;
                    break;
                case VideoSource.VIDEO_SOURCE_NONE:
                    _UpdateAll();
                    _UpdateAudioLink(null);
                    return;
            }
            
            foreach (var audioGroup in selectedVideoSource.audioGroups)
            {
                if (!audioGroup || audioGroup.groupName != selectedChannelGroup.groupName)
                    continue;

                activeAudioGroup = audioGroup;
                _SetVideoAudioGroupState(activeAudioGroup, true);
                break;
            }

            _UpdateAll();
            _UpdateAudioLink();
        }

        void _SetVideoAudioGroupState(VideoSourceAudioGroup group, bool enabled)
        {
            if (!group)
                return;

            // Unity
            if (selectedVideoSource.VideoSourceType == VideoSource.VIDEO_SOURCE_UNITY)
            {
                if (enabled)
                {
                    for (int i = 0; i < group.channelAudio.Length; i++)
                    {
                        if (group.channelAudio[i])
                        {
                            group.channelAudio[i].enabled = true;
                            if (group.channelReference[i])
                            {
                                _CopyAudioSourceProperties(group.channelReference[i].audioSourceTemplate, group.channelAudio[i]);
                                group.channelReference[i]._BindSource(group.channelAudio[i]);
                            }
                        }
                    }
                }
            }

            // AVPro
            if (selectedVideoSource.VideoSourceType == VideoSource.VIDEO_SOURCE_AVPRO)
            {
                for (int i = 0; i < group.channelAudio.Length; i++)
                {
                    if (group.channelAudio[i])
                        group.channelAudio[i].enabled = enabled;

                    if (enabled && group.channelReference[i])
                        group.channelReference[i]._BindSource(group.channelAudio[i]);
                }
            }
        }

        void _CopyAudioSourceProperties(AudioSource source, AudioSource target)
        {
            if (!source || !target)
                return;

            // Do not copy volume, mute

            target.spatialBlend = source.spatialBlend;
            target.spread = source.spread;
            target.minDistance = source.minDistance;
            target.maxDistance = source.maxDistance;
            target.spatialize = source.spatialize;
            target.rolloffMode = source.rolloffMode;
            target.reverbZoneMix = source.reverbZoneMix;
            target.dopplerLevel = source.dopplerLevel;

            
            if (source.GetCustomCurve(AudioSourceCurveType.SpatialBlend) != null)
                target.SetCustomCurve(AudioSourceCurveType.SpatialBlend, source.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
            if (source.GetCustomCurve(AudioSourceCurveType.Spread) != null)
                target.SetCustomCurve(AudioSourceCurveType.Spread, source.GetCustomCurve(AudioSourceCurveType.Spread));
            if (source.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix) != null)
                target.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, source.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
            if (source.GetCustomCurve(AudioSourceCurveType.CustomRolloff) != null)
                target.SetCustomCurve(AudioSourceCurveType.CustomRolloff, source.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
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

        public void _OnVideoStateUpdate()
        {
            if (!muteSourceForInactiveVideo)
                return;

            switch (videoPlayer.playerState)
            {
                case TXLVideoPlayer.VIDEO_STATE_PLAYING:
                    videoMute = false;
                    _UpdateAudioSources();
                    break;
                default:
                    videoMute = true;
                    _UpdateAudioSources();
                    break;
            }
        }

        public void _OnVideoSourceChange()
        {
            _SelectVideoSource(videoPlayer.videoMux.ActiveSource);
        }

        public void _SetMasterVolume(float value)
        {
            //ovrMasterVolume = true;
            masterVolume = value;

            _UpdateAll();
            _UpdateHandlers(EVENT_MASTER_VOLUME_UPDATE);
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
            //ovrMasterMute = true;
            masterMute = state;

            _UpdateAll();
            _UpdateHandlers(EVENT_MASTER_MUTE_UPDATE);
        }

        /*
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

            channelData[channel].mute = state;

            _UpdateAudioChannel(channel);
            _UpdateHandlers(EVENT_CHANNEL_MUTE_UPDATE, channel);
        }

        public AudioSource _GetChannelSource(string channel)
        {
            return _GetChannelSource(_GetChannelIndex(channel));
        }

        public AudioSource _GetChannelSource(int index)
        {
            if (index < 0 || index >= channelCount)
                return null;

            return activeAudioGroup.channelAudio[index];
        }*/

        /*
        public void _SetChannelSource(string channel, AudioSource source)
        {
            _SetChannelSource(_GetChannelIndex(channel), source);
        }

        public void _SetChannelSource(int channel, AudioSource source)
        {
            if (channel >= 0 && channel < channelCount)
            {
                channelAudio[channel] = source;
                if (source)
                    source.enabled = true;

                _UpdateAudioLink();
                _UpdateAudioChannel(channel);
                _UpdateHandlers(EVENT_CHANNEL_SOURCE_UPDATE, channel);
            }
        }
        */

            /*
        int _GetChannelIndex(string channel)
        {
            int index = -1;
            if (channel == null || channel == "")
                index = 0;
            else
            {
                for (int i = 0; i < channelCount; i++)
                {
                    if (channelData[i].channelName == channel)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }
        */

        /*
        public void _ClearChannelSources()
        {
            for (int i = 0; i < channelCount; i++)
            {
                if (channelAudio[i])
                    channelAudio[i].enabled = false;
                channelAudio[i] = null;
            }

            if (audioLinkSystem)
                audioLinkSystem.SetProgramVariable("audioSource", null);
        }
        */

        /*
        public void _SyncUpdate()
        {
            _UpdateAll();
        }
        */

        void _UpdateAll()
        {
            _UpdateAudioSources();
            _UpdateAudioControls();
        }

        void _UpdateAudioSources()
        {
            if (!activeAudioGroup)
                return;

            for (int i = 0; i < activeAudioGroup.channelReference.Length; i++)
            {
                AudioChannel channel = activeAudioGroup.channelReference[i];
                if (channel)
                    channel._UpdateAudioSource();
            }

            /*bool baseMute = _InputMute() || _MasterMute();
            float baseVolume = _InputVolume() * _MasterVolume();

            for (int i = 0; i < channelCount; i++)
                _UpdateAudioChannelWithBase(i, baseMute, baseVolume);*/
        }

        /*
        void _UpdateAudioChannel(int channel)
        {
            bool baseMute = _InputMute() || _MasterMute();
            float baseVolume = _InputVolume() * _MasterVolume();

            _UpdateAudioChannelWithBase(channel, baseMute, baseVolume);
        }

        // todo: private internal and cache some stuff
        void _UpdateAudioChannelWithBase(int channel, bool baseMute, float baseVolume)
        {
            AudioSource source = activeAudioGroup.channelAudio[channel];
            if (!Utilities.IsValid(source))
                return;

            float rawVolume = baseVolume * _ChannelVolume(channel) * channelFade[channel];
            float expVolume = Mathf.Clamp01(3.1623e-3f * Mathf.Exp(rawVolume * 5.757f) - 3.1623e-3f);

            source.mute = baseMute || _ChannelMute(channel);
            source.volume = expVolume;

            //if (Utilities.IsValid(spatialCurve[channel]))
            //    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialCurve[channel]);
            //else
            //    source.spatialBlend = spatialBlend[channel];

        }
        */

        public float _BaseVolume()
        {
            return _InputVolume() * _MasterVolume();
        }

        public bool _BaseMute()
        {
            return _InputMute() || _MasterMute();
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
            return /*hasSync ? syncAudioManager.syncInputMute :*/ inputMute;
        }

        float _InputVolume()
        {
            return /*hasSync ? syncAudioManager.syncInputVolume :*/ inputVolume;
        }

        bool _MasterMute()
        {
            return (/*(hasSync && !ovrMasterMute) ? syncAudioManager.syncMasterMute :*/ masterMute) || videoMute;
        }

        float _MasterVolume()
        {
            return /*(hasSync && !ovrMasterVolume) ? syncAudioManager.syncMasterVolume :*/ masterVolume;
        }

        //bool _ChannelMute(int channel)
        //{
        //    return (/*hasSync ? syncAudioManager.syncChannelMutes[channel] :*/ channelData[channel].mute) || videoMute;
        //}

        //float _ChannelVolume(int channel)
        //{
        //    return /*hasSync ? syncAudioManager.syncChannelVolumes[channel] :*/ channelData[channel].volume;
        //}

        void _UpdateAudioLink()
        {
            if (!audioLinkSystem || !activeAudioGroup)
            {
                _UpdateAudioLink(null);
                return;
            }

            AudioSource source = null;
            for (int i = 0; i < activeAudioGroup.channelReference.Length; i++)
            {
                AudioChannel channel = activeAudioGroup.channelReference[i];
                if (channel)
                {
                    AudioSource channelSource = activeAudioGroup.channelAudio[i];
                    if (channelSource)
                    {
                        if (channel.audioLinkSource)
                            source = channelSource;
                    }
                }
            }

            if (!source)
                source = selectedVideoSource.avproReservedChannel;

            _UpdateAudioLink(source);
        }

        void _UpdateAudioLink(AudioSource source)
        {
            if (!audioLinkSystem)
                return;

            audioLinkSystem.SetProgramVariable("audioSource", source);
        }

        void _DebugLog(string message)
        {
            if (debugLogging)
                Debug.Log("[VideoTXL:AudioManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioManager", message);
        }
    }
}