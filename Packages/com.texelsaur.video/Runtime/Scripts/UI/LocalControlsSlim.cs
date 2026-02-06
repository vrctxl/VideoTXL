using System;
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
        [Obsolete("AudioManager will be taken from the bound video player instead")]
        public AudioManager audioManager;
        public ControlColorProfile colorProfile;

        public GameObject muteToggleOn;
        public GameObject muteToggleOff;
        public Slider volumeSlider;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        bool initialized = false;

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

            if (gameObject.activeInHierarchy)
                _RegisterVideoListeners();

            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            _RegisterVideoListeners();
        }

        private void OnDisable()
        {
            _UnregisterVideoListeners();
        }

        public void _BindVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            _UnregisterVideoListeners();

            this.videoPlayer = videoPlayer;

            if (gameObject.activeInHierarchy)
                _RegisterVideoListeners();
        }

        void _RegisterVideoListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_BIND_AUDIOMANAGER, this, nameof(_InternalOnBindAudioManager));
                videoPlayer._Register(TXLVideoPlayer.EVENT_UNBIND_AUDIOMANAGER, this, nameof(_InternalOnUnbindAudioManager));

                _RegisterAudioManagerListeners();
            }
        }

        void _UnregisterVideoListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_BIND_AUDIOMANAGER, this, nameof(_InternalOnBindAudioManager));
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_UNBIND_AUDIOMANAGER, this, nameof(_InternalOnUnbindAudioManager));

                _UnregisterAudioManagerListeners();
            }
        }

        public void _InternalOnUnbindAudioManager()
        {
            _UnregisterAudioManagerListeners();
        }

        public void _InternalOnBindAudioManager()
        {
            if (gameObject.activeInHierarchy)
                _RegisterAudioManagerListeners();
        }

        void _RegisterAudioManagerListeners()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager)
            {
                manager._Register(AudioManager.EVENT_MASTER_VOLUME_UPDATE, this, nameof(_InternalOnMasterVolumeUpdate));
                manager._Register(AudioManager.EVENT_MASTER_MUTE_UPDATE, this, nameof(_InternalOnMasterMuteUpdate));

                _InternalOnMasterVolumeUpdate();
                _InternalOnMasterMuteUpdate();
            }
        }

        void _UnregisterAudioManagerListeners()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager)
            {
                manager._Unregister(AudioManager.EVENT_MASTER_VOLUME_UPDATE, this, nameof(_InternalOnMasterVolumeUpdate));
                manager._Unregister(AudioManager.EVENT_MASTER_MUTE_UPDATE, this, nameof(_InternalOnMasterMuteUpdate));
            }
        }

        public void _InternalOnMasterVolumeUpdate()
        {
            if (volumeSlider)
                volumeSlider.SetValueWithoutNotify(videoPlayer.AudioManager.masterVolume);
        }

        public void _InternalOnMasterMuteUpdate()
        {
            AudioManager manager = videoPlayer.AudioManager;
            _SetActive(muteToggleOn, manager.masterMute);
            _SetActive(muteToggleOff, !manager.masterMute);
        }

        public void _Resync()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._Resync();
        }

        public void _ToggleVolumeMute()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager)
                manager._SetMasterMute(!manager.masterMute);
        }

        public void _UpdateVolumeSlider()
        {
            if (!videoPlayer)
                return;

            AudioManager manager = videoPlayer.AudioManager;
            if (manager && volumeSlider)
                manager._SetMasterVolume(volumeSlider.value);
        }

        void _SetActive(GameObject obj, bool active)
        {
            if (obj)
                obj.SetActive(active);
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
