
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StreamSource : VideoUrlSource
    {
        TXLVideoPlayer videoPlayer;

        [SerializeField] internal string defaultStreamName;
        [SerializeField] internal VRCUrl defaultStreamUrl;
        // [SerializeField] internal VRCUrl defaultStreamQuestUrl;
        [SerializeField] internal string[] additionalStreamNames;
        [SerializeField] internal VRCUrl[] additionalStreamUrls;
        // [SerializeField] internal VRCUrl[] additionalStreamQuestUrls;
        [SerializeField] internal bool allowCustomUrl = false;
        [SerializeField] internal bool allowCustomQuestUrl = false;

        [SerializeField] internal bool loadOnStart = false;
        [SerializeField] internal bool interruptible = false;
        [SerializeField] internal bool fallback = false;
        [SerializeField] internal int preErrorRetryThreshold = 3;
        [SerializeField] internal int postErrorRetryThreshold = 10;
        [SerializeField] internal int endRetryThreshold = 1;

        [UdonSynced]
        VRCUrl syncStreamUrl;
        [UdonSynced]
        VRCUrl syncStreamQuestUrl;
        [UdonSynced, FieldChangeCallback(nameof(SyncCustomUrl))]
        VRCUrl syncCustomUrl = VRCUrl.Empty;
        [UdonSynced, FieldChangeCallback(nameof(SyncCustomQuestUrl))]
        VRCUrl syncCustomQuestUrl = VRCUrl.Empty;
        [UdonSynced, FieldChangeCallback(nameof(SyncReady))]
        bool syncReady;
        [UdonSynced]
        int syncStreamUrlSerial = 0;
        [UdonSynced]
        bool syncLoadSuccess;
        [UdonSynced]
        bool syncEndSuccess;
        [UdonSynced]
        int syncErrorCount;
        [UdonSynced]
        bool syncEnabled;

        private int prevStreamUrlSerial = 0;
        private int urlChangeSerial = 0;

        public const int EVENT_URL_CHANGE = VideoUrlSource.EVENT_COUNT + 0;
        public const int EVENT_CUSTOM_URL_CHANGE = VideoUrlSource.EVENT_COUNT + 1;
        protected new const int EVENT_COUNT = VideoUrlSource.EVENT_COUNT + 2;

        protected override int EventCount => EVENT_COUNT;

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            syncStreamUrl = VRCUrl.Empty;

            int validElements = Mathf.Min(additionalStreamNames.Length, additionalStreamUrls.Length);

            if (additionalStreamNames.Length < validElements)
                additionalStreamNames = (string[])UtilityTxl.ArrayMinSize(additionalStreamNames, validElements, typeof(string));
            if (additionalStreamUrls.Length < validElements)
                additionalStreamUrls = (VRCUrl[])UtilityTxl.ArrayMinSize(additionalStreamUrls, validElements, typeof(VRCUrl));
            // if (additionalStreamQuestUrls.Length < validElements)
            //     additionalStreamQuestUrls = (VRCUrl[])UtilityTxl.ArrayMinSize(additionalStreamQuestUrls, validElements, typeof(VRCUrl));

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

        public override TXLVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
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

        public int UrlChangeSerial
        {
            get { return prevStreamUrlSerial; }
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

        public string DefaultStreamName
        {
            get { return defaultStreamName; }
        }

        public VRCUrl DefaulStreamtUrl
        {
            get { return defaultStreamUrl; }
        }

        /* public VRCUrl DefaultStreamQuestUrl
        {
            get { return defaultStreamQuestUrl; }
        } */

        internal VRCUrl SyncCustomUrl
        {
            get { return syncCustomUrl; }
            set
            {
                if (syncCustomUrl != value)
                {
                    syncCustomUrl = value;

                    _UpdateHandlers(EVENT_CUSTOM_URL_CHANGE);
                }
            }
        }

        public VRCUrl CustomStreamUrl
        {
            get { return syncCustomUrl; }
            set
            {
                if (!_TakeControl())
                    return;

                SyncCustomUrl = value;
                RequestSerialization();
            }
        }

        internal VRCUrl SyncCustomQuestUrl
        {
            get { return syncCustomQuestUrl; }
            set
            {
                if (syncCustomQuestUrl != value)
                {
                    syncCustomQuestUrl = value;

                    _UpdateHandlers(EVENT_CUSTOM_URL_CHANGE);
                }
            }
        }

        public VRCUrl CustomStreamQuestUrl
        {
            get { return syncCustomQuestUrl; }
            set
            {
                if (!_TakeControl())
                    return;

                SyncCustomQuestUrl = value;
                RequestSerialization();
            }
        }

        public int AdditionalUrlCount
        {
            get { return additionalStreamUrls.Length; }
        }

        public string _GetAdditionalStreamName(int index)
        {
            if (index < 0 || index >= additionalStreamUrls.Length)
                return null;

            return additionalStreamNames[index];
        }

        public VRCUrl _GetAdditionalStreamUrl(int index)
        {
            if (index < 0 || index >= additionalStreamUrls.Length)
                return null;

            return additionalStreamUrls[index];
        }

        /* public VRCUrl _GetAdditionalStreamQuestUrl(int index)
        {
            if (index < 0 || index >= additionalStreamQuestUrls.Length)
                return null;

            return additionalStreamQuestUrls[index];
        } */

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            this.videoPlayer = videoPlayer;

            _UpdateHandlers(EVENT_BIND_VIDEOPLAYER);
        }

        public override VRCUrl _GetCurrentUrl()
        {
            return syncStreamUrl;
        }

        public override VRCUrl _GetCurrentQuestUrl()
        {
            return syncStreamQuestUrl;
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

        public void _SetUrl(VRCUrl url, VRCUrl questUrl = null)
        {
            if (!URLUtil.WellFormedUrl(url))
                return;
            if (!URLUtil.EmptyUrl(questUrl) && !URLUtil.WellFormedUrl(questUrl))
                return;

            if (!_TakeControl())
                return;

            //syncEnabled = true;
            syncErrorCount = 0;
            syncLoadSuccess = false;
            syncEndSuccess = false;
            SyncReady = false;

            syncStreamUrl = url;
            syncStreamQuestUrl = questUrl;
            syncStreamUrlSerial += 1;

            RequestSerialization();

            _MoveNext();
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            if (syncStreamUrlSerial > prevStreamUrlSerial)
            {
                prevStreamUrlSerial = syncStreamUrlSerial;
                _EventUrlChange();
            }
        }

        bool _TakeControl()
        {
            if (videoPlayer && videoPlayer.SupportsOwnership && !videoPlayer._TakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }

        protected void _EventUrlChange()
        {
            _UpdateHandlers(EVENT_URL_CHANGE);
        }
    }
}
