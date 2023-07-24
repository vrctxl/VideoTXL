using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Override Settings")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideSettings : UdonSharpBehaviour
    {
        public bool applyVoice = true;
        public float voiceGain = 15;
        public float voiceNear = 0;
        public float voiceFar = 25;
        public float voiceVolumetric = 0;
        public bool voiceLowpass = true;

        public bool applyAvatar = true;
        public float avatarGain = 10;
        public float avatarNear = 0;
        public float avatarFar = 40;
        public float avatarVolumetric = 0;

        public DebugLog debugLog;
        public bool vrcLogging = false;

        public void _Apply(VRCPlayerApi player)
        {
            if (applyVoice)
            {
                DebugLog($"Setting voice override for {player.displayName} ({player.playerId}): {voiceGain}, {voiceNear}, {voiceFar}, {voiceLowpass}");

                player.SetVoiceGain(voiceGain);
                player.SetVoiceDistanceNear(voiceNear);
                player.SetVoiceDistanceFar(voiceFar);
                player.SetVoiceVolumetricRadius(voiceVolumetric);
                player.SetVoiceLowpass(voiceLowpass);
            }

            if (applyAvatar)
            {
                DebugLog($"Setting avatar override for {player.displayName} ({player.playerId}): {avatarGain}, {avatarNear}, {avatarFar}");
                player.SetAvatarAudioGain(avatarGain);
                player.SetAvatarAudioNearRadius(avatarNear);
                player.SetAvatarAudioFarRadius(avatarFar);
                player.SetAvatarAudioVolumetricRadius(avatarVolumetric);
            }
        }

        void DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[Texel:AudioOverride] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AudioOverride", message);
        }
    }
}
