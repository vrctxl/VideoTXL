
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalControls : UdonSharpBehaviour
    {
        public UdonBehaviour videoPlayer;
        public StaticUrlSource staticUrlSource;
        public AudioManager AudioManager;
        //public ControlColorProfile colorProfile;

        public bool autoLayout = true;
        public bool enableResync = true;
        public bool enableQualitySelect = false;
        public bool enableVolume = true;
        public bool enable2DAudioToggle = false;
        public bool enableMessageBar = true;

        public GameObject volumeGroup;
        public GameObject resyncGroup;
        public GameObject toggleGroup;
        public GameObject messageBarGroup;

        public GameObject volumeSliderControl;
        public GameObject audio2DControl;

        public GameObject toggle720On;
        public GameObject toggle720Off;
        public GameObject toggle1080On;
        public GameObject toggle1080Off;
        public GameObject toggleAudioOn;
        public GameObject toggleAudioOff;
        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public GameObject audio2DToggleOn;
        public GameObject audio2DToggleOff;
        public Slider volumeSlider;

        void Start()
        {
            SendCustomEventDelayedFrames("_UpdateLayout", 1);

            if (Utilities.IsValid(AudioManager))
                AudioManager._RegisterControls(this);
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._RegisterControls(this);
        }

        public void _UpdateLayout()
        {
            if (!autoLayout)
                return;

            bool volumePresent = Utilities.IsValid(volumeGroup) && Utilities.IsValid(AudioManager);

            if (enableVolume && volumePresent)
            {
                if (Utilities.IsValid(audio2DControl))
                    audio2DControl.SetActive(enable2DAudioToggle);

                RectTransform volumeRT = volumeSlider.GetComponent<RectTransform>();
                if (Utilities.IsValid(audio2DControl) && enable2DAudioToggle)
                    volumeRT.offsetMax = new Vector2(-25, volumeRT.offsetMax.y);
                else
                    volumeRT.offsetMax = new Vector2(0, volumeRT.offsetMax.y);
            }

            if (Utilities.IsValid(volumeGroup))
                volumeGroup.SetActive(enableVolume);

            bool qualityDepsMet = Utilities.IsValid(staticUrlSource) && staticUrlSource.multipleResolutions;

            if (Utilities.IsValid(resyncGroup))
                resyncGroup.SetActive(enableResync);
            if (Utilities.IsValid(toggleGroup))
                toggleGroup.SetActive(enableQualitySelect && qualityDepsMet);
            if (Utilities.IsValid(messageBarGroup))
                messageBarGroup.SetActive(enableMessageBar);

            UpdateToggleVisual();
        }

        bool inVolumeControllerUpdate = false;

        //public void _VolumeControllerUpdate()
        public void _AudioManagerUpdate()
        {
            if (!Utilities.IsValid(AudioManager))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = AudioManager.masterVolume;
                if (volume != volumeSlider.value)
                    volumeSlider.value = volume;
            }

            UpdateToggleVisual();

            inVolumeControllerUpdate = false;
        }

        public void _Resync()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer.SendCustomEvent("_Resync");
        }

        public void _UrlChanged()
        {
            UpdateToggleVisual();
        }

        public void _SetQuality720()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQuality720();
            UpdateToggleVisual();
        }

        public void _SetQuality1080()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQuality1080();
            UpdateToggleVisual();
        }

        public void _SetQualityAudio()
        {
            if (Utilities.IsValid(staticUrlSource))
                staticUrlSource._SetQualityAudio();
            UpdateToggleVisual();
        }

        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(AudioManager))
                AudioManager._SetMasterMute(!AudioManager.masterMute);
        }

        public void _ToggleAudio2D()
        {
            if (inVolumeControllerUpdate)
                return;

            //if (Utilities.IsValid(AudioManager))
            //    AudioManager._ToggleAudio2D();
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(AudioManager) && Utilities.IsValid(volumeSlider))
                AudioManager._SetMasterVolume(volumeSlider.value);
        }

        void UpdateToggleVisual()
        {
            if (Utilities.IsValid(staticUrlSource))
            {
                bool is720 = staticUrlSource._IsQuality720();
                bool is1080 = staticUrlSource._IsQuality1080();
                bool isAudio = staticUrlSource._IsQualityAudio();
                if (Utilities.IsValid(toggle720On))
                    toggle720On.SetActive(is720);
                if (Utilities.IsValid(toggle720Off))
                    toggle720Off.SetActive(!is720);
                if (Utilities.IsValid(toggle1080On))
                    toggle1080On.SetActive(is1080);
                if (Utilities.IsValid(toggle1080Off))
                    toggle1080Off.SetActive(!is1080);
                if (Utilities.IsValid(toggleAudioOn))
                    toggleAudioOn.SetActive(isAudio);
                if (Utilities.IsValid(toggleAudioOff))
                    toggleAudioOff.SetActive(!isAudio);
            }
            
            if (Utilities.IsValid(AudioManager))
            {
                if (Utilities.IsValid(muteToggleOn) && Utilities.IsValid(muteToggleOff))
                {
                    muteToggleOn.SetActive(AudioManager.masterMute);
                    muteToggleOff.SetActive(!AudioManager.masterMute);
                }
                if (Utilities.IsValid(audio2DToggleOn) && Utilities.IsValid(audio2DToggleOff))
                {
                    //audio2DToggleOn.SetActive(AudioManager.audio2D);
                    //audio2DToggleOff.SetActive(!AudioManager.audio2D);
                }
            }
        }
    }
}
