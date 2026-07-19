
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum GamePlatform
    {
        None = -1,
        PC = 0,
        Quest,
        IOS,
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
        public bool[] customRule;

        [Obsolete("Use applyX fields")]
        public GamePlatform[] platforms;
        public VideoSourceBackend[] sourceTypes;
        public VideoSourceLatency[] sourceLatencies;
        public int[] sourceResolutions;
        public string[] audioProfiles;
        public BasicTest[] customTests;

        public bool[] applyPC;
        public bool[] applyQuest;
        public bool[] applyIOS;

        GamePlatform platform;
        VideoSource videoSource;
        AudioChannelGroup audioProfile;

        private DataDictionary ruleLookup;

        private void Start()
        {
            _Init();
        }

        protected virtual void _Init()
        {
            ruleLookup = new DataDictionary();

            int count = referenceUrls.Length;
            applyPC = (bool[])UtilityTxl.ArrayMinSize(applyPC, count, typeof(bool));
            applyQuest = (bool[])UtilityTxl.ArrayMinSize(applyQuest, count, typeof(bool));
            applyIOS = (bool[])UtilityTxl.ArrayMinSize(applyIOS, count, typeof(bool));

            for (int i = 0; i < referenceUrls.Length; i++)
            {
                VRCUrl url = referenceUrls[i];
                if (url == null)
                    continue;

                string urlstr = url.ToString();
                if (urlstr == "")
                    continue;

                // Upgrade platform setting
                if (platformRule[i] && !applyPC[i] && !applyQuest[i] && !applyIOS[i])
                {
                    if (platform == GamePlatform.PC)
                        applyPC[i] = true;
                    else if (platform == GamePlatform.Quest)
                        applyQuest[i] = true;
                    else if (platform == GamePlatform.IOS)
                        applyIOS[i] = true;
                }

                if (ruleLookup.TryGetValue(urlstr, TokenType.DataList, out var dataListToken))
                {
                    DataList indexList = dataListToken.DataList;
                    indexList.Add(i);
                }
                else
                {
                    DataList indexList = new DataList();
                    indexList.Add(i);
                    ruleLookup.Add(urlstr, indexList);
                }
            }
        }

        public virtual void _SetPlatform(GamePlatform platform)
        {
            this.platform = platform;
        }

        public virtual void _SetVideoSource(VideoSource source)
        {
            videoSource = source;
        }

        public virtual void _SetAudioProfile(AudioChannelGroup group)
        {
            audioProfile = group;
        }

        public virtual bool _ValidRemapped(VRCUrl input, VRCUrl matchup)
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

        public virtual VRCUrl _Remap(VRCUrl input)
        {
            if (!Utilities.IsValid(input))
                return input;

            bool videoSourceValid = videoSource != null;

            string inputStr = input.Get();
            if (ruleLookup.TryGetValue(inputStr, TokenType.DataList, out var dataListToken))
            {
                DataList indexList = dataListToken.DataList;
                for (int index = 0; index < indexList.Count; index++)
                {
                    int i = indexList[index].Int;
                    VRCUrl reffed = referenceUrls[i];
                    if (!Utilities.IsValid(reffed) || inputStr != reffed.Get())
                        continue;

                    if (platformRule[i])
                    {
                        if (platform == GamePlatform.PC && !applyPC[i])
                            continue;
                        if (platform == GamePlatform.Quest && !applyQuest[i])
                            continue;
                        if (platform == GamePlatform.IOS && !applyIOS[i])
                            continue;
                    }

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

                    if (customRule[i] && customTests[i] && !customTests[i]._Test())
                        continue;

                    if (remappedUrls.Length <= i || !Utilities.IsValid(remappedUrls[i]))
                        continue;

                    VRCUrl mapped = remappedUrls[i];
                    if (mapped.Get() == "")
                        continue;

                    return remappedUrls[i];
                }
            }

            return input;
        }
    }
}
