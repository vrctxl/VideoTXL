
using System;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;

public class VideoMux : EventBase
{
    public VideoSource[] sources;

    [Header("Debug")]
    public bool debugLogging = true;
    public DebugLog debugLog;

    const int VIDEO_READY_EVENT = 0;
    const int VIDEO_START_EVENT = 1;
    const int VIDEO_END_EVENT = 2;
    const int VIDEO_ERROR_EVENT = 3;
    const int VIDEO_LOOP_EVENT = 4;
    const int VIDEO_PAUSE_EVENT = 5;
    const int VIDEO_PLAY_EVENT = 6;
    const int SOURCE_CHANGE_EVENT = 7;
    const int EVENT_COUNT = 8;

    public VideoError LastError { get; private set; }

    int activeSource;
    int prevSource;
    int nextSourceIndex = 0;

    int videoType = VideoSource.VIDEO_SOURCE_NONE;
    int lowLatency = VideoSource.LOW_LATENCY_UNKNOWN;
    int preferredResIndex = 0;
    bool avSyncEnabled = false;

    int[] supportedResolutions;
    int[] sourceResMap;

    VideoSource[] unitySources;
    VideoSource[] avproSources;

    BaseVRCVideoPlayer activeVideoPlayer;

    void Start()
    {
        _EnsureInit();
    }

    protected override void _Init()
    {
        _InitHandlers(EVENT_COUNT);

        if (sources == null)
            sources = new VideoSource[0];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                sources[i]._Register(this, i);
        }

        _Discover();

