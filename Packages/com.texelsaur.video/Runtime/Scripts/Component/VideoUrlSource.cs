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
        protected const int EVENT_COUNT = 2;

        protected override int EventCount { get => EVENT_COUNT; }

        public virtual void _SetVideoPlayer(TXLVideoPlayer videoPlayer) { }

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
    }
}
