using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;

namespace Texel
{
    public class VideoComponentUpdater
    {
        public static void UpdateComponents(TXLVideoPlayer videoPlayer)
        {
            if (EditorApplication.isPlaying)
                return;
            if (!videoPlayer)
                return;

            UpdateAudioComponents(videoPlayer);
            UpdateVideoUI(videoPlayer);
            UpdateAudioUI(videoPlayer);

            PrefabUtility.RecordPrefabInstancePropertyModifications(videoPlayer.gameObject);
        }

        public static void UpdateVideoUI(TXLVideoPlayer videoPlayer)
        {
            AudioChannelGroup[] groups = new AudioChannelGroup[0];
            List<AudioManager> managers = GetAudioManagers(videoPlayer);
            if (managers.Count > 0)
                groups = GetAudioGroups(managers[0]);

            List<int> resolutions = new List<int>();
            List<int> latencies = new List<int>();
            List<int> videoModes = new List<int>();
            List<VideoManager> videoManagers = GetVideoManagers(videoPlayer);
            if (videoManagers.Count > 0)
            {
                resolutions = GetResolutions(videoManagers[0]);
                latencies = GetLatencies(videoManagers[0]);
                videoModes = GetVideoTypes(videoManagers[0]);
            }

            OptionsUI[] list = GameObject.FindObjectsOfType<OptionsUI>();
            Debug.Log($"Res count: {resolutions.Count}, latencies count: {latencies.Count}, modes count: {videoModes.Count}");
            foreach (var item in list)
            {
                if (item.videoPlayer != videoPlayer)
                    continue;

                Debug.Log($"Found {item}");
                GameObject row;

                bool hasResOption = resolutions.Count > 1;
                if (!hasResOption)
                {
                    row = item.videoResolutionDropdown.transform.parent.gameObject;
                    Undo.RecordObject(row, "Update Connected Components");
                    row.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(row);
                }
                else
                {
                    Dropdown template = null;
                    Transform templateObj = item.videoResolutionDropdown.transform.Find("IconTemplate");
                    if (templateObj)
                        template = templateObj.GetComponent<Dropdown>();

                    row = item.videoResolutionDropdown.transform.parent.gameObject;
                    Undo.RecordObject(row, "Update Connected Components");

                    row.SetActive(true);
                    item.videoResolutionDropdown.ClearOptions();
                    item.videoResolutionDropdown.AddOptions(GetResolutionOptions(resolutions, template));

                    PrefabUtility.RecordPrefabInstancePropertyModifications(item.videoResolutionDropdown);
                }

                bool hasLatencyOption = latencies.Count > 1;
                row = item.videoLatencyDropdown.transform.parent.gameObject;
                Undo.RecordObject(row, "Update Connected Components");
                row.SetActive(hasLatencyOption);
                PrefabUtility.RecordPrefabInstancePropertyModifications(row);

                bool hasModeOption = videoModes.Count > 1 && !item.localOptionsOnly;
                row = item.videoModeDropdown.transform.parent.gameObject;
                Undo.RecordObject(row, "Update Connected Components");
                row.SetActive(hasModeOption);
                PrefabUtility.RecordPrefabInstancePropertyModifications(row);

                bool hasFitOption = !item.localOptionsOnly;

                bool hasAnyVideoOption = (hasResOption || hasLatencyOption || hasModeOption || hasFitOption) && item.videoPanel;
                row = item.optionsVideoNav.gameObject;
                Undo.RecordObject(row, "Update Connected Components");
                row.SetActive(hasAnyVideoOption);
                PrefabUtility.RecordPrefabInstancePropertyModifications(row);
            }
        }

        public static void UpdateAudioUI(TXLVideoPlayer videoPlayer)
        {
            if (!videoPlayer)
                return;

            AudioChannelGroup[] groups = GetAudioGroups(videoPlayer.audioManager);

            OptionsUI[] list = GameObject.FindObjectsOfType<OptionsUI>();
            foreach (var item in list)
            {
                if (item.videoPlayer != videoPlayer)
                    continue;

                UpdateOptionUIAudio(item, groups);
            }
        }

