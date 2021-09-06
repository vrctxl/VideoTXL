
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Override Settings")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideSettings : UdonSharpBehaviour
    {
        public float voiceGain = 15;
        public float voiceNear = 0;
        public float voiceFar = 25;
        public bool voiceLowpass = true;

        [Header("Debug")]
        public DebugLog debugLog;
        public bool vrcLogging = false;

        public void _Apply(VRCPlayerApi player)
        {
            DebugLog($"Setting voice override for {player.displayName} ({player.playerId}): {voiceGain}, {voiceNear}, {voiceFar}, {voiceLowpass}");

            player.SetVoiceGain(voiceGain);
            player.SetVoiceDistanceNear(voiceNear);
            player.SetVoiceDistanceFar(voiceFar);
            player.SetVoiceLowpass(voiceLowpass);
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
