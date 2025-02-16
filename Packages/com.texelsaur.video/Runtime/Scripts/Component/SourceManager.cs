
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public class SourceManager : EventBase
    {
        [SerializeField] internal TXLVideoPlayer videoPlayer;
        [SerializeField] internal VideoUrlSource[] sources;

        public const int EVENT_BIND_VIDEOPLAYER = 0;
        public const int EVENT_TRACK_CHANGE = 1;
        protected const int EVENT_COUNT = 2;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            base._Init();

            sources = (VideoUrlSource[])UtilityTxl.ArrayCompact(sources);
        }

        public void _BindVideoPlayer(TXLVideoPlayer videoPlayer) {
            this.videoPlayer = videoPlayer;
            foreach (VideoUrlSource source in sources)
                source._SetVideoPlayer(videoPlayer);

            _UpdateHandlers(EVENT_BIND_VIDEOPLAYER);
        }

        public TXLVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
        }

        public VideoUrlSource FirstSource
        {
            get { return sources.Length > 0 ? sources[0] : null; }
        }

        public VideoUrlSource _GetSource(int index)
        {
            if (index < 0 || index >= sources.Length)
                return null;
            return sources[index];
        }

        public VideoUrlSource _GetValidSource()
        {
            foreach (VideoUrlSource source in sources)
            {
                if (source.IsValid)
                    return source;
            }

            return null;
        }

        public VideoUrlSource _GetReadySource()
        {
            foreach (VideoUrlSource source in sources)
            {
                if (source.IsReady)
                    return source;
            }

            return null;
        }

        public bool CanMoveNext
        {
            get
            {
                foreach (VideoUrlSource source in sources)
                {
                    if (source._CanMoveNext())
                        return true;
                }

                return false;
            }
        }

        public bool CanMovePrev
        {
            get
            {
                foreach (VideoUrlSource source in sources)
                {
                    if (source._CanMovePrev())
                        return true;
                }

                return false;
            }
        }

        public bool _MoveNext()
        {
            foreach (VideoUrlSource source in sources)
            {
                if (source._CanMoveNext())
                    return source._MoveNext();
            }

            return false;
        }

        public bool _MovePrev()
        {
            foreach (VideoUrlSource source in sources)
            {
                if (source._CanMovePrev())
                    return source._MovePrev();
            }

            return false;
        }

        public void _OnSourceTrackChange(int sourceIndex)
        {
            _UpdateHandlers(EVENT_TRACK_CHANGE, sourceIndex);
        }
    }
}
