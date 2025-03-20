using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum VideoEndAction
    {
        Default,
        Retry,
        Stop,
        Advance,
    }

    public enum VideoErrorAction
    {
        Default,    // Let video player make its own decision
        Retry,      // Instruct video player to retry loading the same URL
        Stop,       // Instruct video player to stop all playback
        Advance     // Instruct video player to move to next available track/source
    }

    public enum VideoDisplayOverride
    {
        None,
        Logo,
    }

    public abstract class VideoUrlSource : EventBase
    {
        public const int EVENT_BIND_VIDEOPLAYER = 0;
        public const int EVENT_OPTION_CHANGE = 1;
        public const int EVENT_URL_READY = 2;
        public const int EVENT_INTERRUPT = 3;
        protected const int EVENT_COUNT = 4;

        [SerializeField] protected string sourceName;
        [SerializeField, HideInInspector] protected SourceManager sourceManager;
        [SerializeField] protected VideoDisplayOverride overrideDisplay; 

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

        public virtual VideoDisplayOverride DisplayOverride
        {
            get { return overrideDisplay; }
        }

        public virtual bool Interruptable
        {
            get { return false; }
        }

        public virtual VRCUrl _GetCurrentUrl()
        {
            return VRCUrl.Empty;
        }

        public virtual VRCUrl _GetCurrentQuestUrl()
        {
            return VRCUrl.Empty;
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

        public virtual void _OnVideoStop() { }

        public virtual void _OnVideoReady() { }

        public virtual void _OnVideoStart() { }

        public virtual VideoEndAction _OnVideoEnd()
        {
            return VideoEndAction.Default;
        }

        public virtual VideoErrorAction _OnVideoError(VideoError error)
        {
            return VideoErrorAction.Default;
        }

        public virtual VideoErrorAction _OnVideoError(VideoErrorTXL error)
        {
            return VideoErrorAction.Default;
        }

        protected void _EventUrlReady()
        {
            if (sourceManager)
                sourceManager._OnUrlReady(sourceIndex);

            _UpdateHandlers(EVENT_URL_READY);
        }
    }
}
