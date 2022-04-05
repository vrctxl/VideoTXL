
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

        [Tooltip("Optional catalogue to sync load playlist data from")]
        public PlaylistCatalogue catalogue;
        [Tooltip("Default playlist track set")]
        public PlaylistData playlistData;

        VRCUrl[] playlist;
        string[] trackNames;

        [UdonSynced, FieldChangeCallback("PlaylistEnabled")]
        bool syncEnabled;
        [UdonSynced, FieldChangeCallback("CurrentIndex")]
        byte syncCurrentIndex;
        [UdonSynced]
        byte[] syncTrackerOrder;
        [UdonSynced, FieldChangeCallback("ShuffleEnabled")]
        bool syncShuffle;
        [UdonSynced, FieldChangeCallback("CatalogueIndex")]
        int syncCatalogueIndex = -1;

        bool end = false;
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
                trackNames = new string[0];
            } else
            {
                playlist = data.playlist;
                trackNames = data.trackNames ?? new string[playlist.Length];
            }

            trackCount = playlist.Length;

            for (int i = 0; i < trackCount; i++)
            {
                if (!Utilities.IsValid(playlist[i]))
                    playlist[i] = VRCUrl.Empty;
                if (!Utilities.IsValid(trackNames[i]))
                    trackNames[i] = $"Track {i + 1}";
            }

            CurrentIndex = 0;

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

        public byte CurrentIndex
        {
            get { return syncCurrentIndex; }
            set
            {
                syncCurrentIndex = value;
                _UpdateHandlers(TRACK_CHANGE_EVENT);
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
            return CurrentIndex < trackCount - 1 || syncPlayer.repeatPlaylist;
        }

        public bool _HasPrevTrack()
        {
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

            if (CurrentIndex < playlist.Length - 1)
                CurrentIndex += 1;
            else if (syncPlayer.repeatPlaylist)
                CurrentIndex = 0;
            else
            {
                end = true;
                return false;
            }

            DebugLog($"Move next track {CurrentIndex}");

            RequestSerialization();

            return true;
        }

        public bool _MovePrev()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (CurrentIndex > 0)
                CurrentIndex -= 1;
            else if (syncPlayer.repeatPlaylist)
                CurrentIndex = (byte)(playlist.Length - 1);
            else
                return false;

            if (end)
                end = false;

            DebugLog($"Move previous track {CurrentIndex}");

            RequestSerialization();

            return true;
        }

        public bool _MoveTo(int index)
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (index < 0 || index >= trackCount)
                return false;

            if (CurrentIndex == index)
            {
                if (syncEnabled)
                    return false;
            }

            syncEnabled = true;
            CurrentIndex = (byte)index;

            if (end)
                end = false;

            DebugLog($"Move track to {CurrentIndex}");

            RequestSerialization();

            return true;
        }

        public VRCUrl _GetCurrent()
        {
            if (end || !syncEnabled)
                return VRCUrl.Empty;

            int index = syncTrackerOrder[CurrentIndex];
            return playlist[index];
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
                _Shuffle();

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
