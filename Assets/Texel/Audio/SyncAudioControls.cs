
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Sync Audio Controls")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncAudioControls : UdonSharpBehaviour
    {
        public SyncAudioManager syncAudioManager;

        [Header("Source Input")]
        public Slider sourceSlider;
        public GameObject sourceMuteOn;
        public GameObject sourceMuteOff;

        [Header("Master Control")]
        public Slider masterSlider;
        public GameObject masterMuteOn;
        public GameObject masterMuteOff;

        [Header("Audio Channels")]
        public Slider[] channelSlider;
        public Text[] channelText;
        public GameObject[] channelMuteOn;
        public GameObject[] channelMuteOff;

        int channelCount = 0;

        void Start()
        {
            channelCount = channelSlider.Length;
            if (Utilities.IsValid(syncAudioManager))
                syncAudioManager._RegisterControls(gameObject);
        }

        bool inUpdate = false;

        public void _AudioManagerUpdate()
        {
            inUpdate = true;

            sourceSlider.value = syncAudioManager.syncInputVolume;
            sourceMuteOn.SetActive(syncAudioManager.syncInputMute);
            sourceMuteOff.SetActive(!syncAudioManager.syncInputMute);

            masterSlider.value = syncAudioManager.syncMasterVolume;
            masterMuteOn.SetActive(syncAudioManager.syncMasterMute);
            masterMuteOff.SetActive(!syncAudioManager.syncMasterMute);

            for (int i = 0; i < syncAudioManager.channelCount && i < channelCount; i++)
            {
                if (Utilities.IsValid(channelSlider[i]))
                    channelSlider[i].value = syncAudioManager.syncChannelVolumes[i];
                if (Utilities.IsValid(channelText[i]))
                    channelText[i].text = syncAudioManager.channelNames[i];
                if (Utilities.IsValid(channelMuteOn[i]))
                    channelMuteOn[i].SetActive(syncAudioManager.syncChannelMutes[i]);
                if (Utilities.IsValid(channelMuteOff[i]))
                    channelMuteOff[i].SetActive(!syncAudioManager.syncChannelMutes[i]);
            }

            inUpdate = false;
        }

        public void _SourceSliderChanged()
        {
            if (inUpdate)
                return;

            syncAudioManager._SetInputVolume(sourceSlider.value);
        }

        public void _MasterSliderChanged()
        {
            if (inUpdate)
                return;

            syncAudioManager._SetMasterVolume(masterSlider.value);
        }

        public void _SourceMuteToggled()
        {
            if (inUpdate)
                return;

            syncAudioManager._MuteInput(!syncAudioManager.syncInputMute);
        }

        public void _MasterMuteToggled()
        {
            if (inUpdate)
                return;

            syncAudioManager._MuteMaster(!syncAudioManager.syncMasterMute);
        }

        void _ChannelSliderChanged(int channel)
        {
            if (inUpdate || channelCount <= channel)
                return;

            syncAudioManager._SetChannelVolume(channel, channelSlider[channel].value);
        }

        void _ChannelMuteToggled(int channel)
        {
            if (inUpdate || channelCount <= channel)
                return;

            syncAudioManager._MuteChannel(channel, !syncAudioManager.syncChannelMutes[channel]);
        }

        public void _Channel0SliderChanged()
        {
            _ChannelSliderChanged(0);
        }

        public void _Channel0MuteToggled()
        {
            _ChannelMuteToggled(0);
        }

        public void _Channel1SliderChanged()
        {
            _ChannelSliderChanged(1);
        }

        public void _Channel1MuteToggled()
        {
            _ChannelMuteToggled(1);
        }

        public void _Channel2SliderChanged()
        {
            _ChannelSliderChanged(2);
        }

        public void _Channel2MuteToggled()
        {
            _ChannelMuteToggled(2);
        }

        public void _Channel3SliderChanged()
        {
            _ChannelSliderChanged(3);
        }

        public void _Channel3MuteToggled()
        {
            _ChannelMuteToggled(3);
        }

        public void _Channel4SliderChanged()
        {
            _ChannelSliderChanged(4);
        }

        public void _Channel4MuteToggled()
        {
            _ChannelMuteToggled(4);
        }

        public void _Channel5SliderChanged()
        {
            _ChannelSliderChanged(5);
        }

        public void _Channel5MuteToggled()
        {
            _ChannelMuteToggled(5);
        }

        public void _Channel6SliderChanged()
        {
            _ChannelSliderChanged(6);
        }

        public void _Channel6MuteToggled()
        {
            _ChannelMuteToggled(6);
        }

        public void _Channel7SliderChanged()
        {
            _ChannelSliderChanged(7);
        }

        public void _Channel7MuteToggled()
        {
            _ChannelMuteToggled(7);
        }
    }
}