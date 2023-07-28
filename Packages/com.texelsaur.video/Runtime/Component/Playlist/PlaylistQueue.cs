
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlaylistQueue : VideoUrlSource
    {
        TXLVideoPlayer videoPlayer;

        public bool removeTracks = true;

        [UdonSynced]
        VRCUrl[] syncQueue;
        [UdonSynced]
        int syncTrackCount = 0;
        [UdonSynced]
        int syncCurrentIndex = 0;
        [UdonSynced, FieldChangeCallback("SourceEnabled")]
        bool syncEnabled = true;

        [UdonSynced]
        int syncQueueUpdate = 0;
        int prevQueueUpdate = 0;

        [UdonSynced]
        int syncTrackChangeUpdate = 0;
        int prevTrackChangeUpdate = 0;

        public const int EVENT_LIST_CHANGE = 0;
        public const int EVENT_TRACK_CHANGE = 1;
        public const int EVENT_OPTION_CHANGE = 2;
        public const int EVENT_BIND_VIDEOPLAYER = 3;
        const int EVENT_COUNT = 4;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            base._Init();
        }

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            this.videoPlayer = videoPlayer;
        }

        public override bool IsEnabled
        {
            get { return syncEnabled; }
        }

        public override bool IsValid
        {
            get { return syncEnabled && syncTrackCount > 0; }
        }

        public bool SourceEnabled
        {
            get { return syncEnabled; }
            set
            {
                syncEnabled = value;
            }
        }

        public override bool _CanMoveNext()
        {
            if (syncTrackCount == 0)
                return false;

            return CurrentIndex < syncTrackCount - 1 || _Repeats();
        }

        public override bool _CanMovePrev()
        {
            if (syncTrackCount == 0)
                return false;

            return CurrentIndex > 0 || _Repeats();
        }

        public override bool _CanMoveTo(int index)
        {
            return index >= 0 && index < syncTrackCount;
        }

        public override VRCUrl _GetCurrentUrl()
        {
            if (CurrentIndex < 0 || !IsEnabled || CurrentIndex >= syncTrackCount)
                return VRCUrl.Empty;

            return syncQueue[CurrentIndex];
        }

        public override bool _MoveNext()
        {
            if (!_TakeControl())
                return false;

            if (!removeTracks)
            {
                if (CurrentIndex < syncTrackCount - 1)
                    CurrentIndex += 1;
                else if (_Repeats())
                    CurrentIndex = 0;
                else
                    CurrentIndex = -1;

                RequestSerialization();
                return CurrentIndex >= 0;
            }

            int originalIndex = CurrentIndex;

            int shift = CurrentIndex + 1;
            int limit = syncTrackCount - shift;

            for (int i = 0; i < limit; i++)
                syncQueue[i] = syncQueue[i + shift];
            for (int i = limit; i < syncTrackCount; i++)
                syncQueue[i] = VRCUrl.Empty;

            syncTrackCount -= shift;
            syncQueueUpdate += 1;

            CurrentIndex = syncTrackCount > 0 ? 0 : -1;
            if (CurrentIndex >= 0 && Networking.IsOwner(gameObject))
                _UpdateHandlers(EVENT_LIST_CHANGE);

            if (CurrentIndex != originalIndex)
            {
                syncTrackChangeUpdate += 1;
                if (Networking.IsOwner(gameObject))
                    _UpdateHandlers(EVENT_TRACK_CHANGE);
            }

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public override bool _MovePrev()
        {
            if (!_TakeControl())
                return false;

            if (!removeTracks)
            {
                if (CurrentIndex > 0)
                    CurrentIndex -= 1;
                else if (_Repeats())
                    CurrentIndex = syncTrackCount - 1;
                else
                    CurrentIndex = 01;

                RequestSerialization();
                return CurrentIndex >= 0;
            }

            return false;
        }

        public override bool _MoveTo(int index)
        {
            if (!_TakeControl())
                return false;

            if (!removeTracks)
            {
                if (index < 0 || index >= syncTrackCount)
                    return false;

                CurrentIndex = index;

                RequestSerialization();
                return CurrentIndex >= 0;
            }

            return false;
        }

        public int CurrentIndex
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

        public override void OnDeserialization()
        {
            base.OnDeserialization();

            if (syncQueueUpdate != prevQueueUpdate)
            {
                prevQueueUpdate = syncQueueUpdate;
                _UpdateHandlers(EVENT_LIST_CHANGE);
            }

            if (syncTrackChangeUpdate != prevTrackChangeUpdate)
            {
                prevTrackChangeUpdate = syncTrackChangeUpdate;
                _UpdateHandlers(EVENT_TRACK_CHANGE);
            }
        }

        public void _AddTrack(VRCUrl url)
        {
            _AddTrack(url, VRCUrl.Empty, "");
        }

        public void _AddTrack(VRCUrl url, VRCUrl questUrl, string title)
        {
            if (!_TakeControl())
                return;

            if (syncTrackCount >= syncQueue.Length)
                syncQueue = (VRCUrl[])UtilityTxl.ArrayMinSize(syncQueue, syncTrackCount + 1, url.GetType());

            syncQueue[syncTrackCount] = url;
            syncQueueUpdate += 1;
            syncTrackCount += 1;

            if (Networking.IsOwner(gameObject))
                _UpdateHandlers(EVENT_LIST_CHANGE);

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

        bool _Repeats()
        {
            if (videoPlayer)
                return videoPlayer.repeatPlaylist;

            return false;
        }
    }
}
