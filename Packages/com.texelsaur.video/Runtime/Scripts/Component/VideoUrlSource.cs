using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum UrlSourceType
{
    None = 0,
    Playlist = 1,
    Queue = 2,
    Custom = 100,
}

namespace Texel
{
    public abstract class VideoUrlSource : EventBase
    {
        public const int EVENT_BIND_VIDEOPLAYER = 0;
        public const int EVENT_OPTION_CHANGE = 1;
        public const int EVENT_URL_READY = 2;
        protected const int EVENT_COUNT = 3;

        [SerializeField] protected string sourceName;
        [SerializeField] protected SourceManager sourceManager;

        protected int sourceIndex = -1;

        protected override int EventCount { get => EVENT_COUNT; }

        public virtual void _SetVideoPlayer(TXLVideoPlayer videoPlayer) { }

        public void _SetSourceManager(SourceManager manager, int index)
        {
            sourceManager = manager;
            sourceIndex = index;
        }

        public virtual string SourceName
        {
            get { return (sourceName != null && sourceName != "") ? sourceName : SourceDefaultName; }
        }

        public virtual string SourceDefaultName
        {
            get { return ""; }
        }

        public virtual string TrackDisplay
        {
            get { return ""; }
        }

        public virtual TXLVideoPlayer VideoPlayer
        {
            get { return null; }
        }

        public virtual bool IsEnabled
        {
            get { return false; }
        }

        public virtual bool IsValid
        {
            get { return false; }
        }

        public virtual bool IsReady
        {
            get { return false; }
        }

        public virtual bool AutoAdvance
        {
            get { return false; }
            set { }
        }

        public virtual bool ResumeAfterLoad
        {
            get { return false; }
            set { }
        }

        public virtual VideoUrlListSource ListSource
        {
            get { return null; }
        }

        public virtual PlaylistQueue TargetQueue
        {
            get { return null; }
        }

        public virtual VRCUrl _GetCurrentUrl()
        {
            return VRCUrl.Empty;
        }

        public virtual VRCUrl _GetCurrentQuestUrl()
        {
            return VRCUrl.Empty;
        }

        public virtual void _PlayCurrentUrl()
        {

        }

        public virtual bool _CanMoveNext()
        {
            return false;
        }

        public virtual bool _CanMovePrev()
        {
            return false;
        }

        public virtual bool _CanMoveTo(int index)
        {
            return false;
        }

        public virtual bool _MoveNext()
        {
            return false;
        }

        public virtual bool _MovePrev()
        {
            return false;
        }

        public virtual bool _MoveTo(int index)
        {
            return false;
        }

        public virtual bool _CanAddTrack()
        {
            return false;
        }

        public virtual bool _AddTrack(VRCUrl url)
        {
            return false;
        }

        protected void _EventUrlReady()
        {
            if (sourceManager)
                sourceManager._OnUrlReady(sourceIndex);

            _UpdateHandlers(EVENT_URL_READY);
        }
    }
}