        static void UpdateOptionUIAudio(OptionsUI ui, AudioChannelGroup[] groups)
        {
            GameObject row;

            bool hasProfileOption = groups.Length > 1;
            if (!hasProfileOption)
            {
                row = ui.audioProfileDropdown.transform.parent.gameObject;
                Undo.RecordObject(row, "Update Connected Components");
                row.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(row);
            }
            else
            {
                Dropdown template = null;
                Transform templateObj = ui.audioProfileDropdown.transform.Find("IconTemplate");
                if (templateObj)
                    template = templateObj.GetComponent<Dropdown>();

                row = ui.audioProfileDropdown.transform.parent.gameObject;
                Undo.RecordObject(row, "Update Connected Components");

                row.SetActive(true);
                ui.audioProfileDropdown.ClearOptions();
                ui.audioProfileDropdown.AddOptions(GetAudioGroupOptions(new List<AudioChannelGroup>(groups)));

                PrefabUtility.RecordPrefabInstancePropertyModifications(ui.audioProfileDropdown);
            }

            bool hasAnyAudioOption = hasProfileOption && ui.audioPanel;
            row = ui.optionsAudioNav.gameObject;
            Undo.RecordObject(row, "Update Connected Components");
            row.SetActive(hasAnyAudioOption);
            PrefabUtility.RecordPrefabInstancePropertyModifications(row);
        }

        public static void UpdateAudioComponents(TXLVideoPlayer videoPlayer)
        {
            if (!videoPlayer)
                return;

            Undo.RecordObject(videoPlayer.gameObject, "Update Audio Setup");

            UpdateUnityAudioSources(videoPlayer);
            UpdateAVProAudioSources(videoPlayer);

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                EditorSceneManager.MarkSceneDirty(stage.scene);
        }

        public static bool ValidateAudioSources(TXLVideoPlayer videoPlayer)
        {
            return ValidateUnityAudioSources(videoPlayer) && ValidateAVProAudioSources(videoPlayer);
        }

