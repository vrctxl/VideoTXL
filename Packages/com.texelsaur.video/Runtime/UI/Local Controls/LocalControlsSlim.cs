using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalControlsSlim : UdonSharpBehaviour
    {
        public TXLVideoPlayer videoPlayer;
        public AudioManager audioManager;
        public ControlColorProfile colorProfile;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public Slider volumeSlider;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        void Start()
        {
            _PopulateMissingReferences();

            if (Utilities.IsValid(colorProfile))
            {
                normalColor = colorProfile.normalColor;
                disabledColor = colorProfile.disabledColor;
                activeColor = colorProfile.activeColor;
                attentionColor = colorProfile.attentionColor;
            }

            if (Utilities.IsValid(audioManager))
                audioManager._RegisterControls(this);
        }

        bool inVolumeControllerUpdate = false;

        public void _AudioManagerUpdate()
        {
            if (!Utilities.IsValid(audioManager))
                return;

            inVolumeControllerUpdate = true;

            if (Utilities.IsValid(volumeSlider))
            {
                float volume = audioManager.masterVolume;
                if (volume != volumeSlider.value)
                    volumeSlider.value = volume;
            }

            UpdateToggleVisual();

            inVolumeControllerUpdate = false;
        }

        public void _Resync()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._Resync();
        }

        public void _ToggleVolumeMute()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager))
                audioManager._SetMasterMute(!audioManager.masterMute);
        }

        public void _UpdateVolumeSlider()
        {
            if (inVolumeControllerUpdate)
                return;

            if (Utilities.IsValid(audioManager) && Utilities.IsValid(volumeSlider))
                audioManager._SetMasterVolume(volumeSlider.value);
        }

        void UpdateToggleVisual()
        {
            if (Utilities.IsValid(audioManager))
            {
                if (Utilities.IsValid(muteToggleOn) && Utilities.IsValid(muteToggleOff))
                {
                    muteToggleOn.SetActive(audioManager.masterMute);
                    muteToggleOff.SetActive(!audioManager.masterMute);
                }
            }
        }

        void _PopulateMissingReferences()
        {
            if (!Utilities.IsValid(volumeSlider))
                volumeSlider = (Slider)_FindComponent("ControlArea/VolumeGroup/Slider", typeof(Slider));
            if (!Utilities.IsValid(muteToggleOn))
                muteToggleOn = _FindGameObject("ControlArea/VolumeGroup/MuteButton/IconMuted");
            if (!Utilities.IsValid(muteToggleOff))
                muteToggleOff = _FindGameObject("ControlArea/VolumeGroup/MuteButton/IconVolume");
        }

        GameObject _FindGameObject(string path)
        {
            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.gameObject;
        }

        Component _FindComponent(string path, System.Type type)
        {
            Transform t = transform.Find(path);
            if (!Utilities.IsValid(t))
                return null;

            return t.GetComponent(type);
        }
    }
}
