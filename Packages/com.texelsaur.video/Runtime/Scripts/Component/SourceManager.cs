
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

        [Tooltip("Log debug statements to a world object")]
        [SerializeField] internal DebugLog debugLog;
        [SerializeField] internal bool vrcLogging = false;
        [SerializeField] internal bool eventLogging = false;
        [SerializeField] internal bool lowLevelLogging = false;

        private int nextSourceIndex = 0;

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
            foreach (VideoUrlSource source in sources)
                source._SetSourceManager(this, nextSourceIndex++);

            if (eventLogging)
                eventDebugLog = debugLog;
        }

        public void _BindVideoPlayer(TXLVideoPlayer videoPlayer) {
            this.videoPlayer = videoPlayer;
            foreach (VideoUrlSource source in sources)
                source._SetVideoPlayer(videoPlayer);

            _UpdateHandlers(EVENT_BIND_VIDEOPLAYER);
        }

        public int Count
        {
            get { return sources.Length; }
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

        public int _GetValidSource(int startIndex = 0)
        {
            for (int i = startIndex; i < sources.Length; i++)
            {
                if (sources[i].IsValid)
                    return i;
            }

            return -1;
        }

        public int _GetReadySource(int startIndex = 0)
        {
            for (int i = startIndex; i < sources.Length; i++)
            {
                if (sources[i].IsReady)
                    return i;
            }

            return -1;
        }

        public int _GetCanAddTrack(int startIndex = 0)
        {
            for (int i = startIndex; i < sources.Length; i++)
            {
                if (sources[i]._CanAddTrack())
                    return i;
            }

            return -1;
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

        public bool _AdvanceNext(string currentUrl = null)
        {
            _DebugLowLevel("AdvanceNext");

            foreach (VideoUrlSource source in sources)
            {
                if (!source.IsValid || !source.AutoAdvance)
                    continue;

                if (currentUrl != null && !source.ResumeAfterLoad)
                {
                    string playlistUrl = source._GetCurrentUrl() != null ? source._GetCurrentUrl().Get() : "";
                    bool currentTrackFromList = currentUrl == playlistUrl;
                    if (!currentTrackFromList)
                        continue;
                }

                if (source._CanMoveNext() && source._MoveNext())
                {
                    _DebugLowLevel($"Advanced source {source}");
                    return true;
                }
            }

            return false;
        }

        public void _OnSourceTrackChange(int sourceIndex)
        {
            _UpdateHandlers(EVENT_TRACK_CHANGE, sourceIndex);
        }

        void _DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[VideoTXL:SourceManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SourceManager", message);
        }

        void _DebugError(string message, bool force = false)
        {
            if (vrcLogging || force)
                Debug.LogError("[VideoTXL:SourceManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("SourceManager", message);
        }

        void _DebugLowLevel(string message)
        {
            if (lowLevelLogging)
                _DebugLog(message);
        }
    }
}