        public static bool ValidateUnityAudioSources(TXLVideoPlayer videoPlayer)
        {
            if (!videoPlayer)
                return false;

            AudioChannelGroup[] groups = new AudioChannelGroup[0];
            List<AudioManager> managers = GetAudioManagers(videoPlayer);
            if (managers.Count > 0)
                groups = GetAudioGroups(managers[0]);

            List<AudioChannelGroup> validGroups = new List<AudioChannelGroup>();
            foreach (AudioChannelGroup group in groups)
            {
                if (group && group.unityChannel)
                    validGroups.Add(group);
            }

            // Enumerate video sources
            List<VideoSource> unitySources = new List<VideoSource>();
            List<VideoManager> videoManagers = GetVideoManagers(videoPlayer);
            if (videoManagers.Count > 0)
                unitySources = GetVideoSources(videoManagers[0], VideoSource.VIDEO_SOURCE_UNITY);

            bool valid = true;
            foreach (VideoSource source in unitySources)
            {
                List<Transform> audioResources = new List<Transform>();

                Transform sourceRoot = source.gameObject.transform;
                Transform resources = sourceRoot.Find("AudioResources");
                if (resources)
                {
                    for (int i = 0; i < resources.childCount; i++)
                    {
                        audioResources.Add(resources.GetChild(i));
                    }
                }

                if (validGroups.Count != audioResources.Count)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        static void UpdateUnityAudioSources(TXLVideoPlayer videoPlayer)
        {
            AudioChannelGroup[] groups = new AudioChannelGroup[0];
            List<VideoSource> unitySources = new List<VideoSource>();

            List<AudioManager> managers = GetAudioManagers(videoPlayer);
            if (managers.Count > 0)
                groups = GetAudioGroups(managers[0]);

            // Enumerate video sources
            List<VideoManager> videoManagers = GetVideoManagers(videoPlayer);
            if (videoManagers.Count > 0)
                unitySources = GetVideoSources(videoManagers[0], VideoSource.VIDEO_SOURCE_UNITY);

            // Unity audios sources must be copied as settings
            foreach (VideoSource source in unitySources)
            {
                Transform sourceRoot = source.gameObject.transform;
                Transform resources = sourceRoot.Find("AudioResources");
                if (!resources)
                {
                    resources = new GameObject("AudioResources").transform;
                    resources.parent = sourceRoot;
                }

                List<Transform> existingResources = new List<Transform>();
                for (int i = 0; i < resources.childCount; i++)
                    existingResources.Add(resources.GetChild(i));

                foreach (Transform child in existingResources)
                    GameObject.DestroyImmediate(child.gameObject);

                source.audioGroups = new VideoSourceAudioGroup[groups.Length];

                for (int g = 0; g < groups.Length; g++)
                {
                    AudioChannelGroup group = groups[g];
                    Transform groupRoot = new GameObject(group.groupName).transform;
                    groupRoot.parent = resources;
                    groupRoot.localPosition = Vector3.zero;
                    groupRoot.localRotation = Quaternion.identity;
                    groupRoot.localScale = Vector3.one;

                    VideoSourceAudioGroup audioGroup = groupRoot.gameObject.AddUdonSharpComponent<VideoSourceAudioGroup>();
                    audioGroup.groupName = group.groupName;
                    audioGroup.channelAudio = new AudioSource[1];
                    audioGroup.channelReference = new AudioChannel[1];

                    source.audioGroups[g] = audioGroup;

                    AudioSource unityAudioSource = null;
                    Transform unityAudio = sourceRoot.Find("AudioSource");
                    if (unityAudio)
                    {
                        unityAudioSource = unityAudio.GetComponent<AudioSource>();

                        ConditionalCopyComponent<AudioHighPassFilter>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyComponent<AudioLowPassFilter>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyComponent<AudioDistortionFilter>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyComponent<AudioEchoFilter>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyComponent<AudioChorusFilter>(group.unityChannel.gameObject, unityAudioSource.gameObject);

                        ConditionalCopyConstraint<ParentConstraint>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyConstraint<ScaleConstraint>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyConstraint<PositionConstraint>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                        ConditionalCopyConstraint<RotationConstraint>(group.unityChannel.gameObject, unityAudioSource.gameObject);
                    }

                    audioGroup.channelAudio[0] = unityAudioSource;
                    audioGroup.channelReference[0] = group.unityChannel;
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            }
        }

        public static bool ValidateAVProAudioSources(TXLVideoPlayer videoPlayer)
        {
            AudioChannelGroup[] groups = new AudioChannelGroup[0];
            List<AudioManager> managers = GetAudioManagers(videoPlayer);
            if (managers.Count > 0)
                groups = GetAudioGroups(managers[0]);

            List<AudioChannelGroup> validGroups = new List<AudioChannelGroup>();
            foreach (AudioChannelGroup group in groups)
            {
                if (group)
                    validGroups.Add(group);
            }

            // Enumerate video sources
            List<VideoSource> avproSources = new List<VideoSource>();
            List<VideoManager> videoManagers = GetVideoManagers(videoPlayer);
            if (videoManagers.Count > 0)
                avproSources = GetVideoSources(videoManagers[0], VideoSource.VIDEO_SOURCE_AVPRO);

            bool valid = true;
            foreach (VideoSource source in avproSources)
            {
                List<Transform> audioResources = new List<Transform>();

                Transform sourceRoot = source.gameObject.transform;
                Transform resources = sourceRoot.Find("AudioResources");
                if (resources)
                {
                    for (int i = 0; i < resources.childCount; i++)
                    {
                        audioResources.Add(resources.GetChild(i));
                    }
                }

                if (validGroups.Count != audioResources.Count)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        static void UpdateAVProAudioSources(TXLVideoPlayer videoPlayer)
        {
            AudioChannelGroup[] groups = new AudioChannelGroup[0];
            List<VideoSource> avproSources = new List<VideoSource>();

            List<AudioManager> managers = GetAudioManagers(videoPlayer);
            if (managers.Count > 0)
                groups = GetAudioGroups(managers[0]);

            // Enumerate video sources
            List<VideoManager> videoManagers = GetVideoManagers(videoPlayer);
            if (videoManagers.Count > 0)
                avproSources = GetVideoSources(videoManagers[0], VideoSource.VIDEO_SOURCE_AVPRO);
            
            foreach (VideoSource source in avproSources)
            {
                Transform sourceRoot = source.gameObject.transform;
                Transform template = sourceRoot.Find("AudioTemplates");
                Transform resources = sourceRoot.Find("AudioResources");
                if (!resources)
                {
                    resources = new GameObject("AudioResources").transform;
                    resources.parent = sourceRoot;
                }

                List<Transform> existingResources = new List<Transform>();
                for (int i = 0; i < resources.childCount; i++)
                    existingResources.Add(resources.GetChild(i));

                foreach (Transform child in existingResources)
                    GameObject.DestroyImmediate(child.gameObject);

                source.audioGroups = new VideoSourceAudioGroup[groups.Length];

                for (int g = 0; g < groups.Length; g++)
                {
                    AudioChannelGroup group = groups[g];
                    Transform groupRoot = new GameObject(group.groupName).transform;
                    groupRoot.parent = resources;
                    groupRoot.localPosition = Vector3.zero;
                    groupRoot.localRotation = Quaternion.identity;
                    groupRoot.localScale = Vector3.one;

                    VideoSourceAudioGroup audioGroup = groupRoot.gameObject.AddUdonSharpComponent<VideoSourceAudioGroup>();
                    audioGroup.groupName = group.groupName;
                    audioGroup.channelAudio = new AudioSource[group.avproChannels.Length];
                    audioGroup.channelReference = new AudioChannel[group.avproChannels.Length];

                    source.audioGroups[g] = audioGroup;

                    for (int i = 0; i < group.avproChannels.Length; i++)
                    {
                        AudioChannel channel = group.avproChannels[i];
                        Transform existingChannel = groupRoot.Find(channel.channelName);
                        if (existingChannel)
                            GameObject.DestroyImmediate(existingChannel.gameObject);

                        string templateName = null;
                        switch (channel.track)
                        {
                            case AudioChannelTrack.STEREO: templateName = "StereoMix"; break;
                            case AudioChannelTrack.LEFT: templateName = "MonoLeft"; break;
                            case AudioChannelTrack.RIGHT: templateName = "MonoRight"; break;
                            case AudioChannelTrack.THREE: templateName = "Three"; break;
                            case AudioChannelTrack.FOUR: templateName = "Four"; break;
                            case AudioChannelTrack.FIVE: templateName = "Five"; break;
                            case AudioChannelTrack.SIX: templateName = "Six"; break;
                            case AudioChannelTrack.SEVEN: templateName = "Seven"; break;
                            case AudioChannelTrack.EIGHT: templateName = "Eight"; break;
                        }

                        if (templateName == null)
                            continue;

                        Transform templateChannel = template.Find(templateName);
                        if (!templateChannel)
                            continue;

                        GameObject audioResource = GameObject.Instantiate(templateChannel.gameObject, groupRoot);
                        audioResource.name = channel.channelName;
                        audioResource.SetActive(true);

                        VRCSpatialAudioSource sourceSpatial = channel.gameObject.GetComponent<VRCSpatialAudioSource>();
                        VRCSpatialAudioSource targetSpatial = audioResource.gameObject.GetComponent<VRCSpatialAudioSource>();
                        if (sourceSpatial && targetSpatial)
                        {
                            targetSpatial.Gain = sourceSpatial.Gain;
                            targetSpatial.Far = sourceSpatial.Far;
                            targetSpatial.Near = sourceSpatial.Near;
                            targetSpatial.VolumetricRadius = sourceSpatial.VolumetricRadius;
                            targetSpatial.EnableSpatialization = sourceSpatial.EnableSpatialization;
                            targetSpatial.UseAudioSourceVolumeCurve = sourceSpatial.UseAudioSourceVolumeCurve;
                        }

                        AudioSource sourceAudioSource = channel.gameObject.GetComponent<AudioSource>();
                        AudioSource targetAudioSource = audioResource.gameObject.GetComponent<AudioSource>();
                        if (sourceAudioSource && targetAudioSource)
                        {
                            targetAudioSource.enabled = false;
                            targetAudioSource.volume = sourceAudioSource.volume;
                            targetAudioSource.spatialBlend = sourceAudioSource.spatialBlend;
                            targetAudioSource.spread = sourceAudioSource.spread;
                            targetAudioSource.minDistance = sourceAudioSource.minDistance;
                            targetAudioSource.maxDistance = sourceAudioSource.maxDistance;
                            targetAudioSource.spatialize = sourceAudioSource.spatialize;
                            targetAudioSource.rolloffMode = sourceAudioSource.rolloffMode;
                            targetAudioSource.reverbZoneMix = sourceAudioSource.reverbZoneMix;
                            targetAudioSource.dopplerLevel = sourceAudioSource.dopplerLevel;

                            if (sourceAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend) != null)
                                targetAudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, sourceAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
                            if (sourceAudioSource.GetCustomCurve(AudioSourceCurveType.Spread) != null)
                                targetAudioSource.SetCustomCurve(AudioSourceCurveType.Spread, sourceAudioSource.GetCustomCurve(AudioSourceCurveType.Spread));
                            if (sourceAudioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix) != null)
                                targetAudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, sourceAudioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
                            if (sourceAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff) != null)
                                targetAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, sourceAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));

                            targetAudioSource.transform.SetPositionAndRotation(sourceAudioSource.transform.position, source.transform.rotation);

                            ConditionalCopyConstraint<ParentConstraint>(sourceAudioSource.gameObject, targetAudioSource.gameObject);
                            ConditionalCopyConstraint<ScaleConstraint>(sourceAudioSource.gameObject, targetAudioSource.gameObject);
                            ConditionalCopyConstraint<PositionConstraint>(sourceAudioSource.gameObject, targetAudioSource.gameObject);
                            ConditionalCopyConstraint<RotationConstraint>(sourceAudioSource.gameObject, targetAudioSource.gameObject);
                        }

                        audioGroup.channelAudio[i] = targetAudioSource;
                        audioGroup.channelReference[i] = channel;
                    }
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            }
        }

        static void ConditionalCopyComponent<T>(GameObject source, GameObject dest) where T : Component
        {
            T destComponent = dest.GetComponent<T>();
            if (destComponent)
                GameObject.DestroyImmediate(destComponent);

            T filter = source.GetComponent<T>();
            if (filter)
                CopyComponent(filter, dest);
        }

        static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.GetComponent<T>();
            if (!copy)
                copy = destination.AddComponent(type);

            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        static void ConditionalCopyConstraint<T>(GameObject source, GameObject dest) where T : Component, IConstraint
        {
            T filter = source.GetComponent<T>();
            if (filter && !dest.GetComponent<T>())
                CopyConstraint(filter, dest);
        }

        static T CopyConstraint<T>(T original, GameObject destination) where T : Component, IConstraint
        {
            T derived = CopyComponent(original, destination);
            for (int i = 0; i < original.sourceCount; i++)
            {
                derived.AddSource(original.GetSource(i));
            }
            derived.constraintActive = original.constraintActive;

            return derived;
        }

        public static AudioChannelGroup[] GetAudioGroups(AudioManager manager)
        {
            if (!manager)
                return new AudioChannelGroup[0];

            List<AudioChannelGroup> groups = new List<AudioChannelGroup>();
            int nodeCount = manager.transform.childCount;
            for (int i = 0; i < nodeCount; i++)
            {
                GameObject obj = manager.transform.GetChild(i).gameObject;
                if (!obj.activeSelf)
                    continue;

                AudioChannelGroup group = obj.GetComponent<AudioChannelGroup>();
                if (group)
                    groups.Add(group);
            }

            return groups.ToArray();
        }

        public static List<AudioChannelGroup> GetValidAudioGroups(AudioManager manager)
        {
            AudioChannelGroup[] groups = GetAudioGroups(manager);
            List<AudioChannelGroup> list = new List<AudioChannelGroup>();

            foreach (AudioChannelGroup group in groups)
            {
                if (group)
                    list.Add(group);
            }

            return list;
        }

        static List<int> GetResolutions(VideoManager mux)
        {
            List<int> resolutions = new List<int>();
            if (!mux)
                return resolutions;

            foreach (VideoSource source in mux.sources)
            {
                if (!source)
                    continue;
                if (resolutions.IndexOf(source.maxResolution) < 0)
                    resolutions.Add(source.maxResolution);
            }

            return resolutions;
        }

        static List<int> GetLatencies(VideoManager mux)
        {
            List<int> latencies = new List<int>();
            if (!mux)
                return latencies;

            foreach (VideoSource source in mux.sources)
            {
                if (!source)
                    continue;

                int videoType = GetVideoType(source);
                if (videoType == VideoSource.VIDEO_SOURCE_UNITY)
                    continue;

                int value = source.lowLatency ? VideoSource.LOW_LATENCY_ENABLE : VideoSource.LOW_LATENCY_DISABLE;
                if (latencies.IndexOf(value) < 0)
                    latencies.Add(value);
            }

            return latencies;
        }

        public static List<int> GetVideoTypes(VideoManager mux)
        {
            List<int> types = new List<int>();
            if (!mux)
                return types;

            foreach (VideoSource source in mux.sources)
            {
                if (!source)
                    continue;

                int value = GetVideoType(source);
                if (value >= 0 && types.IndexOf(value) < 0)
                    types.Add(value);
            }

            return types;
        }

        public static int GetVideoType(VideoSource source)
        {
            int value = -1;
            VRCAVProVideoPlayer avp = (VRCAVProVideoPlayer)source.gameObject.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            if (avp)
                value = VideoSource.VIDEO_SOURCE_AVPRO;

            VRCUnityVideoPlayer unity = (VRCUnityVideoPlayer)source.gameObject.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (unity)
                value = VideoSource.VIDEO_SOURCE_UNITY;

            return value;
        }

        public static List<VideoManager> GetVideoManagers(TXLVideoPlayer videoPlayer)
        {
            List<VideoManager> list = new List<VideoManager>();
            if (!videoPlayer)
                return list;

            VideoManager[] managers = Object.FindObjectsOfType<VideoManager>();
            foreach (VideoManager manager in managers)
            {
                if (manager.videoPlayer == videoPlayer)
                    list.Add(manager);
            }

            return list;
        }

        public static List<AudioManager> GetAudioManagers(TXLVideoPlayer videoPlayer)
        {
            List<AudioManager> list = new List<AudioManager>();
            if (!videoPlayer)
                return list;

            AudioManager[] managers = Object.FindObjectsOfType<AudioManager>();
            foreach (AudioManager manager in managers)
            {
                if (manager.videoPlayer == videoPlayer)
                    list.Add(manager);
            }

            return list;
        }

        public static List<VideoSource> GetVideoSources(VideoManager manager, int videoType)
        {
            if (!manager)
                return new List<VideoSource>();

            List<VideoSource> sources = new List<VideoSource>();
            int nodeCount = manager.transform.childCount;
            for (int i = 0; i < nodeCount; i++)
            {
                GameObject obj = manager.transform.GetChild(i).gameObject;
                if (!obj.activeSelf)
                    continue;

                VideoSource source = obj.GetComponent<VideoSource>();
                if (source)
                    sources.Add(source);
            }

            List<VideoSource> list = new List<VideoSource>();
            foreach (VideoSource source in sources)
            {
                if (!source)
                    continue;

                int type = GetVideoType(source);
                if (videoType == -1 || type == videoType)
                    list.Add(source);
            }

            return list;
        }

        static List<Dropdown.OptionData> GetResolutionOptions(List<int> resolutions, Dropdown iconTemplate)
        {
            List<int> sorted = new List<int>(resolutions);
            sorted.Sort();
            sorted.Reverse();

            Sprite iconLow = null;
            Sprite iconMid = null;
            Sprite iconHigh = null;
            if (iconTemplate)
            {
                foreach (var entry in iconTemplate.options)
                {
                    if (entry.text == "low")
                        iconLow = entry.image;
                    else if (entry.text == "mid")
                        iconMid = entry.image;
                    else if (entry.text == "high")
                        iconHigh = entry.image;
                }
            }

            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (int res in sorted)
            {
                Sprite icon = iconHigh;
                if (res < 1080)
                    icon = iconMid;
                if (res < 480)
                    icon = iconLow;

                options.Add(new Dropdown.OptionData($"{res}p", icon));
            }

            return options;
        }

        static List<Dropdown.OptionData> GetAudioGroupOptions(List<AudioChannelGroup> groups)
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (AudioChannelGroup group in groups)
            {
                if (group)
                    options.Add(new Dropdown.OptionData(group.groupName, group.groupIcon));
            }

            return options;
        }
    }
}
