
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StreamSource : VideoUrlSource
    {
        TXLVideoPlayer videoPlayer;

        [SerializeField] internal VRCUrl defaultStreamUrl;

        [SerializeField] internal bool loadOnStart = false;
        [SerializeField] internal bool interruptible = false;
        [SerializeField] internal bool fallback = false;
        [SerializeField] internal int preErrorRetryThreshold = 3;
        [SerializeField] internal int postErrorRetryThreshold = 10;
        [SerializeField] internal int endRetryThreshold = 1;

        [UdonSynced]
        VRCUrl syncStreamUrl;
        [UdonSynced, FieldChangeCallback(nameof(SyncReady))]
        bool syncReady;
        [UdonSynced]
        bool syncLoadSuccess;
        [UdonSynced]
        bool syncEndSuccess;
        [UdonSynced]
        int syncErrorCount;
        [UdonSynced]
        bool syncEnabled;

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            syncStreamUrl = VRCUrl.Empty;

            if (Networking.IsOwner(gameObject))
            {
                syncStreamUrl = defaultStreamUrl;
                syncErrorCount = 0;
                syncEnabled = loadOnStart;
                syncReady = false;

                RequestSerialization();
            }
        }

        public override bool IsEnabled
        {
            get { return syncEnabled; }
        }

        public override bool IsValid
        {
            get { return syncEnabled && syncStreamUrl != null && syncStreamUrl != VRCUrl.Empty; }
        }

        public override bool IsReady
        {
            get { return IsValid && syncReady; }
        }

        public override string SourceDefaultName
        {
            get { return "STREAM"; }
        }

        public override string TrackDisplay
        {
            get
            {
                if (!videoPlayer)
                    return "";

                if (videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING)
                {
                    if (!videoPlayer.seekableSource)
                        return "LIVE";
                } else if (videoPlayer.playerState != TXLVideoPlayer.VIDEO_STATE_STOPPED)
                {
                    if (!syncLoadSuccess && syncErrorCount >= 1 && syncErrorCount <= preErrorRetryThreshold)
                        return $"Retry {syncErrorCount} / {preErrorRetryThreshold}";
                    else if (syncLoadSuccess && !syncEndSuccess && syncErrorCount >= 1 && syncErrorCount <= postErrorRetryThreshold)
                        return $"Retry {syncErrorCount} / {postErrorRetryThreshold}";
                    else if (syncEndSuccess && syncErrorCount >= 1)
                        return $"Retry {syncErrorCount} / {endRetryThreshold}";
                }

                return "";
            }
        }

        public override bool AutoAdvance
        {
            get { return true; }
        }

        public override bool ResumeAfterLoad
        {
            get { return true; }
        }

        public override bool Interruptable
        {
            get { return interruptible; }
        }

        public override bool _CanMoveNext()
        {
            return IsValid && !syncReady;
        }

        public override bool _MoveNext()
        {
            Debug.Log("_MoveNext");
            if (!IsValid)
                return false;

            Debug.Log("_MoveNext IsValid");
            if (!_TakeControl())
                return false;

            Debug.Log("_MoveNext TookControl");
            SyncReady = true;
            RequestSerialization();

            return true;
        }

        internal bool SyncReady
        {
            set
            {
                if (syncReady != value)
                {
                    syncReady = value;

                    if (syncReady && URLUtil.WellFormedUrl(syncStreamUrl))
                        _EventUrlReady();
                }
            }
        }

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            this.videoPlayer = videoPlayer;

            _UpdateHandlers(EVENT_BIND_VIDEOPLAYER);
        }

        public override VRCUrl _GetCurrentUrl()
        {
            return syncStreamUrl;
        }

        public override void _OnVideoReady()
        {
            if (Networking.IsOwner(gameObject))
            {
                syncLoadSuccess = true;
                syncErrorCount = 0;

                RequestSerialization();
            }
        }

        public override void _OnVideoStop()
        {
            if (Networking.IsOwner(gameObject))
            {
                if (fallback)
                    syncReady = false;
                syncErrorCount = 0;
                syncLoadSuccess = false;
                syncEndSuccess = false;

                RequestSerialization();
            }
        }

        public override VideoEndAction _OnVideoEnd()
        {
            VideoEndAction result = VideoEndAction.Advance;

            if (Networking.IsOwner(gameObject))
            {
                syncEndSuccess = true;
                if (endRetryThreshold > 0)
                    result = VideoEndAction.Retry;
                else
                {
                    if (fallback)
                        syncReady = false;
                    syncErrorCount = 0;
                    syncLoadSuccess = false;
                    syncEndSuccess = false;

                    RequestSerialization();
                }
            }

            return result;
        }

        public override VideoErrorAction _OnVideoError(VideoError error)
        {
            VideoErrorAction result = VideoErrorAction.Retry;

            if (Networking.IsOwner(gameObject))
            {
                if (syncLoadSuccess && syncErrorCount > postErrorRetryThreshold)
                    result = VideoErrorAction.Advance;
                else if (!syncLoadSuccess && syncErrorCount > preErrorRetryThreshold)
                    result = VideoErrorAction.Advance;

                syncErrorCount += 1;
                RequestSerialization();
            }

            return result;
        }

        public void _SetUrl(VRCUrl url)
        {
            if (!URLUtil.WellFormedUrl(url))
                return;

            if (!_TakeControl())
                return;

            syncEnabled = true;
            syncReady = false;
            syncErrorCount = 0;
            syncLoadSuccess = false;
            syncEndSuccess = false;
            syncStreamUrl = url;
            
            RequestSerialization();
        }

        bool _TakeControl()
        {
            if (videoPlayer && videoPlayer.SupportsOwnership && !videoPlayer._TakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }
    }
}
