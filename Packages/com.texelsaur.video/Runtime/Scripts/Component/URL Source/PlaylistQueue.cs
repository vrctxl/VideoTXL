using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    enum QueueEntryType : byte
    {
        URL,
        Playlist,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlaylistQueue : VideoUrlSource
    {
        TXLVideoPlayer videoPlayer;

        [Tooltip("Optional. ACL to control access to the move-to-front button.  If not set, uses the video player's ACL settings.")]
        [SerializeField] protected internal AccessControl priorityAccess;
        [Tooltip("Optional. ACL to control access to the delete button.  If not set, uses the video player's ACL settings.")]
        [SerializeField] protected internal AccessControl deleteAccess;

        [SerializeField] protected internal bool enableSyncQuestUrls = true;
        [SerializeField] protected internal bool syncTrackTitles = true;
        [SerializeField] protected internal bool syncTrackAuthors = true;
        [SerializeField] protected internal bool syncPlayerNames = true;

        // public bool removeTracks = true;
        [UdonSynced]
        VRCUrl syncReadyUrl;
        [UdonSynced]
        VRCUrl syncReadyQuestUrl;
        [UdonSynced]
        string syncReadyTitle;
        [UdonSynced]
        string syncReadyPlayer;
        [UdonSynced]
        VRCUrl[] syncUrls;
        [UdonSynced]
        VRCUrl[] syncQuestUrls;
        [UdonSynced]
        Vector3[] syncEntries; // (playlistIndex, catalogIndex, trackIndex)
        [UdonSynced]
        string[] syncTitles;
        [UdonSynced]
        string[] syncAuthors;
        [UdonSynced]
        string[] syncPlayers;
        [UdonSynced]
        short syncTrackCount = 0;
        [UdonSynced, FieldChangeCallback("SourceEnabled")]
        bool syncEnabled = true;

        [UdonSynced]
        int syncQueueUpdate = 0;
        int prevQueueUpdate = 0;

        [UdonSynced]
        int syncTrackChangeUpdate = 0;
        int prevTrackChangeUpdate = 0;

        [UdonSynced]
        int syncReadyUrlUpdate = 0;
        int prevReadyUrlUpdate = 0;

        bool usingQuestUrls = false;
        bool usingTitles = false;
        bool usingAuthors = false;
        bool usingPlayers = false;
        [HideInInspector] public VRCUrl internalArgUrl;

        private Playlist[] playlistSources = new Playlist[0];

        public const int EVENT_LIST_CHANGE = VideoUrlSource.EVENT_COUNT + 0;
        public const int EVENT_TRACK_CHANGE = VideoUrlSource.EVENT_COUNT + 1;
        protected new const int EVENT_COUNT = VideoUrlSource.EVENT_COUNT + 2;

        protected override int EventCount => EVENT_COUNT;

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            syncReadyUrl = VRCUrl.Empty;
            syncReadyTitle = "";
            syncReadyPlayer = "";

            syncUrls = new VRCUrl[0];
            syncQuestUrls = new VRCUrl[0];
            syncEntries = new Vector3[0];

            // Info data
            syncTitles = new string[0];
            syncAuthors = new string[0];
            syncPlayers = new string[0];

            usingQuestUrls = false;
            usingTitles = false;
            usingAuthors = false;
            usingPlayers = syncPlayerNames;
        }

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            _EnsureInit();

            this.videoPlayer = videoPlayer;

            usingQuestUrls = enableSyncQuestUrls;

            if (videoPlayer.UrlInfoResolver)
            {
                usingTitles = syncTrackTitles;
                usingAuthors = syncTrackAuthors;

                videoPlayer.UrlInfoResolver._Register(UrlInfoResolver.EVENT_URL_INFO, this, nameof(_InternalOnInfoResolve), nameof(internalArgUrl));
            }

            _UpdateHandlers(VideoUrlSource.EVENT_BIND_VIDEOPLAYER);
        }

        public int _RegisterPlaylistSource(Playlist playlist)
        {
            playlistSources = (Playlist[])UtilityTxl.ArrayAddElement(playlistSources, playlist, playlist.GetType());
            return playlistSources.Length - 1;
        }

        public void _InternalOnInfoResolve()
        {
            int index = _FindUrlIndex(internalArgUrl);
            if (index == -1)
                return;

            if (usingTitles && syncTitles[index] != "")
                return;
            if (usingAuthors && syncAuthors[index] != "")
                return;

            if (_UpdateInfoFromResolver(internalArgUrl, index))
            {
                if (!_TakeControl())
                    return;

                syncQueueUpdate += 1;

                _EventListChange();
                RequestSerialization();
            }
        }

        public override string SourceDefaultName
        {
            get { return "QUEUE"; }
        }

        public override string TrackDisplay
        {
            get 
            {
                if (IsInErrorRetry)
                    return RetryTrackDisplay;

                return IsValid ? $"+{syncTrackCount} Queued" : ""; 
            }
        }

        public override TXLVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
        }

        public override bool IsEnabled
        {
            get { return syncEnabled; }
        }

        public override bool IsValid
        {
            get { return syncEnabled && syncTrackCount > 0; }
        }

        public override bool IsReady
        {
            get { return syncEnabled && syncTrackCount > 0; }
        }

        public override bool AutoAdvance
        {
            get { return true; }
        }

        public override bool ResumeAfterLoad
        {
            get { return true; }
        }

        public bool SourceEnabled
        {
            get { return syncEnabled; }
            set
            {
                syncEnabled = value;
            }
        }

        public short Count
        {
            get { return syncTrackCount; }
        }

        public bool HasPriorityAccess
        {
            get
            {
                if (priorityAccess)
                    return priorityAccess._LocalHasAccess();
                if (videoPlayer)
                    return videoPlayer._CanTakeControl();
                return false;
            }
        }

        public bool HasDeleteAccess
        {
            get
            {
                if (deleteAccess)
                    return deleteAccess._LocalHasAccess();
                if (videoPlayer)
                    return videoPlayer._CanTakeControl();
                return false;
            }
        }

        public override bool _CanMoveNext()
        {
            return syncTrackCount > 0;
        }

        public override bool _CanMovePrev()
        {
            return false;
        }

        public override bool _CanMoveTo(int index)
        {
            return index >= 0 && index < syncTrackCount;
        }

        public override VRCUrl _GetCurrentUrl()
        {
            return _GetCurrentUrl(TXLUrlType.Normal);
        }

        public override VRCUrl _GetCurrentQuestUrl()
        {
            return _GetCurrentUrl(TXLUrlType.Quest);
        }

        public virtual VRCUrl _GetCurrentUrl(TXLUrlType urlType)
        {
            if (urlType == TXLUrlType.Quest)
                return syncReadyQuestUrl;

            return syncReadyUrl;
        }

        public virtual string _GetCurrentTitle()
        {
            return syncReadyTitle;
        }

        public virtual string _GetCurrentPlayer()
        {
            return syncReadyPlayer;
        }

        public VRCUrl _GetTrackURL(int index)
        {
            return _GetTrackURL(index, TXLUrlType.Normal);
        }

        public VRCUrl _GetTrackURL(int index, TXLUrlType urlType)
        {
            if (index < 0 || index >= syncTrackCount)
                return null;

            if (syncEntries[index].x == -1)
            {
                if (usingQuestUrls && urlType == TXLUrlType.Quest)
                    return syncQuestUrls[index];
                return syncUrls[index];
            }

            return _GetPlaylistUrl(index, urlType);
        }

        private VRCUrl _GetPlaylistUrl(int entryIndex, TXLUrlType type)
        {
            PlaylistData data = _GetPlaylistData(entryIndex);
            int trackIndex = (int)syncEntries[entryIndex].z;
            if (trackIndex > -1 && data && trackIndex < data.playlist.Length)
            {
                if (type == TXLUrlType.Quest && data.questPlaylist[trackIndex] != VRCUrl.Empty)
                    return data.questPlaylist[trackIndex];
                return data.playlist[trackIndex];
            }

            return VRCUrl.Empty;
        }

        private PlaylistData _GetPlaylistData(int entryIndex)
        {
            int playlistIndex = (int)syncEntries[entryIndex].x;
            if (playlistIndex < playlistSources.Length)
            {
                Playlist playlist = playlistSources[playlistIndex];
                int catalogIndex = (int)syncEntries[entryIndex].y;

                PlaylistData data = playlist.playlistData;
                if (catalogIndex > -1 && playlist.playlistCatalog)
                {
                    PlaylistCatalog catalog = playlist.playlistCatalog;
                    if (catalogIndex < catalog.playlists.Length)
                        data = catalog.playlists[catalogIndex];
                }

                return data;
            }

            return null;
        }

        public string _GetTrackName(int index)
        {
            if (syncEntries[index].x == -1) {
                if (videoPlayer && videoPlayer.UrlInfoResolver)
                    return videoPlayer.UrlInfoResolver._GetFormatted(syncUrls[index]);
                return "";
            }

            PlaylistData data = _GetPlaylistData(index);
            int trackIndex = (int)syncEntries[index].z;
            if (trackIndex > -1 && data && trackIndex < data.playlist.Length)
                return data.trackNames[trackIndex];

            return "";
        }

        public string _GetTrackPlayer(int index)
        {
            if (!usingPlayers)
                return "";

            return syncPlayers[index];
        }

        public override bool _MoveNext()
        {
            if (!_TakeControl())
                return false;

            if (syncTrackCount == 0)
                return false;

            syncReadyUrl = _GetTrackURL(0);
            syncReadyQuestUrl = _GetTrackURL(0, TXLUrlType.Quest);
            syncReadyTitle = _GetTrackName(0);
            syncReadyPlayer = _GetTrackPlayer(0);

            _PopTracks(0, 1);

            _EventTrackChange();
            _EventUrlReady();

            errorCount = 0;
            syncReadyUrlUpdate += 1;
            syncTrackChangeUpdate += 1;

            RequestSerialization();

            return true;
        }

        void _PopTracks(int startAt = 0, int popCount = 1)
        {
            popCount = Math.Min(popCount, syncTrackCount);
            int limit = syncTrackCount - popCount;

            for (int i = startAt; i < limit; i++)
            {
                syncUrls[i] = syncUrls[i + popCount];
                syncEntries[i] = syncEntries[i + popCount];
                if (usingQuestUrls)
                    syncQuestUrls[i] = syncQuestUrls[i + popCount];
                if (usingTitles)
                    syncTitles[i] = syncTitles[i + popCount];
                if (usingAuthors)
                    syncAuthors[i] = syncAuthors[i + popCount];
                if (usingPlayers)
                    syncPlayers[i] = syncPlayers[i + popCount];
            }

            for (int i = limit; i < syncTrackCount; i++)
            {
                syncUrls[i] = VRCUrl.Empty;
                syncEntries[i] = new Vector3(-1, -1, -1);
                if (usingQuestUrls)
                    syncQuestUrls[i] = VRCUrl.Empty;
                if (usingTitles)
                    syncTitles[i] = "";
                if (usingAuthors)
                    syncAuthors[i] = "";
                if (usingPlayers)
                    syncPlayers[i] = "";
            }

            syncTrackCount -= (short)popCount;
            syncQueueUpdate += 1;

            _EventListChange();
        }

        public override bool _MovePrev()
        {
            return false;
        }

        public override bool _MoveTo(int index)
        {
            return false;
        }

        public override void OnDeserialization()
        {
            base.OnDeserialization();

            if (syncQueueUpdate != prevQueueUpdate)
            {
                prevQueueUpdate = syncQueueUpdate;
                _PopulateResolver();

                _EventListChange();
            }

            if (syncTrackChangeUpdate != prevTrackChangeUpdate)
            {
                prevTrackChangeUpdate = syncTrackChangeUpdate;
                _EventTrackChange();
            }

            if (syncReadyUrlUpdate != prevReadyUrlUpdate)
            {
                errorCount = 0;
                prevReadyUrlUpdate = syncReadyUrlUpdate;
                _EventUrlReady();
            }
        }

        void _PopulateResolver()
        {
            if (!usingTitles && !usingAuthors)
                return;

            UrlInfoResolver resolver = videoPlayer.UrlInfoResolver;
            if (!resolver)
                return;

            for (int i = 0; i < syncTrackCount; i++)
            {
                if (syncEntries[i].x == -1)
                    continue;

                bool hasTitle = usingTitles && syncTitles[i] != "";
                bool hasAuthor = usingAuthors && syncAuthors[i] != "";
                if (!hasTitle && !hasAuthor)
                    continue;

                VRCUrl url = syncUrls[i];
                if (resolver._HasInfo(url))
                    continue;

                resolver._AddInfo(url, usingTitles ? syncTitles[i] : null, usingAuthors ? syncAuthors[i] : null);
            }
        }

        public bool _RemoveTrack(int index)
        {
            if (index < 0 || index >= syncTrackCount)
                return false;

            if (!_TakeControl(deleteAccess))
                return false;

            _PopTracks(index, 1);
            RequestSerialization();

            return true;
        }

        public bool _MoveTrack(int index, int destIndex)
        {
            if (index < 0 || index >= syncTrackCount)
                return false;
            if (destIndex < 0 || destIndex >= syncTrackCount)
                return false;
            if (index == destIndex)
                return false;

            if (!_TakeControl(priorityAccess))
                return false;

            VRCUrl dstUrl = syncUrls[index];
            Vector3 dstEntry = syncEntries[index];
            VRCUrl dstQuestUrl = VRCUrl.Empty;
            if (usingQuestUrls)
                dstQuestUrl = syncQuestUrls[index];
            string dstTitle = "";
            if (usingTitles)
                dstTitle = syncTitles[index];
            string dstAuthor = "";
            if (usingAuthors)
                dstAuthor = syncAuthors[index];
            string dstPlayer = "";
            if (usingPlayers)
                dstPlayer = syncPlayers[index];

            if (destIndex < index)
            {
                for (int i = index; i > destIndex; i--)
                {
                    syncUrls[i] = syncUrls[i - 1];
                    syncEntries[i] = syncEntries[i - 1];
                    if (usingQuestUrls)
                        syncQuestUrls[i] = syncQuestUrls[i - 1];
                    if (usingTitles)
                        syncTitles[i] = syncTitles[i - 1];
                    if (usingAuthors)
                        syncAuthors[i] = syncAuthors[i - 1];
                    if (usingPlayers)
                        syncPlayers[i] = syncPlayers[i - 1];
                }
            } else
            {
                for (int i = index; i < destIndex; i++)
                {
                    syncUrls[i] = syncUrls[i + 1];
                    syncEntries[i] = syncEntries[i + 1];
                    if (usingQuestUrls)
                        syncQuestUrls[i] = syncQuestUrls[i + 1];
                    if (usingTitles)
                        syncTitles[i] = syncTitles[i + 1];
                    if (usingAuthors)
                        syncAuthors[i] = syncAuthors[i + 1];
                    if (usingPlayers)
                        syncPlayers[i] = syncPlayers[i + 1];
                }
            }

            syncUrls[destIndex] = dstUrl;
            syncEntries[destIndex] = dstEntry;
            if (usingQuestUrls)
                syncQuestUrls[destIndex] = dstQuestUrl;
            if (usingTitles)
                syncTitles[destIndex] = dstTitle;
            if (usingAuthors)
                syncAuthors[destIndex] = dstAuthor;
            if (usingPlayers)
                syncPlayers[destIndex] = dstPlayer;

            syncQueueUpdate += 1;
            _EventListChange();

            RequestSerialization();

            return true;
        }

        public override bool _CanAddTrack()
        {
            return true;
        }

        public override bool _AddTrack(VRCUrl url)
        {
            return _AddTrack(url, VRCUrl.Empty, "");
        }

        public bool _AddTrack(VRCUrl url, VRCUrl questUrl, string title)
        {
            return _AddTrack(url, questUrl, title, "");
        }

        public bool _AddTrack(VRCUrl url, VRCUrl questUrl, string title, string author)
        {
            if (!URLUtil.WellFormedUrl(url))
                return false;

            if (!_TakeControl())
                return false;

            _EnsureSyncCapacity();

            syncUrls[syncTrackCount] = url;
            if (usingQuestUrls)
                syncQuestUrls[syncTrackCount] = questUrl;

            syncEntries[syncTrackCount] = new Vector3(-1, -1, -1);

            if (usingTitles || usingAuthors)
            {
                if (usingTitles && title != null && title != "")
                {
                    syncTitles[syncTrackCount] = title;
                    if (usingAuthors)
                        syncAuthors[syncTrackCount] = author != null ? author : "";
                }
                else
                    _updateOrResolveInfo(url, syncTrackCount);
            }

            if (usingPlayers)
            {
                VRCPlayerApi player = Networking.LocalPlayer;
                if (Utilities.IsValid(player))
                    syncPlayers[syncTrackCount] = Networking.LocalPlayer.displayName;
                else
                    syncPlayers[syncTrackCount] = "";
            }

            return _CommitAddTrack();
        }

        int _FindUrlIndex(VRCUrl url)
        {
            if (url == null)
                return -1;

            string val = url.Get();
            if (val == "")
                return -1;

            for (int i = 0; i < syncTrackCount; i++)
            {
                if (syncEntries[i].x > -1)
                    continue;
                if (syncUrls[i].Get() == val)
                    return i;
            }

            return -1;
        }

        void _updateOrResolveInfo(VRCUrl url, int index)
        {
            bool foundInfo = _UpdateInfoFromResolver(url, index);

            if (!foundInfo && (usingTitles || usingAuthors))
            {
                UrlInfoResolver resolver = videoPlayer.UrlInfoResolver;
                resolver._ResolveInfo(url);
            }
        }

        bool _UpdateInfoFromResolver(VRCUrl url, int index)
        {
            if (usingTitles || usingAuthors)
            {
                UrlInfoResolver resolver = videoPlayer.UrlInfoResolver;
                DataDictionary info = resolver._GetInfo(url);
                string infoTitle = null;
                string infoAuthor = null;

                if (info != null)
                {
                    infoTitle = resolver._GetTitle(info);
                    infoAuthor = resolver._GetAuthor(info);
                }

                if (usingTitles)
                    syncTitles[index] = infoTitle ?? "";
                if (usingAuthors)
                    syncAuthors[index] = infoAuthor ?? "";

                return info != null;
            }

            return false;
        }

        public bool _AddTrack(int playlistIndex, int catalogIndex, int trackIndex)
        {
            if (!_TakeControl())
                return false;

            _EnsureSyncCapacity();

            syncUrls[syncTrackCount] = VRCUrl.Empty;
            if (usingQuestUrls)
                syncQuestUrls[syncTrackCount] = VRCUrl.Empty;

            syncEntries[syncTrackCount] = new Vector3(playlistIndex, catalogIndex, trackIndex);

            if (usingTitles)
                syncTitles[syncTrackCount] = "";
            if (usingAuthors)
                syncAuthors[syncTrackCount] = "";

            if (usingPlayers)
            {
                VRCPlayerApi player = Networking.LocalPlayer;
                if (Utilities.IsValid(player))
                    syncPlayers[syncTrackCount] = Networking.LocalPlayer.displayName;
                else
                    syncPlayers[syncTrackCount] = "";
            }

            return _CommitAddTrack();
        }

        private void _EnsureSyncCapacity()
        {
            if (syncTrackCount >= syncEntries.Length)
            {
                syncEntries = (Vector3[])UtilityTxl.ArrayMinSize(syncEntries, syncTrackCount + 1, typeof(Vector3));
                syncUrls = (VRCUrl[])UtilityTxl.ArrayMinSize(syncUrls, syncTrackCount + 1, typeof(VRCUrl));

                if (usingQuestUrls)
                    syncQuestUrls = (VRCUrl[])UtilityTxl.ArrayMinSize(syncQuestUrls, syncTrackCount + 1, typeof(VRCUrl));
                if (usingTitles)
                    syncTitles = (string[])UtilityTxl.ArrayMinSize(syncTitles, syncTrackCount + 1, typeof(string));
                if (usingAuthors)
                    syncAuthors = (string[])UtilityTxl.ArrayMinSize(syncAuthors, syncTrackCount + 1, typeof(string));
                if (usingPlayers)
                    syncPlayers = (string[])UtilityTxl.ArrayMinSize(syncPlayers, syncTrackCount + 1, typeof(string));
            }
        }

        private bool _CommitAddTrack()
        {
            syncQueueUpdate += 1;
            syncTrackCount += 1;

            bool isPlaying = videoPlayer && (videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_LOADING || videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING);

            if (Networking.IsOwner(gameObject))
            {
                _EventListChange();
                _EventInterupt();
            }

            RequestSerialization();

            return true;
        }

        bool _TakeControl(AccessControl acl = null)
        {
            if (acl && !acl._LocalHasAccess())
                return false;
            if (!acl && videoPlayer && videoPlayer.SupportsOwnership && !videoPlayer._TakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }

        TXLRepeatMode RepeatMode
        {
            get
            {
                if (videoPlayer)
                    return videoPlayer.RepeatMode;

                return TXLRepeatMode.None;
            }
        }

        protected void _EventTrackChange()
        {
            if (sourceManager)
                sourceManager._OnSourceTrackChange(sourceIndex);

            _UpdateHandlers(EVENT_TRACK_CHANGE);
        }

        protected void _EventListChange()
        {
            _UpdateHandlers(EVENT_LIST_CHANGE);
        }

        protected void _EventInterupt()
        {
            if (sourceManager)
                sourceManager._OnSourceInterrupt(sourceIndex);

            _UpdateHandlers(EVENT_INTERRUPT);
        }
    }
}
