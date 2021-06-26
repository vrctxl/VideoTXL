
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Sync Audio Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncAudioManager : UdonSharpBehaviour
    {
        [Header("Optional Components")]
        public AccessControl accessControl;
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Header("Default Options")]
        public bool locked;

        [UdonSynced]
        bool syncLocked;
        [UdonSynced, NonSerialized]
        public float syncInputVolume;
        [UdonSynced, NonSerialized]
        public bool syncInputMute;
        [UdonSynced, NonSerialized]
        public float syncMasterVolume;
        [UdonSynced, NonSerialized]
        public bool syncMasterMute;
        [UdonSynced, NonSerialized]
        public float[] syncChannelVolumes;
        [UdonSynced, NonSerialized]
        public bool[] syncChannelMutes;

        [NonSerialized]
        public string[] channelNames;

        [NonSerialized]
        public int channelCount = 0;

        bool hasAccessControl = false;

        bool initialized = false;
        GameObject[] audioControls;

        UdonBehaviour listener;

        public void _Initialize(UdonBehaviour localManager, float inputVolume, float masterVolume, float[] channelVolumes, bool inputMute, bool masterMute, bool[] channelMutes, string[] channelNames)
        {
            hasAccessControl = Utilities.IsValid(accessControl);

            listener = localManager;
            channelCount = channelVolumes.Length;

            syncInputVolume = inputVolume;
            syncMasterVolume = masterVolume;
            syncChannelVolumes = channelVolumes;
            syncInputMute = inputMute;
            syncMasterMute = masterMute;
            syncChannelMutes = channelMutes;

            this.channelNames = channelNames;

            if (Networking.IsOwner(gameObject))
                _RequestSerialization();

            initialized = true;
            _UpdateAudioControls();
        }

        public void _RegisterControls(GameObject controls)
        {
            if (!Utilities.IsValid(controls))
                return;

            if (!Utilities.IsValid(audioControls))
                audioControls = new GameObject[0];

            foreach (GameObject c in audioControls)
            {
                if (c == controls)
                    return;
            }

            GameObject[] newControls = new GameObject[audioControls.Length + 1];
            for (int i = 0; i < audioControls.Length; i++)
                newControls[i] = audioControls[i];

            newControls[audioControls.Length] = controls;
            audioControls = newControls;

            if (initialized)
                _UpdateAudioControl(controls);
        }

        public void _SetInputVolume(float value)
        {
            if (value == syncInputVolume || !_TakeControl())
                return;

            syncInputVolume = value;
            _RequestSerialization();
        }

        public void _SetMasterVolume(float value)
        {
            if (value == syncMasterVolume || !_TakeControl())
                return;

            syncMasterVolume = value;
            _RequestSerialization();
        }

        public void _SetChannelVolume(int channel, float value)
        {
            if (channel < 0 || channel >= channelCount || value == syncChannelVolumes[channel])
                return;
            if (!_TakeControl())
                return;

            syncChannelVolumes[channel] = value;
            _RequestSerialization();
        }

        public void _MuteInput(bool state)
        {
            if (state == syncInputMute || !_TakeControl())
                return;

            syncInputMute = state;
            _RequestSerialization();
        }

        public void _MuteMaster(bool state)
        {
            if (state == syncMasterMute || !_TakeControl())
                return;

            syncMasterMute = state;
            _RequestSerialization();
        }

        public void _MuteChannel(int channel, bool state)
        {
            if (channel < 0 || channel >= channelCount || state == syncChannelMutes[channel])
                return;
            if (!_TakeControl())
                return;

            syncChannelMutes[channel] = state;
            _RequestSerialization();
        }

        void _RequestSerialization()
        {
            RequestSerialization();
            _UpdateAll();
        }

        void _UpdateAll()
        {
            _UpdateAudioSources();
            _UpdateAudioControls();
        }

        void _UpdateAudioSources()
        {
            if (!initialized)
                return;

            listener.SendCustomEvent("_SyncUpdate");
        }

        void _UpdateAudioControls()
        {
            foreach (var control in audioControls)
                _UpdateAudioControl(control);
        }

        void _UpdateAudioControl(GameObject control)
        {
            if (!Utilities.IsValid(control) || !initialized)
                return;

            UdonBehaviour script = (UdonBehaviour)control.GetComponent(typeof(UdonBehaviour));
            if (Utilities.IsValid(script))
                script.SendCustomEvent("_AudioManagerUpdate");
        }

        public override void OnDeserialization()
        {
            _UpdateAll();
        }

        bool _IsAdmin()
        {
            if (hasAccessControl)
                return accessControl._LocalHasAccess();

            VRCPlayerApi player = Networking.LocalPlayer;
            return player.isMaster || player.isInstanceOwner;
        }

        bool _CanTakeControl()
        {
            return !syncLocked || _IsAdmin();
        }

        bool _TakeControl()
        {
            if (!_CanTakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }
    }
}