        nextSourceIndex = sources.Length;
        activeSource = 0;
    }

    void _Discover()
    {
        int[] foundResolutions = new int[sources.Length];
        int resCount = 0;

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null)
                continue;

            int maxRes = sources[i].maxResolution;
            if (Array.IndexOf(foundResolutions, maxRes) >= 0)
                continue;

            foundResolutions[resCount] = maxRes;
            resCount++;
        }

        supportedResolutions = new int[resCount];
        Array.Copy(foundResolutions, supportedResolutions, resCount);
        UtilityTxl.ArraySort(supportedResolutions);
        Array.Reverse(supportedResolutions);

        unitySources = new VideoSource[resCount];
        avproSources = new VideoSource[resCount * 2];

        sourceResMap = new int[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            VideoSource source = sources[i];
            if (source == null)
                continue;

            int resIndex = Array.IndexOf(supportedResolutions, source.maxResolution);
            sourceResMap[i] = resIndex;

            switch (source.VideoSourceType)
            {
                case VideoSource.VIDEO_SOURCE_UNITY:
                    unitySources[resIndex] = source;
                    break;
                case VideoSource.VIDEO_SOURCE_AVPRO:
                    avproSources[resIndex + (source.lowLatency ? resCount : 0)] = source;
                    break;
            }
        }
    }

    VideoSource _FindBestSource(int videoType, int resIndex, int lowLatency)
    {
        switch (videoType)
        {
            case VideoSource.VIDEO_SOURCE_UNITY: return _FindBestUnitySource(resIndex);
            case VideoSource.VIDEO_SOURCE_AVPRO: return _FindBestAVProSource(resIndex, lowLatency);
            default: return null;
        }
    }

    VideoSource _FindBestUnitySource(int resIndex)
    {
        if (resIndex < 0 || resIndex >= supportedResolutions.Length)
            return null;

        VideoSource source = unitySources[resIndex];
        if (source != null)
            return source;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            source = unitySources[i];
            if (source != null)
                break;
        }

        return source;
    }

    VideoSource _FindBestAVProSource(int resIndex, int lowLatency)
    {
        if (resIndex < 0 || resIndex >= supportedResolutions.Length)
            return null;

        VideoSource source = null;
        if (lowLatency == VideoSource.LOW_LATENCY_DISABLE)
            source = avproSources[resIndex];
        if (source == null)
            source = avproSources[resIndex + supportedResolutions.Length];

        if (source != null)
            return source;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            if (lowLatency == VideoSource.LOW_LATENCY_DISABLE)
                source = avproSources[i];
            if (source == null)
                source = avproSources[i + supportedResolutions.Length];
            if (source != null)
                break;
        }

        return source;
    }

    public void _OnVideoReady(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video ready event");

        _UpdateHandlers(VIDEO_READY_EVENT);
    }

    public void _OnVideoStart(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video start event");

        _UpdateHandlers(VIDEO_START_EVENT);
    }

    public void _OnVideoEnd(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video end event");

        _UpdateHandlers(VIDEO_END_EVENT);
    }

    public void _OnVideoError(int id, VideoError videoError)
    {
        VideoSource source = sources[id];
        _DebugLog(source, $"Video error event: {videoError}");

        LastError = videoError;
        _UpdateHandlers(VIDEO_ERROR_EVENT);
    }

    public void _OnVideoLoop(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video loop event");

        _UpdateHandlers(VIDEO_LOOP_EVENT);
    }

    public void _OnVideoPause(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video pause event");

        _UpdateHandlers(VIDEO_PAUSE_EVENT);
    }

    public void _OnVideoPlay(int id)
    {
        VideoSource source = sources[id];
        _DebugLog(source, "Video play event");

        _UpdateHandlers(VIDEO_PLAY_EVENT);
    }

    public bool VideoIsPlaying
    {
        get { return activeVideoPlayer.IsPlaying; }
    }

    public float VideoTime
    {
        get { return activeVideoPlayer.GetTime(); }
    }

    public float VideoDuration
    {
        get { return activeVideoPlayer.GetDuration(); }
    }

    public void _VideoPlay()
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, "Play");

        source._VideoPlay();
    }

    public void _VideoPause()
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, "Pause");

        source._VideoPause();
    }

    public void _VideoStop()
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, "Stop");

        source._VideoStop();
    }

    public void _VideoStop(int frameDelay)
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, $"Stop({frameDelay})");

        source._VideoStop(frameDelay);
    }

    public void _VideoLoadUrl(VRCUrl url)
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, $"Load Url: {url}");

        source._VideoLoadUrl(url);
    }

    public void _VideoSetTime(float time)
    {
        VideoSource source = sources[activeSource];
        _DebugLog(source, $"Set time: {time}");

        source._VideoSetTime(time);
    }

    public void _SetAVSync(bool state)
    {
        avSyncEnabled = state;
        _UpdateAVSync(sources[activeSource]);
    }

    void _UpdateAVSync(VideoSource source)
    {
        source._SetAVSync(avSyncEnabled);
    }

    // The activeSourceType is video component type that must be selected
    // The preferredSourceType is the video component type selected in UI that should be preferred (or NONE for auto)
    public void _UpdateVideoSource(int activeSourceType, int preferredSourceType)
    {
        if (videoType == activeSourceType)
            return;

        VideoSource bestSource = _FindBestSource(activeSourceType, preferredResIndex, lowLatency);
        int bestSourceId = (bestSource != null) ? bestSource.ID : -1;

        if (activeSource == bestSourceId)
            return;

        prevSource = activeSource;
        activeSource = bestSourceId;

        if (prevSource >= 0)
            sources[prevSource]._VideoStop(1);

        activeVideoPlayer = null;
        if (activeSource >= 0)
            activeVideoPlayer = sources[activeSource].VideoPlayer;
 
        _UpdateHandlers(SOURCE_CHANGE_EVENT);

        /*int oldSourceOverride = dataProxy.playerSourceOverride;

        if (oldSourceOverride != sourceOverride)
        {
            _StopVideo();

            dataProxy.playerSourceOverride = (short)sourceOverride;

            switch (dataProxy.playerSourceOverride)
            {
                case VIDEO_SOURCE_AVPRO:
                    DebugLog("Setting video source override to AVPro");
                    _currentPlayer = avProVideo;
                    break;
                case VIDEO_SOURCE_UNITY:
                    DebugLog("Setting video source override to Unity");
                    _currentPlayer = unityVideo;
                    break;
                case VIDEO_SOURCE_NONE:
                default:
                    DebugLog("Setting video source override to Auto");
                    _currentPlayer = unityVideo;
                    break;
            }

            change = true;
        }

        if (dataProxy.playerSource != source)
        {
            if (oldSourceOverride == sourceOverride)
            {
                switch (dataProxy.playerSource)
                {
                    case VIDEO_SOURCE_AVPRO: SendCustomEventDelayedFrames("_StopAVPro", 1); break;
                    case VIDEO_SOURCE_UNITY: SendCustomEventDelayedFrames("_StopUnity", 1); break;
                }
            }

            dataProxy.playerSource = (short)source;

            switch (dataProxy.playerSource)
            {
                case VIDEO_SOURCE_AVPRO:
                    DebugLog("Switching video source to AVPro");
                    _currentPlayer = avProVideo;
                    break;
                case VIDEO_SOURCE_UNITY:
                    DebugLog("Switching video source to Unity");
                    _currentPlayer = unityVideo;
                    break;
            }

            change = true;
        }*/
    }

    void _DebugLog(VideoSource source, string message)
    {
        string name = "";
        switch (source.VideoSourceType)
        {
            case VideoSource.VIDEO_SOURCE_UNITY:
                name = $"Unity-{source.ID}";
                break;
            case VideoSource.VIDEO_SOURCE_AVPRO:
                name = $"AVPro-{source.ID}";
                break;
        }

        if (debugLogging)
            Debug.Log($"[VideoTXL:{name}] " + message);
        if (Utilities.IsValid(debugLog))
            debugLog._Write(name, message);
    }
}
