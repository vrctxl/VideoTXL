
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

#if AUDIOLINK_V1
using AudioLink;
#endif

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    public enum TXLScreenFit : byte
    {
        Fit,
        FitHeight,
        FitWidth,
        Stretch,
        Fill,
    }

    public enum TXLRepeatMode : byte
    {
        None,
        All,
        Single,
    }

    public enum TXLUrlType : byte
    {
        Normal,
        Quest,
    }

    public abstract class TXLVideoPlayer : EventBase
    {
        public const int EVENT_VIDEO_STATE_UPDATE = 0;
        public const int EVENT_VIDEO_TRACKING_UPDATE = 1;
        public const int EVENT_VIDEO_INFO_UPDATE = 2;
        public const int EVENT_VIDEO_LOCK_UPDATE = 3;
        public const int EVENT_VIDEO_PLAYLIST_UPDATE = 4;
        public const int EVENT_VIDEO_READY = 5;
        public const int EVENT_BIND_VIDEOMANAGER = 6;
        public const int EVENT_UNBIND_VIDEOMANAGER = 7;
        public const int EVENT_VIDEO_SOURCE_CHANGE = 8;
        public const int EVENT_POSTINIT_DONE = 9;
        protected const int EVENT_COUNT = 10;

        public const int VIDEO_STATE_STOPPED = 0;
        public const int VIDEO_STATE_LOADING = 1;
        public const int VIDEO_STATE_PLAYING = 2;
        public const int VIDEO_STATE_ERROR = 3;

        public const int SCREEN_FIT = 0;
        public const int SCREEN_FIT_HEIGHT = 1;
        public const int SCREEN_FIT_WIDTH = 2;
        public const int SCREEN_STRETCH = 3;
        public const int SCREEN_FILL = 4;

        protected VideoManager videoMux;
        protected AudioManager audioManager;

        [SerializeField] protected internal SourceManager sourceManager;

        [HideInInspector]
        public bool prefabInitialized = false;

        public bool runBuildHooks = true;

        [NonSerialized]
        public short playerSource;
        [NonSerialized]
        public short playerSourceOverride;
        [NonSerialized]
        public byte screenFit;
        [NonSerialized]
        public int playerState;
        [NonSerialized]
        public bool paused;
        [NonSerialized]
        public bool syncing;
        [NonSerialized]
        public VideoError lastErrorCode;
        [NonSerialized]
        public bool streamFallback;
        [NonSerialized]
        public bool videoFallback;
        [NonSerialized]
        public bool seekableSource;
        [NonSerialized]
        public float trackDuration;
        [NonSerialized]
        public float trackPosition;
        [NonSerialized]
        public float trackTarget;
        [NonSerialized]
        public bool locked;
        [NonSerialized]
        [Obsolete("Use property RepeatMode")]
        public bool repeatPlaylist;
        [NonSerialized]
        public VRCUrl currentUrl = VRCUrl.Empty;
        [NonSerialized]
        public VRCUrl lastUrl = VRCUrl.Empty;
        [NonSerialized]
        [Obsolete("Queued URL has been replaced by Source Manager")]
        public VRCUrl queuedUrl = VRCUrl.Empty;
        [NonSerialized]
        public VideoUrlSource currentUrlSource = null;

        public bool IsQuest { get; private set; }

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            IsQuest = false;
#if UNITY_ANDROID
            IsQuest = true;
#endif

#if AUDIOLINK_V1
            _InitAudioLink();
#endif
        }

        public virtual bool SupportsLock { get; protected set; }

        public virtual bool SupportsOwnership { get; protected set; }

        public virtual TXLRepeatMode RepeatMode { get; set; }

        public virtual bool _CanTakeControl()
        {
            return true;
        }

        public virtual void _ChangeUrl(VRCUrl url) { }

        public virtual void _ChangeUrl(VRCUrl url, VRCUrl questUrl) { }

        public virtual void _Resync() { }

        public virtual void _SetSourceMode(int mode) { }

        public virtual void _SetSourceLatency(int latency) { }

        public virtual void _SetSourceResolution(int res) { }

        public virtual void _SetScreenFit(TXLScreenFit fit) { }

        public virtual void _ValidateVideoSources() { }

        public virtual bool _TakeControl()
        {
            return false;
        }

        public VideoManager VideoManager
        {
            get { return videoMux; }
        }

        public AudioManager AudioManager
        {
            get { return audioManager; }
        }

        public SourceManager SourceManager
        {
            get { return sourceManager; }
        }

        public virtual UrlInfoResolver UrlInfoResolver
        {
            get { return null; }
        }

        public virtual void _SetVideoManager(VideoManager manager)
        {
            if (videoMux)
                _UpdateHandlers(EVENT_UNBIND_VIDEOMANAGER);

            videoMux = manager;

            if (videoMux)
                _UpdateHandlers(EVENT_BIND_VIDEOMANAGER);
        }

        public virtual void _SetAudioManager(AudioManager manager)
        {
            audioManager = manager;

#if AUDIOLINK_V1
            audioManager._Register(AudioManager.EVENT_MASTER_VOLUME_UPDATE, this, nameof(_AudioLinkOnMasterVolumeUpdate));
            audioManager._Register(AudioManager.EVENT_AUDIOLINK_CHANGED, this, nameof(_AudioLinkOnBind));
            _AudioLinkOnBind();
#endif
        }

        internal static string _ParseYoutube(string url)
        {
            string pattern = @"(?:https?:\/\/)?(?:www\.)?youtu\.?be(?:\.com)?\/?.*(?:watch|embed)?(?:.*v=|v\/|\/)([\w\-_]+)\&?";
            Match match = Regex.Match(url, pattern);
            if (match.Success)
                return $"YouTube [{match.Groups[1]}]";

            return null;
        }

        // AudioLink API

