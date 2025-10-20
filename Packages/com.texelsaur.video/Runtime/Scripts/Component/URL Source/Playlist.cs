
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Playlist : VideoUrlSource
    {
        TXLVideoPlayer videoPlayer;

        [Header("Optional Components")]

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Tooltip("Shuffle track order on load")]
        public bool shuffle;
        [Tooltip("Automatically advance to next track when finished playing")]
        public bool autoAdvance = true;
        [Tooltip("Treat tracks as independent, unlinked entities.  Disables normal playlist controls.")]
        public bool trackCatalogMode = false;
        [Tooltip("When loading new playlist, immediately start playing first track even if another track is currently playing.")]
        public bool immediate = false;
        [Tooltip("Resume playing from the playlist after a manually loaded URL is finished.")]
        public bool resumeAfterLoad = false;

        [Tooltip("Optional catalog to sync load playlist data from")]
        public PlaylistCatalog playlistCatalog;
        [Tooltip("Default playlist track set")]
        public PlaylistData playlistData;
        [Tooltip("Optional queue playlist entries can be queued up on")]
        public PlaylistQueue queue;

        VRCUrl[] playlist;
        VRCUrl[] questPlaylist;
        string[] trackNames;

        [UdonSynced, FieldChangeCallback("PlaylistEnabled")]
        bool syncEnabled;
        [UdonSynced]
        short syncCurrentIndex = -1;
        [UdonSynced]
        short syncCurrentIndexSerial = 0;
        [UdonSynced]
        short syncPlaylistSerial = 0;
        [UdonSynced]
        byte[] syncTrackerOrder = new byte[0];
        [UdonSynced, FieldChangeCallback("ShuffleEnabled")]
        bool syncShuffle;
        [UdonSynced]
        short syncShuffleSerial = 0;
        [UdonSynced, FieldChangeCallback("CatalogueIndex")]
        int syncCatalogueIndex = -1;
        [UdonSynced, FieldChangeCallback("AutoAdvance")]
        bool syncAutoAdvance = true;

        short prevCurrentIndexSerial = 0;
        short prevPlaylistSerial = 0;
        short prevShuffleSerial = 0;
        int queueIndex = -1;
        int listChangeSerial = 0;
        int trackChangeSerial = 0;

        bool _initDeserialize = false;

        [NonSerialized]
        public int trackCount;

        [Obsolete("Use VideoUrlSource.EVENT_OPTION_CHANGE")]
        public new const int EVENT_OPTION_CHANGE = VideoUrlSource.EVENT_OPTION_CHANGE;
        [Obsolete("Use VideoUrlSource.EVENT_BIND_VIDEOPLAYER")]
        public new const int EVENT_BIND_VIDEOPLAYER = VideoUrlSource.EVENT_BIND_VIDEOPLAYER;

        public const int EVENT_LIST_CHANGE = VideoUrlSource.EVENT_COUNT + 0;
        public const int EVENT_TRACK_CHANGE = VideoUrlSource.EVENT_COUNT + 1;
        protected new const int EVENT_COUNT = VideoUrlSource.EVENT_COUNT + 2;

        protected override int EventCount => EVENT_COUNT;

        private void Start()
        {
            _EnsureInit();
        }

        //protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            base._Init();

            DebugLog("Common initialization");

            if (queue)
                queueIndex = queue._RegisterPlaylistSource(this);

            _LoadDataLow(playlistData);

            if (Networking.IsOwner(gameObject))
            {
                syncShuffle = shuffle;
                syncAutoAdvance = autoAdvance;
                RequestSerialization();
            }
            else
                SendCustomEventDelayedSeconds(nameof(_InitCheck), 5);
        }

        public void _MasterInit()
        {
            _EnsureInit();

            DebugLog("Master initialization");

            if (videoPlayer && videoPlayer.SupportsOwnership)
            {
                if (!Networking.IsOwner(videoPlayer.gameObject))
                    return;

                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            else if (!Networking.IsOwner(gameObject))
                return;

            syncEnabled = true;

            _PostLoadShuffle();
            syncShuffleSerial += 1;

            _EventListChange();

            _PostLoadTrack();

            RequestSerialization();
        }

        public void _InitCheck()
        {
            if (!_initDeserialize)
            {
                DebugLog("Deserialize not received in reasonable time");
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "RequestOwnerSync");
            }
        }

        public void RequestOwnerSync()
        {
            DebugLog("RequestOwnerSync");
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            this.videoPlayer = videoPlayer;
            if (queue)
                queue._SetVideoPlayer(videoPlayer);

            _PopulateCatalog(playlistCatalog);
            _PopulateInfo(playlistData);

            _MasterInit();

            _UpdateHandlers(VideoUrlSource.EVENT_BIND_VIDEOPLAYER);
        }

        public override string SourceDefaultName
        {
            get { return "PLAYLIST"; }
        }

        public override string TrackDisplay
        {
            get
            {
                return IsReady ? $"TRACK: {CurrentIndex + 1} / {Count}" : "";
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
            get { return syncEnabled && trackCount > 0; }
        }

        public override bool IsReady
        {
            get { return syncEnabled && trackCount > 0 && syncCurrentIndex >= 0; }
        }

        public PlaylistQueue TargetQueue
        {
            get { return queue; }
        }

        public string ListName
        {
            get { return playlistData ? playlistData.playlistName : ""; }
        }

        public int ListChangeSerial
        {
            get { return listChangeSerial; }
        }

        public int TrackChangeSerial
        {
            get { return trackChangeSerial; }
        }

        public override bool _CanMoveNext()
        {
            if (trackCatalogMode || trackCount == 0)
                return false;

            return CurrentIndex < trackCount - 1 || _Repeats();
        }

        public override bool _CanMovePrev()
        {
            if (trackCatalogMode || trackCount == 0)
                return false;

            return CurrentIndex > 0 || _Repeats();
        }

        public override bool _CanMoveTo(int index)
        {
            return index >= 0 && index < trackCount;
        }

        public void _LoadData(PlaylistData data)
        {
            //if (!_TakeControl())
            //    return;

            _PopulateInfo(data);
            _LoadDataLow(data);

            if (Utilities.IsValid(data))
                DebugLog($"Loading playlist data {data.playlistName}");
            else
                DebugLog("Loading empty playlist data");

            _EventListChange();
        }

        public void _LoadFromCatalogueData(PlaylistData data)
        {
            if (!_TakeControl())
                return;

            int index = -1;
            if (Utilities.IsValid(playlistCatalog) && playlistCatalog.PlaylistCount > 0)
            {
                for (int i = 0; i < playlistCatalog.playlists.Length; i++)
                {
                    if (playlistCatalog.playlists[i] == data)
                    {
                        index = i;
                        break;
                    }
                }
            }

            syncCatalogueIndex = index;
            syncPlaylistSerial += 1;

            _LoadSyncedCatalogIndex();
            CurrentIndex = -1;

            _PostLoadShuffle();
            _EventListChange();

            _PostLoadTrack();

            RequestSerialization();
        }

        public void _LoadFromCatalogueIndex(int index)
        {
            if (!_TakeControl())
                return;

            if (!Utilities.IsValid(playlistCatalog) || playlistCatalog.PlaylistCount == 0)
                index = -1;
            if (index < 0 || index >= playlistCatalog.PlaylistCount)
                index = -1;

            syncCatalogueIndex = index;
            syncPlaylistSerial += 1;

            _LoadSyncedCatalogIndex();
            CurrentIndex = -1;

            _PostLoadShuffle();
            _EventListChange();

            _PostLoadTrack();

            RequestSerialization();
        }

        void _LoadDataLow(PlaylistData data)
        {
            DebugLog($"LoadDataLow trackcount={(data && data.playlist != null ? data.playlist.Length : 0)}");
            playlistData = data;

            if (!Utilities.IsValid(data) || !Utilities.IsValid(data.playlist))
            {
                playlist = new VRCUrl[0];
                questPlaylist = new VRCUrl[0];
                trackNames = new string[0];
            }
            else
            {
                playlist = data.playlist;
                questPlaylist = data.questPlaylist;
                if (!Utilities.IsValid(questPlaylist) || questPlaylist.Length != playlist.Length)
                    questPlaylist = new VRCUrl[data.playlist.Length];

                trackNames = data.trackNames;
                if (!Utilities.IsValid(trackNames) || trackNames.Length != playlist.Length)
                    trackNames = new string[playlist.Length];
            }

            trackCount = playlist.Length;

            for (int i = 0; i < trackCount; i++)
            {
                if (!Utilities.IsValid(playlist[i]))
                    playlist[i] = VRCUrl.Empty;
                if (!Utilities.IsValid(questPlaylist[i]))
                    questPlaylist[i] = VRCUrl.Empty;
                if (!Utilities.IsValid(trackNames[i]))
                    trackNames[i] = $"Track {i + 1}";
            }
        }

        void _PostLoadShuffle()
        {
            if (syncShuffle)
            {
                syncTrackerOrder = new byte[playlist.Length];
                _Shuffle();
                syncShuffleSerial += 1;
            }
            else
                syncTrackerOrder = new byte[0];
        }

        void _PostLoadTrack()
        {
            CurrentIndex = (short)((AutoAdvance && !trackCatalogMode && immediate) ? 0 : -1);
            if (immediate)
                CurrentIndexSerial += 1;
            else
            {
                if (videoPlayer && (videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING || videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_LOADING))
                    CurrentIndex = -1;
                else
                    CurrentIndexSerial += 1;
            }
        }

        public short Count
        {
            get { return (short)playlist.Length; }
        }

        public short CurrentIndex
        {
            get { return syncCurrentIndex; }
            protected set
            {
                syncCurrentIndex = value;
            }
        }

        protected short CurrentIndexSerial
        {
            get { return syncCurrentIndexSerial; }
            set
            {
                syncCurrentIndexSerial = value;
                _EventTrackChange();

                if (IsReady)
                    _EventUrlReady();
            }
        }

        public bool PlaylistEnabled
        {
            get { return syncEnabled; }
            set
            {
                syncEnabled = value;
                _UpdateHandlers(VideoUrlSource.EVENT_OPTION_CHANGE);
            }
        }

        public bool ShuffleEnabled
        {
            get { return syncShuffle; }
            set
            {
                syncShuffle = value;
                _UpdateHandlers(VideoUrlSource.EVENT_OPTION_CHANGE);
            }
        }

        public int CatalogueIndex
        {
            get { return syncCatalogueIndex; }
        }

        public override bool AutoAdvance
        {
            get { return syncAutoAdvance; }
            set
            {
                syncAutoAdvance = value;
                _UpdateHandlers(VideoUrlSource.EVENT_OPTION_CHANGE);
            }
        }

        public override bool ResumeAfterLoad
        {
            get { return resumeAfterLoad; }
        }

        public override bool _MoveNext()
        {
            if (!_TakeControl())
                return false;

            if (!trackCatalogMode && CurrentIndex < playlist.Length - 1)
                CurrentIndex += 1;
            else if (!trackCatalogMode && _Repeats())
                CurrentIndex = 0;
            else
                CurrentIndex = (short)-1;

            CurrentIndexSerial += 1;

            RequestSerialization();

            if (CurrentIndex >= 0)
                DebugLog($"Move next track {CurrentIndex}");
            else
                DebugLog($"Playlist completed");

            return CurrentIndex >= 0;
        }

        public override bool _MovePrev()
        {
            if (!_TakeControl())
                return false;

            bool onSource = videoPlayer && videoPlayer.currentUrlSource == this;

            if (!trackCatalogMode && CurrentIndex >= 0)
            {
                if (onSource)
                    CurrentIndex -= 1;
            }
            else if (!trackCatalogMode && _Repeats())
                CurrentIndex = (short)(playlist.Length - 1);
            else
                CurrentIndex = (short)-1;

            if (CurrentIndex >= 0)
                DebugLog($"Move previous track {CurrentIndex}");
            else
                DebugLog($"Playlist reset");

            CurrentIndexSerial += 1;

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public override bool _MoveTo(int index)
        {
            if (!_TakeControl())
                return false;

            if (index < -1 || index >= trackCount)
                return false;

            //if (CurrentIndex == index)
            //{
            //    if (syncEnabled)
            //        return false;
            //}

            syncEnabled = true;
            CurrentIndex = (short)index;
            CurrentIndexSerial += 1;

            if (CurrentIndex >= 0)
                DebugLog($"Move track to {CurrentIndex}");
            else
                DebugLog($"Playlist reset");

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public override VRCUrl _GetCurrentUrl()
        {
            int index = _TrackIndex(CurrentIndex);
            if (CurrentIndex < 0 || !syncEnabled)
                return VRCUrl.Empty;

            return playlist[index];
        }

        public override VRCUrl _GetCurrentQuestUrl()
        {
            int index = _TrackIndex(CurrentIndex);
            if (index < 0 || !syncEnabled)
                return VRCUrl.Empty;

            return questPlaylist[index];
        }

        public VRCUrl _GetTrackURL(int index)
        {
            index = _TrackIndex(index);
            if (index < 0)
                return VRCUrl.Empty;

            return playlist[index];
        }

        public VRCUrl _GetTrackQuestURL(int index)
        {
            index = _TrackIndex(index);
            if (index < 0)
                return VRCUrl.Empty;

            return questPlaylist[index];
        }

        public string _GetTrackName(int index)
        {
            index = _TrackIndex(index);
            if (index < 0)
                return "";

            return trackNames[index];
        }

        int _TrackIndex(int index)
        {
            if (index < 0 || index >= trackCount)
                return -1;
            if (syncTrackerOrder == null || syncTrackerOrder.Length == 0)
                return index;

            return syncTrackerOrder[index];
        }

        public bool _Enqueue(int index)
        {
            if (!queue || !queue._CanAddTrack())
                return false;

            index = _TrackIndex(index);
            return queue._AddTrack(queueIndex, CatalogueIndex, index);
        }

        public void _SetEnabled(bool state)
        {
            if (!_TakeControl())
                return;

            DebugLog($"Set playlist enabled {state}");

            PlaylistEnabled = state;

            RequestSerialization();
        }

        public void _ToggleShuffle()
        {
            _SetShuffle(!syncShuffle);
        }

        public void _SetShuffle(bool state)
        {
            if (!_TakeControl())
                return;

            DebugLog($"Set shuffle mode {state}");

            bool listChange = state != ShuffleEnabled;

            ShuffleEnabled = state;
            if (syncShuffle)
            {
                _Shuffle();
                listChange = true;
            }

            if (listChange)
            {
                syncShuffleSerial += 1;
                _EventListChange();
            }

            RequestSerialization();
        }

        public void _SetAutoAdvance(bool state)
        {
            if (!_TakeControl())
                return;

            DebugLog($"Set auto advance {state}");

            AutoAdvance = state;
            RequestSerialization();
        }

        void _Shuffle()
        {
            DebugLog("Shuffling track list");
            int[] temp = new int[trackCount];
            for (int i = 0; i < trackCount; i++)
                temp[i] = i;

            Utilities.ShuffleArray(temp);
            if (syncTrackerOrder == null || syncTrackerOrder.Length != trackCount)
                syncTrackerOrder = new byte[trackCount];

            for (int i = 0; i < trackCount; i++)
                syncTrackerOrder[i] = (byte)temp[i];
        }

        bool _TakeControl()
        {
            if (videoPlayer && videoPlayer.SupportsOwnership && !videoPlayer._TakeControl())
                return false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            return true;
        }

        bool _Repeats()
        {
            if (videoPlayer)
                return videoPlayer.RepeatMode == TXLRepeatMode.All;

            return false;
        }

        void _LoadSyncedCatalogIndex()
        {
            DebugLog($"Load synced catalog index {syncCatalogueIndex}");

            PlaylistData data = null;
            if (playlistCatalog && syncCatalogueIndex >= 0 && syncCatalogueIndex < playlistCatalog.PlaylistCount)
                data = playlistCatalog.playlists[syncCatalogueIndex];

            _LoadDataLow(data);
        }

        void _PopulateCatalog(PlaylistCatalog catalog)
        {
            if (!catalog)
                return;

            foreach (PlaylistData list in catalog.playlists)
                _PopulateInfo(list);
        }

        void _PopulateInfo(PlaylistData data)
        {
            if (!data || !videoPlayer || !videoPlayer.UrlInfoResolver)
                return;

            UrlInfoResolver resolver = videoPlayer.UrlInfoResolver;
            for (int i = 0; i < data.playlist.Length; i++)
            {
                if (data.trackNames[i] != null && data.trackNames[i].Length > 0)
                    resolver._AddInfo(data.playlist[i], data.trackNames[i]);
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            //DebugLog($"Deserialize playlist={syncPlaylistSerial}, shuffle={syncShuffleSerial}, currentIndex={syncCurrentIndexSerial}");
            base.OnDeserialization(result);

            bool listChange = false;

            if (syncPlaylistSerial != prevPlaylistSerial)
            {
                prevPlaylistSerial = syncPlaylistSerial;
                _LoadSyncedCatalogIndex();
                listChange = true;
            }

            if (syncShuffleSerial != prevShuffleSerial)
            {
                prevShuffleSerial = syncShuffleSerial;
                listChange = true;
            }

            if (listChange)
                _EventListChange();

            if (syncCurrentIndexSerial != prevCurrentIndexSerial)
            {
                prevCurrentIndexSerial = syncCurrentIndexSerial;
                CurrentIndexSerial = syncCurrentIndexSerial;
            }
        }

        protected void _EventTrackChange()
        {
            trackChangeSerial += 1;

            if (sourceManager)
                sourceManager._OnSourceTrackChange(sourceIndex);

            _UpdateHandlers(EVENT_TRACK_CHANGE);
        }

        protected void _EventListChange()
        {
            listChangeSerial += 1;

            _UpdateHandlers(EVENT_LIST_CHANGE);
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:Playlist] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("Playlist", message);
        }
    }
}
