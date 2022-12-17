using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.SceneManagement;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Components;

namespace Texel
{
    [CustomEditor(typeof(SyncPlayer))]
    internal class SyncPlayerInspector : Editor
    {
        SerializedProperty videoMuxProperty;
        SerializedProperty audioManagerProperty;

        SerializedProperty playlistPoperty;
        SerializedProperty remapperProperty;
        SerializedProperty accessControlProperty;
        SerializedProperty debugLogProperty;
        SerializedProperty debugStageProperty;
        SerializedProperty playbackZoneProperty;

        SerializedProperty defaultUrlProperty;
        SerializedProperty defaultLockedProperty;
        SerializedProperty debugLoggingProperty;
        SerializedProperty loopProperty;
        SerializedProperty retryOnErrorProperty;
        SerializedProperty autoFailbackAVProProperty;

        SerializedProperty syncFrequencyProperty;
        SerializedProperty syncThresholdProperty;
        SerializedProperty autoAVSyncProperty;

        SerializedProperty defaultVideoModeProperty;
        //SerializedProperty useUnityVideoProperty;
        //SerializedProperty useAVProProperty;
        //SerializedProperty unityVideoProperty;
        //SerializedProperty avProVideoProperty;

        private void OnEnable()
        {
            videoMuxProperty = serializedObject.FindProperty(nameof(SyncPlayer.videoMux));
            audioManagerProperty = serializedObject.FindProperty(nameof(SyncPlayer.audioManager));

            playlistPoperty = serializedObject.FindProperty(nameof(SyncPlayer.playlist));
            remapperProperty = serializedObject.FindProperty(nameof(SyncPlayer.urlRemapper));
            accessControlProperty = serializedObject.FindProperty(nameof(SyncPlayer.accessControl));
            debugLogProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLog));
            debugStageProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugState));
            playbackZoneProperty = serializedObject.FindProperty(nameof(SyncPlayer.playbackZoneMembership));

            defaultUrlProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultUrl));
            defaultLockedProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultLocked));
            debugLoggingProperty = serializedObject.FindProperty(nameof(SyncPlayer.debugLogging));
            loopProperty = serializedObject.FindProperty(nameof(SyncPlayer.loop));
            retryOnErrorProperty = serializedObject.FindProperty(nameof(SyncPlayer.retryOnError));
            autoFailbackAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoFailbackToAVPro));

            syncFrequencyProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncFrequency));
            syncThresholdProperty = serializedObject.FindProperty(nameof(SyncPlayer.syncThreshold));
            autoAVSyncProperty = serializedObject.FindProperty(nameof(SyncPlayer.autoInternalAVSync));

            defaultVideoModeProperty = serializedObject.FindProperty(nameof(SyncPlayer.defaultVideoSource));
            //useUnityVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.useUnityVideo));
            //useAVProProperty = serializedObject.FindProperty(nameof(SyncPlayer.useAVPro));
            //unityVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.unityVideo));
            //avProVideoProperty = serializedObject.FindProperty(nameof(SyncPlayer.avProVideo));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(videoMuxProperty, new GUIContent("Video Source Manager", "Internal object for multiplexing multiple video sources."));
            EditorGUILayout.PropertyField(audioManagerProperty, new GUIContent("Audio Source Manager", "Internal object for managing multiple audio source sets against video sources."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playlistPoperty, new GUIContent("Playlist", "Pre-populated playlist to iterate through.  If default URL is set, the playlist will be disabled by default, otherwise it will auto-play."));
            EditorGUILayout.PropertyField(remapperProperty, new GUIContent("URL Remapper", "Set of input URLs to remap to alternate URLs on a per-platform basis."));
            EditorGUILayout.PropertyField(accessControlProperty, new GUIContent("Access Control", "Control access to player controls based on player type or whitelist."));
            
            EditorGUILayout.PropertyField(playbackZoneProperty, new GUIContent("Playback Zone Membership", "Optional zone membership object tied to a trigger zone the player must be in to sustain playback.  Disables playing audio on world load."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultUrlProperty, new GUIContent("Default URL", "Optional default URL to play on world load."));
            EditorGUILayout.PropertyField(defaultLockedProperty, new GUIContent("Default Locked", "Whether player controls are locked to master and instance owner by default."));
            EditorGUILayout.PropertyField(loopProperty, new GUIContent("Loop", "Automatically loop track when finished."));
            EditorGUILayout.PropertyField(retryOnErrorProperty, new GUIContent("Retry on Error", "Whether to keep playing the same URL if an error occurs."));
            EditorGUILayout.PropertyField(autoFailbackAVProProperty, new GUIContent("Auto Failover to AVPro", "If AVPro component is available and enabled, automatically fail back to AVPro when auto mode failed under certain conditions to play in video mode."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(syncFrequencyProperty, new GUIContent("Sync Frequency", "How often to check if video playback has fallen out of sync."));
            EditorGUILayout.PropertyField(syncThresholdProperty, new GUIContent("Sync Threshold", "How far video playback must have fallen out of sync to perform a correction."));
            EditorGUILayout.PropertyField(autoAVSyncProperty, new GUIContent("Auto Internal AV Sync", "Experimental.  Video playback will periodically resync audio and video.  May cause stuttering or temporary playback failure."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Video Sources", EditorStyles.boldLabel);

            /*EditorGUILayout.PropertyField(useAVProProperty, new GUIContent("Use AVPro", "Whether AVPro is a supported video source for this player.  Disabling this component also disables source selection."));
            if (useAVProProperty.boolValue)
                EditorGUILayout.PropertyField(avProVideoProperty, new GUIContent("AVPro Video Source", "AVPro video player component"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useUnityVideoProperty, new GUIContent("Use Unity Video", "Whether Unity video is a supported video source for this player.  Disabling this component also disables source selection."));
            if (useUnityVideoProperty.boolValue)
                EditorGUILayout.PropertyField(unityVideoProperty, new GUIContent("Unity Video Source", "Unity video player component"));
            */

            //if (useAVProProperty.boolValue && useUnityVideoProperty.boolValue)
            //{
                EditorGUILayout.Space();
                GUIContent desc = new GUIContent("Default Video Source", "The video source that should be active by default, or auto to let the player determine on a per-URL basis.");
                defaultVideoModeProperty.intValue = EditorGUILayout.Popup(desc, defaultVideoModeProperty.intValue, new string[] { "Auto", "AVPro", "Unity Video" });
            //}

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
            EditorGUILayout.PropertyField(debugStageProperty, new GUIContent("Debug State", "Periodically refresh internal object state on a world object"));
            EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            if (GUILayout.Button("Update Connected Components", GUILayout.Width(EditorGUIUtility.labelWidth)))
                UpdateComponents();
        }

        void UpdateComponents()
        {
            if (EditorApplication.isPlaying)
                return;

            SyncPlayer videoPlayer = (SyncPlayer)serializedObject.targetObject;
            if (!videoPlayer)
                return;

            List<int> resolutions = GetResolutions(videoPlayer.videoMux);
            List<int> latencies = GetLatencies(videoPlayer.videoMux);
            List<int> videoModes = GetTypes(videoPlayer.videoMux);

            OptionsUI[] list = Object.FindObjectsOfType<OptionsUI>();
            foreach (var item in list)
            {
                if (!item.mainControls || item.mainControls.videoPlayer != serializedObject.targetObject)
                    continue;

                Debug.Log($"Found {item}");

                if (resolutions.Count <= 1)
                {
                    GameObject row = item.videoResolutionDropdown.transform.parent.gameObject;
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

                    Undo.RecordObject(item.videoResolutionDropdown, "Update Connected Components");

                    item.videoResolutionDropdown.ClearOptions();
                    item.videoResolutionDropdown.AddOptions(GetResolutionOptions(resolutions, template));

                    PrefabUtility.RecordPrefabInstancePropertyModifications(item.videoResolutionDropdown);
                }

                if (latencies.Count <= 1)
                {
                    GameObject row = item.videoLatencyDropdown.transform.parent.gameObject;
                    Undo.RecordObject(row, "Update Connected Components");
                    row.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(row);
                }

                if (videoModes.Count <= 1)
                {
                    GameObject row = item.videoModeDropdown.transform.parent.gameObject;
                    Undo.RecordObject(row, "Update Connected Components");
                    row.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(row);
                }
            }

            Undo.RecordObject(videoPlayer.gameObject, "Update Audio Setup");

            // Enumerate video sources
            List<VideoSource> unitySources = new List<VideoSource>();
            List<VideoSource> avproSources = new List<VideoSource>();

            foreach (VideoSource source in videoPlayer.videoMux.sources)
            {
                if (!source)
                    continue;

                if (source.gameObject.GetComponent<VRCAVProVideoPlayer>())
                    avproSources.Add(source);
                else if (source.gameObject.GetComponent<VRCUnityVideoPlayer>())
                    unitySources.Add(source);
            }

            // Unity audios sources must be copied as settings

            // Apply Audio
            AudioChannelGroup[] groups = videoPlayer.audioManager.channelGroups;

            foreach (VideoSource source in avproSources)
            {
                Transform sourceRoot = source.gameObject.transform;
                Transform template = sourceRoot.Find("AudioTemplates");
                Transform resources = sourceRoot.Find("AudioResources");
                if (!resources)
                {
                    resources = Instantiate(new GameObject("AudioResources"), sourceRoot).transform;
                    resources.name = "AudioResources";
                }

                source.audioGroups = new VideoSourceAudioGroup[groups.Length];

                for (int g = 0; g < groups.Length; g++)
                {
                    AudioChannelGroup group = groups[g];
                    Transform groupRoot = resources.Find(group.groupName);
                    if (groupRoot)
                        DestroyImmediate(groupRoot.gameObject);

                    groupRoot = Instantiate(new GameObject(), resources).transform;
                    groupRoot.name = group.groupName;

                    VideoSourceAudioGroup audioGroup = groupRoot.gameObject.AddUdonSharpComponent<VideoSourceAudioGroup>();
                    audioGroup.groupName = group.groupName;
                    audioGroup.channelAudio = new AudioSource[group.avproChannels.Length];
                    audioGroup.channelName = new string[group.avproChannels.Length];
                    audioGroup.channelReference = new AudioChannel[group.avproChannels.Length];

                    source.audioGroups[g] = audioGroup;

                    for (int i = 0; i < group.avproChannels.Length; i++)
                    {
                        AudioChannel channel = group.avproChannels[i];
                        Transform existingChannel = groupRoot.Find(channel.channelName);
                        if (existingChannel)
                            DestroyImmediate(existingChannel.gameObject);

                        string templateName = null;
                        switch (channel.track)
                        {
                            case AudioChannelTrack.STEREO: templateName = "StereoMix"; break;
                            case AudioChannelTrack.LEFT: templateName = "MonoLeft"; break;
                            case AudioChannelTrack.RIGHT: templateName = "MonoRight"; break;
                            case AudioChannelTrack.THREE: templateName = "Three"; break;
                            case AudioChannelTrack.FOUR: templateName = "Four"; break;
                            case AudioChannelTrack.FIVE: templateName = "Five"; break;
                        }

                        if (templateName == null)
                            continue;

                        Transform templateChannel = template.Find(templateName);
                        if (!templateChannel)
                            continue;

                        GameObject audioResource = Instantiate(templateChannel.gameObject, groupRoot);
                        audioResource.name = channel.channelName;

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
                        }

                        audioGroup.channelName[i] = channel.name;
                        audioGroup.channelAudio[i] = targetAudioSource;
                        audioGroup.channelReference[i] = channel;
                    }
                }
                
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(videoPlayer.gameObject);
        }

        List<int> GetResolutions(VideoMux mux)
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

        List<int> GetLatencies(VideoMux mux)
        {
            List<int> latencies = new List<int>();
            if (!mux)
                return latencies;

            foreach (VideoSource source in mux.sources)
            {
                if (!source)
                    continue;
                int value = source.lowLatency ? VideoSource.LOW_LATENCY_ENABLE : VideoSource.LOW_LATENCY_DISABLE;
                if (latencies.IndexOf(value) < 0)
                    latencies.Add(value);
            }

            return latencies;
        }

        List<int> GetTypes(VideoMux mux)
        {
            List<int> types = new List<int>();
            if (!mux)
                return types;

            foreach (VideoSource source in mux.sources)
            {
                if (!source)
                    continue;

                int value = -1;
                if (source.gameObject.GetComponent<VRCAVProVideoPlayer>())
                    value = VideoSource.VIDEO_SOURCE_AVPRO;
                else if (source.gameObject.GetComponent<VRCUnityVideoPlayer>())
                    value = VideoSource.VIDEO_SOURCE_UNITY;

                if (value >= 0 && types.IndexOf(value) < 0)
                    types.Add(value);
            }

            return types;
        }

        List<Dropdown.OptionData> GetResolutionOptions(List<int> resolutions, Dropdown iconTemplate)
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
    }
}