#if AUDIOLINK_V1
        void _InitAudioLink()
        {
            _Register(EVENT_VIDEO_STATE_UPDATE, this, nameof(_AudioLinkOnVideoStateUpdate));
            _Register(EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_AudioLinkOnVideoTrackingUpdate));
            _Register(EVENT_VIDEO_PLAYLIST_UPDATE, this, nameof(_AudioLinkOnVideoPlaylistUpdate));

            _AudioLinkOnVideoStateUpdate();
            _AudioLinkOnVideoTrackingUpdate();
            _AudioLinkOnMasterVolumeUpdate();
        }

        AudioLink.AudioLink audioLink;

        public void _AudioLinkOnVideoStateUpdate()
        {
            if (!audioLink)
                return;

            switch (playerState)
            {
                case VIDEO_STATE_STOPPED:
                    audioLink.SetMediaPlaying(MediaPlaying.Stopped);
                    audioLink.SetMediaTime(0);
                    break;
                case VIDEO_STATE_LOADING:
                    audioLink.SetMediaPlaying(MediaPlaying.Loading);
                    audioLink.SetMediaTime(0);
                    break;
                case VIDEO_STATE_ERROR:
                    audioLink.SetMediaPlaying(MediaPlaying.Error);
                    audioLink.SetMediaTime(0);
                    break;
                case VIDEO_STATE_PLAYING:
                    if (paused)
                        audioLink.SetMediaPlaying(MediaPlaying.Paused);
                    else if (seekableSource)
                        audioLink.SetMediaPlaying(MediaPlaying.Playing);
                    else
                    {
                        audioLink.SetMediaPlaying(MediaPlaying.Streaming);
                        audioLink.SetMediaTime(0);
                    }
                    break;
            }

            TXLRepeatMode mode = RepeatMode;
            if (mode == TXLRepeatMode.None)
                audioLink.SetMediaLoop(MediaLoop.None);
            else if (mode == TXLRepeatMode.Single)
                audioLink.SetMediaLoop(MediaLoop.LoopOne);
            else
                audioLink.SetMediaLoop(MediaLoop.Loop);
        }

        public void _AudioLinkOnVideoTrackingUpdate()
        {
            if (!audioLink)
                return;

            if (!seekableSource || trackDuration == 0)
                audioLink.SetMediaTime(0);
            else
                audioLink.SetMediaTime(trackPosition / trackDuration);
        }

        public void _AudioLinkOnVideoPlaylistUpdate()
        {
            _AudioLinkOnVideoStateUpdate();
        }

        public void _AudioLinkOnMasterVolumeUpdate()
        {
            if (!audioLink || !audioManager)
                return;

            audioLink.SetMediaVolume(audioManager.masterVolume);
        }

        public void _AudioLinkOnBind()
        {
            audioLink = null;
            if (!audioManager)
                return;

            if (audioManager.audioLinkSystem)
            {
                audioLink = (AudioLink.AudioLink)(Component)audioManager.audioLinkSystem;
                audioLink.autoSetMediaState = false;

                _AudioLinkOnVideoStateUpdate();
                _AudioLinkOnVideoTrackingUpdate();
                _AudioLinkOnMasterVolumeUpdate();
            }
        }
#endif
    }
}
