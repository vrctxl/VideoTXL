using Newtonsoft.Json.Serialization;
using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

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

        [SerializeField] protected internal string sourceName;
        [SerializeField, HideInInspector] protected internal SourceManager sourceManager;
        [SerializeField] protected internal VideoDisplayOverride overrideDisplay;

        [SerializeField] protected internal VideoErrorAction errorAction = VideoErrorAction.Retry;
        [Obsolete("Use retriesExceededAction")]
        [SerializeField] protected internal VideoErrorAction terminalErrorAction = VideoErrorAction.Advance;
        [SerializeField] protected internal VideoErrorAction retriesExceededAction = VideoErrorAction.Advance;
        [SerializeField] protected internal int maxErrorRetryCount = 1;

        protected int sourceIndex = -1;
        protected int errorCount = 0;

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
            get 
            {
                if (IsInErrorRetry)
                    return RetryTrackDisplay;

                return "";
            }
        }

        public virtual bool SupportsRetry
        {
            get { return errorAction == VideoErrorAction.Retry; }
        }

        public virtual int RetryCount
        {
            get { return errorCount; }
        }

        public virtual int MaxRetryCount
        {
            get { return maxErrorRetryCount; }
        }

        protected virtual bool IsInErrorRetry
        {
            get
            {
                if (errorAction == VideoErrorAction.Retry && VideoPlayer)
                {
                    int state = VideoPlayer.playerState;
                    if (state == TXLVideoPlayer.VIDEO_STATE_ERROR || state == TXLVideoPlayer.VIDEO_STATE_LOADING)
                        return errorCount > 1;
                }

                return false;
            }
        }

        protected virtual string RetryTrackDisplay
        {
            get
            {
                if (errorCount >= 1 && errorCount <= maxErrorRetryCount)
                    return $"Retry {errorCount} / {maxErrorRetryCount}";

                return "";
            }
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
            errorCount = 0;
            return false;
        }

        public virtual bool _MovePrev()
        {
            errorCount = 0;
            return false;
        }

        public virtual bool _MoveTo(int index)
        {
            errorCount = 0;
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

        public virtual void _OnVideoStop() 
        {
            errorCount = 0;
        }

        public virtual void _OnVideoReady() 
        {
            errorCount = 0;
        }

        public virtual void _OnVideoStart() { }

        public virtual VideoEndAction _OnVideoEnd()
        {
            errorCount = 0;
            return VideoEndAction.Default;
        }

        public virtual VideoErrorAction _OnVideoError(VideoError error)
        {
            if (error == VideoError.RateLimited)
                return VideoErrorAction.Retry;

            if (errorAction == VideoErrorAction.Retry)
            {
                errorCount += 1;
                if (errorCount > maxErrorRetryCount)
                    return retriesExceededAction;
                else
                    return VideoErrorAction.Retry;
            }

            return errorAction;
        }

        public virtual VideoErrorAction _OnVideoError(VideoErrorTXL error)
        {
            return errorAction;
        }

        protected void _EventUrlReady()
        {
            if (sourceManager)
                sourceManager._OnUrlReady(sourceIndex);

            _UpdateHandlers(EVENT_URL_READY);
        }
    }
}
