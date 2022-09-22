
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Playlist : EventBase
    {
        public SyncPlayer syncPlayer;

        [Header("Optional Components")]

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

        [Tooltip("Shuffle track order on load")]
        public bool shuffle;
        [Tooltip("Automatically advance to next track when finished playing")]
        public bool autoAdvance = true;
        [Tooltip("Hold videos in ready state until released by an external input")]
        public bool holdOnReady = false;
        [Tooltip("Treat tracks as independent, unlinked entities.  Disables normal playlist controls.")]
        public bool trackCatalogMode = false;

        [Tooltip("Optional catalog to sync load playlist data from")]
        public PlaylistCatalog playlistCatalog;
        [Tooltip("Default playlist track set")]
        public PlaylistData playlistData;

        VRCUrl[] playlist;
        VRCUrl[] questPlaylist;
        string[] trackNames;

        [UdonSynced, FieldChangeCallback("PlaylistEnabled")]
        bool syncEnabled;
        [UdonSynced, FieldChangeCallback("CurrentIndex")]
        short syncCurrentIndex = -1;
        [UdonSynced]
        byte[] syncTrackerOrder;
        [UdonSynced, FieldChangeCallback("ShuffleEnabled")]
        bool syncShuffle;
        [UdonSynced, FieldChangeCallback("CatalogueIndex")]
        int syncCatalogueIndex = -1;

        [NonSerialized]
        public int trackCount;
        
        public const int EVENT_LIST_CHANGE = 0;
        public const int EVENT_TRACK_CHANGE = 1;
        public const int EVENT_OPTION_CHANGE = 2;
        const int EVENT_COUNT = 3;

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            DebugLog("Common initialization");

            _InitHandlers(EVENT_COUNT);

            handlerCount = new int[eventCount];
            handlers = new Component[eventCount][];
            handlerEvents = new string[eventCount][];

            for (int i = 0; i < eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
            }

            syncShuffle = shuffle;
            _LoadDataLow(playlistData);
        }

        public void _LoadData(PlaylistData data)
        {
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _LoadDataLow(data);

            if (Utilities.IsValid(data))
                DebugLog($"Loading playlist data {data.playlistName}");
            else
                DebugLog("Loading empty playlist data");

            RequestSerialization();
        }

        public void _LoadFromCatalogueData(PlaylistData data)
        {
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

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

            CatalogueIndex = index;
            RequestSerialization();
        }

        public void _LoadFromCatalogueIndex(int index)
        {
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (!Utilities.IsValid(playlistCatalog) || playlistCatalog.PlaylistCount == 0)
                index = -1;
            if (index < 0 || index >= playlistCatalog.PlaylistCount)
                index = -1;

            CatalogueIndex = index;
            RequestSerialization();
        }

        void _LoadDataLow(PlaylistData data)
        {
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

            CurrentIndex = (short)((autoAdvance && !trackCatalogMode) ? 0 : -1);

            syncTrackerOrder = new byte[playlist.Length];
            if (syncShuffle)
                _Shuffle();
            else
            {
                for (int i = 0; i < syncTrackerOrder.Length; i++)
                    syncTrackerOrder[i] = (byte)i;
            }

            _UpdateHandlers(EVENT_LIST_CHANGE);
        }

        public void _MasterInit()
        {
            _EnsureInit();

            DebugLog("Master initialization");
            syncEnabled = true;

            if (Networking.IsOwner(syncPlayer.gameObject))
            {
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);

                if (syncShuffle)
                    _Shuffle();

                RequestSerialization();
            }
        }

        public short CurrentIndex
        {
            get { return syncCurrentIndex; }
            set
            {
                if (syncCurrentIndex != value)
                {
                    syncCurrentIndex = value;
                    _UpdateHandlers(EVENT_TRACK_CHANGE);
                }
            }
        }

        public bool PlaylistEnabled
        {
            get { return syncEnabled; }
            set
            {
                syncEnabled = value;
                _UpdateHandlers(EVENT_OPTION_CHANGE);
            }
        }

        public bool ShuffleEnabled
        {
            get { return syncShuffle; }
            set
            {
                syncShuffle = value;
                _UpdateHandlers(EVENT_OPTION_CHANGE);
            }
        }

        public int CatalogueIndex
        {
            get { return syncCatalogueIndex; }
            set
            {
                syncCatalogueIndex = value;

                if (!Utilities.IsValid(playlistCatalog) || value < 0 || value >= playlistCatalog.PlaylistCount)
                    return;

                _LoadDataLow(playlistCatalog.playlists[value]);
            }
        }

        public bool _HasNextTrack()
        {
            if (trackCatalogMode)
                return false;
            return CurrentIndex < trackCount - 1 || syncPlayer.repeatPlaylist;
        }

        public bool _HasPrevTrack()
        {
            if (trackCatalogMode)
                return false;
            return CurrentIndex > 0 || syncPlayer.repeatPlaylist;
        }

        public bool _MoveFirst()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            CurrentIndex = 0;

            DebugLog($"Move first track {CurrentIndex}");

            RequestSerialization();

            return true;
        }

        public bool _MoveNext()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (!trackCatalogMode && CurrentIndex < playlist.Length - 1)
                CurrentIndex += 1;
            else if (!trackCatalogMode && syncPlayer.repeatPlaylist)
                CurrentIndex = 0;
            else
                CurrentIndex = (short)-1;

            RequestSerialization();

            if (CurrentIndex >= 0)
                DebugLog($"Move next track {CurrentIndex}");
            else
                DebugLog($"Playlist completed");

            return CurrentIndex >= 0;
        }

        public bool _MovePrev()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (!trackCatalogMode && CurrentIndex >= 0)
                CurrentIndex -= 1;
            else if (!trackCatalogMode && syncPlayer.repeatPlaylist)
                CurrentIndex = (short)(playlist.Length - 1);
            else
                CurrentIndex = (short)-1;

            if (CurrentIndex >= 0)
                DebugLog($"Move previous track {CurrentIndex}");
            else
                DebugLog($"Playlist reset");

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public bool _MoveTo(int index)
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (index < -1 || index >= trackCount)
                return false;

            if (CurrentIndex == index)
            {
                if (syncEnabled)
                    return false;
            }

            syncEnabled = true;
            CurrentIndex = (short)index;

            if (CurrentIndex >= 0)
                DebugLog($"Move track to {CurrentIndex}");
            else
                DebugLog($"Playlist reset");

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public VRCUrl _GetCurrent()
        {
            if (CurrentIndex < 0 || !syncEnabled || CurrentIndex >= syncTrackerOrder.Length)
                return VRCUrl.Empty;

            int index = syncTrackerOrder[CurrentIndex];
            return playlist[index];
        }

        public VRCUrl _GetCurrentQuest()
        {
            if (CurrentIndex < 0 || !syncEnabled || CurrentIndex >= syncTrackerOrder.Length)
                return VRCUrl.Empty;

            int index = syncTrackerOrder[CurrentIndex];
            return questPlaylist[index];
        }

        public VRCUrl _GetTrackURL(int index)
        {
            if (index < 0 || index >= trackCount || index >= syncTrackerOrder.Length)
                return VRCUrl.Empty;

            index = syncTrackerOrder[index];
            return playlist[index];
        }

        public VRCUrl _GetTrackQuestURL(int index)
        {
            if (index < 0 || index >= trackCount || index >= syncTrackerOrder.Length)
                return VRCUrl.Empty;

            index = syncTrackerOrder[index];
            return questPlaylist[index];
        }

        public string _GetTrackName(int index)
        {
            if (index < 0 || index >= trackCount || index >= syncTrackerOrder.Length)
                return "";

            index = syncTrackerOrder[index];
            return trackNames[index];
        }

        public void _SetEnabled(bool state)
        {
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

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
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            DebugLog($"Set shuffle mode {state}");

            ShuffleEnabled = state;
            if (syncShuffle)
            {
                _Shuffle();
                _UpdateHandlers(EVENT_LIST_CHANGE);
            }

            RequestSerialization();
        }

        void _Shuffle()
        {
            DebugLog("Shuffling track list");
            int[] temp = new int[trackCount];
            for (int i = 0; i < trackCount; i++)
                temp[i] = i;

            Utilities.ShuffleArray(temp);
            for (int i = 0; i < trackCount; i++)
                syncTrackerOrder[i] = (byte)temp[i];
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:Playlist] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("Playlist", message);
        }
    }
}
