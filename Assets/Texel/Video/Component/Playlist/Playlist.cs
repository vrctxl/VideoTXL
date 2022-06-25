
using System;
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("VideoTXL/Component/Playlist")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Playlist : UdonSharpBehaviour
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
        public bool catalogueMode = false;

        [Tooltip("Optional catalogue to sync load playlist data from")]
        public PlaylistCatalogue catalogue;
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

        bool init = false;

        [NonSerialized]
        public int trackCount;

        const int eventCount = 3;
        const int LIST_CHANGE_EVENT = 0;
        const int TRACK_CHANGE_EVENT = 1;
        const int OPTION_CHANGE_EVENT = 2;

        int[] handlerCount;
        Component[][] handlers;
        string[][] handlerEvents;

        private void Start()
        {
            _CommonInit();
        }

        void _CommonInit()
        {
            if (init)
                return;

            DebugLog("Common initialization");

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

            init = true;
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
            if (Utilities.IsValid(catalogue) && catalogue.PlaylistCount > 0)
            {
                for (int i = 0; i < catalogue.playlists.Length; i++)
                {
                    if (catalogue.playlists[i] == data)
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

            if (!Utilities.IsValid(catalogue) || catalogue.PlaylistCount == 0)
                index = -1;
            if (index < 0 || index >= catalogue.PlaylistCount)
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

            CurrentIndex = (short)((autoAdvance && !catalogueMode) ? 0 : -1);

            syncTrackerOrder = new byte[playlist.Length];
            if (syncShuffle)
                _Shuffle();
            else
            {
                for (int i = 0; i < syncTrackerOrder.Length; i++)
                    syncTrackerOrder[i] = (byte)i;
            }

            _UpdateHandlers(LIST_CHANGE_EVENT);
        }

        public void _Init()
        {
            _CommonInit();

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
                    _UpdateHandlers(TRACK_CHANGE_EVENT);
                }
            }
        }

        public bool PlaylistEnabled
        {
            get { return syncEnabled; }
            set
            {
                syncEnabled = value;
                _UpdateHandlers(OPTION_CHANGE_EVENT);
            }
        }

        public bool ShuffleEnabled
        {
            get { return syncShuffle; }
            set
            {
                syncShuffle = value;
                _UpdateHandlers(OPTION_CHANGE_EVENT);
            }
        }

        public int CatalogueIndex
        {
            get { return syncCatalogueIndex; }
            set
            {
                syncCatalogueIndex = value;

                if (!Utilities.IsValid(catalogue) || value < 0 || value >= catalogue.PlaylistCount)
                    return;

                _LoadDataLow(catalogue.playlists[value]);
            }
        }

        public bool _HasNextTrack()
        {
            if (catalogueMode)
                return false;
            return CurrentIndex < trackCount - 1 || syncPlayer.repeatPlaylist;
        }

        public bool _HasPrevTrack()
        {
            if (catalogueMode)
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

            if (!catalogueMode && CurrentIndex < playlist.Length - 1)
                CurrentIndex += 1;
            else if (!catalogueMode && syncPlayer.repeatPlaylist)
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

            if (!catalogueMode && CurrentIndex >= 0)
                CurrentIndex -= 1;
            else if (!catalogueMode && syncPlayer.repeatPlaylist)
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
                _UpdateHandlers(LIST_CHANGE_EVENT);
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

        public void _RegisterTrackChange(Component handler, string eventName)
        {
            _Register(TRACK_CHANGE_EVENT, handler, eventName);
        }

        public void _RegisterListChange(Component handler, string eventName)
        {
            _Register(LIST_CHANGE_EVENT, handler, eventName);
        }

        public void _RegisterOptionChange(Component handler, string eventName)
        {
            _Register(OPTION_CHANGE_EVENT, handler, eventName);
        }

        void _Register(int eventIndex, Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            _CommonInit();

            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                if (handlers[eventIndex][i] == handler)
                    return;
            }

            handlers[eventIndex] = (Component[])_AddElement(handlers[eventIndex], handler, typeof(Component));
            handlerEvents[eventIndex] = (string[])_AddElement(handlerEvents[eventIndex], eventName, typeof(string));

            handlerCount[eventIndex] += 1;
        }

        void _UpdateHandlers(int eventIndex)
        {
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
        }

        Array _AddElement(Array arr, object elem, Type type)
        {
            Array newArr;
            int count = 0;

            if (Utilities.IsValid(arr))
            {
                count = arr.Length;
                newArr = Array.CreateInstance(type, count + 1);
                Array.Copy(arr, newArr, count);
            }
            else
                newArr = Array.CreateInstance(type, 1);

            newArr.SetValue(elem, count);
            return newArr;
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:Playlist] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("Playlist", message);
        }
    }
}
