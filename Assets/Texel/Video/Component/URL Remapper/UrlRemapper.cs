
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum GamePlatform {
        PC,
        Quest,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlRemapper : UdonSharpBehaviour
    {
        public VRCUrl[] referenceUrls;
        public VRCUrl[] remappedUrls;
        public bool[] platformRule;
        public bool[] sourceTypeRule;
        public bool[] latencyRule;
        public bool[] resolutionRule;
        public bool[] audioProfileRule;

        public GamePlatform[] platforms;
        public VideoSourceBackend[] sourceTypes;
        public VideoSourceLatency[] sourceLatencies;
        public int[] sourceResolutions;
        public string[] audioProfiles;


        public bool[] applyPC;
        public bool[] applyQuest;


        int gameMode = GAME_MODE_PC;
        GamePlatform platform;
        VideoSource videoSource;
        AudioChannelGroup audioProfile;

        public const int GAME_MODE_PC = 0;
        public const int GAME_MODE_QUEST = 1;

        public void _SetGameMode(int mode)
        {
            gameMode = mode;
        }

        public void _SetPlatform(GamePlatform platform)
        {
            this.platform = platform;
        }

        public void _SetVideoSource(VideoSource source)
        {
            videoSource = source;
        }

        public void _SetAudioProfile(AudioChannelGroup group)
        {
            audioProfile = group;
        }

        public bool _ValidRemapped(VRCUrl input, VRCUrl matchup)
        {
            if (input == null || input == VRCUrl.Empty || input.Get() == "")
                return false;

            if (matchup == null || matchup == VRCUrl.Empty || matchup.Get() == "")
                return false;

            VRCUrl mapped = _Remap(input);
            if (mapped == null || mapped == VRCUrl.Empty || mapped.Get() == "")
                return false;

            return matchup.Get() != mapped.Get();
        }

        public VRCUrl _Remap(VRCUrl input)
        {
            if (!Utilities.IsValid(input))
                return input;

            bool videoSourceValid = videoSource != null;

            string inputStr = input.Get();
            for (int i = 0; i < referenceUrls.Length; i++)
            {
                VRCUrl reffed = referenceUrls[i];
                if (!Utilities.IsValid(reffed) || inputStr != reffed.Get())
                    continue;

                if (platformRule[i] && platform != platforms[i])
                    continue;

                if (videoSource != null)
                {
                    if (sourceTypeRule[i] && videoSource.VideoSourceType != (int)sourceTypes[i])
                        continue;
                    if (latencyRule[i] && (videoSource.lowLatency ? VideoSourceLatency.LowLatency : VideoSourceLatency.Standard) != sourceLatencies[i])
                        continue;
                    if (resolutionRule[i] && videoSource.maxResolution != sourceResolutions[i])
                        continue;
                }

                if (audioProfile != null)
                {
                    if (audioProfileRule[i] && audioProfile.groupName != audioProfiles[i])
                        continue;
                }

                if (remappedUrls.Length <= i || !Utilities.IsValid(remappedUrls[i]))
                    continue;

                VRCUrl mapped = remappedUrls[i];
                if (mapped.Get() == "")
                    continue;

                return remappedUrls[i];
            }

            return input;
        }
    }
}
