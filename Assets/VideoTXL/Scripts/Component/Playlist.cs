
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Component/Playlist")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Playlist : UdonSharpBehaviour
    {
        public SyncPlayer syncPlayer;

        public bool shuffle;

        public VRCUrl[] playlist;

        [UdonSynced]
        bool syncEnabled;
        [UdonSynced]
        byte syncCurrentIndex;
        [UdonSynced]
        byte[] syncTrackerOrder;
        [UdonSynced]
        bool syncShuffle;

        bool end = false;

        [NonSerialized]
        public bool playlistEnabled;
        [NonSerialized]
        public int trackCount;
        [NonSerialized]
        public int currentIndex;

        private void Start()
        {
            trackCount = playlist.Length;
        }

        public void _Init()
        {
            syncEnabled = true;

            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            syncShuffle = shuffle;
            syncTrackerOrder = new byte[playlist.Length];
            for (int i = 0; i < syncTrackerOrder.Length; i++)
                syncTrackerOrder[i] = (byte)i;

            if (syncShuffle)
                _Shuffle();

            syncCurrentIndex = 0;
            RequestSerialization();
            _UpdateLocal();
        }

        public bool _HasNextTrack()
        {
            return syncCurrentIndex < trackCount - 1 || syncPlayer.repeatPlaylist;
        }

        public bool _HasPrevTrack()
        {
            return syncCurrentIndex > 0 || syncPlayer.repeatPlaylist;
        }

        public bool _MoveNext()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (syncCurrentIndex < playlist.Length - 1)
                syncCurrentIndex += 1;
            else if (syncPlayer.repeatPlaylist)
                syncCurrentIndex = 0;
            else
            {
                end = true;
                return false;
            }

            RequestSerialization();
            _UpdateLocal();

            return true;
        }

        public bool _MovePrev()
        {
            if (!syncPlayer._TakeControl())
                return false;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (syncCurrentIndex > 0)
                syncCurrentIndex -= 1;
            else if (syncPlayer.repeatPlaylist)
                syncCurrentIndex = (byte)(playlist.Length - 1);
            else
                return false;

            if (end)
                end = false;

            RequestSerialization();
            _UpdateLocal();

            return true;
        }

        public VRCUrl _GetCurrent()
        {
            if (end || !syncEnabled)
                return VRCUrl.Empty;

            int index = syncTrackerOrder[syncCurrentIndex];
            return playlist[index];
        }

        public void _SetEnabled(bool state)
        {
            if (!syncPlayer._TakeControl())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            syncEnabled = state;
            RequestSerialization();
            _UpdateLocal();
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

            syncShuffle = state;
            if (syncShuffle)
                _Shuffle();

            RequestSerialization();
            _UpdateLocal();
        }

        public override void OnDeserialization()
        {
            _UpdateLocal();
        }

        void _UpdateLocal()
        {
            playlistEnabled = syncEnabled;
            currentIndex = syncCurrentIndex;
        }

        void _Shuffle()
        {
            int[] temp = new int[trackCount];
            for (int i = 0; i < trackCount; i++)
                temp[i] = i;

            Utilities.ShuffleArray(temp);
            for (int i = 0; i < trackCount; i++)
                syncTrackerOrder[i] = (byte)temp[i];
        }
    }
}
