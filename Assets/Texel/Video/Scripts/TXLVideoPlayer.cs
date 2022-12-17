
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public abstract class TXLVideoPlayer : EventBase
    {
        public const int EVENT_VIDEO_STATE_UPDATE = 0;
        public const int EVENT_VIDEO_TRACKING_UPDATE = 1;
        public const int EVENT_VIDEO_INFO_UPDATE = 2;
        public const int EVENT_VIDEO_LOCK_UPDATE = 3;
        public const int EVENT_VIDEO_PLAYLIST_UPDATE = 4;
        const int EVENT_COUNT = 5;

        public const int VIDEO_STATE_STOPPED = 0;
        public const int VIDEO_STATE_LOADING = 1;
        public const int VIDEO_STATE_PLAYING = 2;
        public const int VIDEO_STATE_ERROR = 3;

        public const int SCREEN_FIT = 0;
        public const int SCREEN_FIT_HEIGHT = 1;
        public const int SCREEN_FIT_WIDTH = 2;
        public const int SCREEN_STRETCH = 3;

        [Header("Internal Objects")]
        [Tooltip("Manager for multiplexing video sources")]
        public VideoMux videoMux;
        public AudioManager audioManager;

        [NonSerialized]
        public short playerSource;
        [NonSerialized]
        public short playerSourceOverride;
        [NonSerialized]
        public short screenFit;
        [NonSerialized]
        public int playerState;
        [NonSerialized]
        public bool paused;
        [NonSerialized]
        public bool syncing;
        [NonSerialized]
        public bool heldReady;
        [NonSerialized]
        public VideoError lastErrorCode;
        [NonSerialized]
        public bool streamFallback;
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
        public bool repeatPlaylist;
        [NonSerialized]
        public VRCUrl currentUrl = VRCUrl.Empty;
        [NonSerialized]
        public VRCUrl lastUrl = VRCUrl.Empty;
        [NonSerialized]
        public VRCUrl queuedUrl = VRCUrl.Empty;

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
        }

        public virtual bool SupportsLock { get; protected set; }

        public virtual bool _CanTakeControl()
        {
            return true;
        }

        public virtual void _ChangeUrl(VRCUrl url) { }

        public virtual void _Resync() { }

        public virtual void _SetSourceMode(int mode) { }

        public virtual void _SetSourceLatency(int latency) { }

        public virtual void _SetSourceResolution(int res) { }

        public virtual void _SetScreenFit(int fit) { }
    }
}
