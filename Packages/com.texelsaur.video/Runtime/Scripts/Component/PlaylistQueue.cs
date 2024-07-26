
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlaylistQueue : VideoUrlListSource
    {
        TXLVideoPlayer videoPlayer;

        public bool removeTracks = true;

        [UdonSynced]
        VRCUrl[] syncQueue;
        [UdonSynced]
        short syncTrackCount = 0;
        [UdonSynced]
        short syncCurrentIndex = -1;
        [UdonSynced, FieldChangeCallback("SourceEnabled")]
        bool syncEnabled = true;

        [UdonSynced]
        int syncQueueUpdate = 0;
        int prevQueueUpdate = 0;

        [UdonSynced]
        int syncTrackChangeUpdate = 0;
        int prevTrackChangeUpdate = 0;

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            syncQueue = new VRCUrl[0];
        }

        public override void _SetVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            Debug.Log($"<color='00FFFF'>[VideoTXL:PlaylistQueue]</color> _SetVideoPlayer {videoPlayer}");
            this.videoPlayer = videoPlayer;

            _UpdateHandlers(VideoUrlSource.EVENT_BIND_VIDEOPLAYER);
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

        public override short Count
        {
            get { return syncTrackCount; }
        }

        public override bool _CanMoveNext()
        {
            if (syncTrackCount == 0)
                return false;

            return CurrentIndex < syncTrackCount - 1 || RepeatMode != TXLRepeatMode.None;
        }

        public override bool _CanMovePrev()
        {
            if (syncTrackCount == 0)
                return false;

            return CurrentIndex > 0 || RepeatMode != TXLRepeatMode.None;
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

        public override VRCUrl _GetTrackURL(int index)
        {
            if (index < 0 || index >= syncTrackCount)
                return null;

            return syncQueue[index];
        }

        public override VRCUrl _GetTrackQuestURL(int index)
        {
            return null;
        }

        public override string _GetTrackName(int index)
        {
            return "";
        }

        public override void _PlayCurrentUrl()
        {
            /*if (!_TakeControl())
                return;

            if (!removeTracks || syncTrackCount <= 0)
                return;

            int originalIndex = CurrentIndex;

            short shift = (short)(CurrentIndex + 1);
            int limit = syncTrackCount - shift;

            for (int i = 0; i < limit; i++)
                syncQueue[i] = syncQueue[i + shift];
            for (int i = limit; i < syncTrackCount; i++)
                syncQueue[i] = VRCUrl.Empty;

            syncTrackCount -= shift;
            syncQueueUpdate += 1;

            CurrentIndex = -1;
            if (Networking.IsOwner(gameObject))
                _UpdateHandlers(EVENT_LIST_CHANGE);

            syncTrackChangeUpdate += 1;
            //if (Networking.IsOwner(gameObject))
            //    _UpdateHandlers(EVENT_TRACK_CHANGE);

            RequestSerialization();*/
        }

        public override bool _MoveNext()
        {
            Debug.Log($"<color='0x00FFFF'>[VideoTXL:PlaylistQueue]</color> _MoveNext");
            if (!_TakeControl())
                return false;

            if (!removeTracks)
            {
                TXLRepeatMode repeat = RepeatMode;
                if (CurrentIndex < syncTrackCount - 1)
                    CurrentIndex += 1;
                else if (repeat == TXLRepeatMode.All)
                    CurrentIndex = 0;
                else if (repeat == TXLRepeatMode.Single)
                    CurrentIndex = CurrentIndex;
                else
                    CurrentIndex = -1;

                RequestSerialization();
                return CurrentIndex >= 0;
            }

            int originalIndex = CurrentIndex;
            int nextIndex = originalIndex + 1;

            if (originalIndex > -1)
            {
                int limit = syncTrackCount - nextIndex;
                for (int i = 0; i < limit; i++)
                    syncQueue[i] = syncQueue[i + nextIndex];
                for (int i = limit; i < syncTrackCount; i++)
                    syncQueue[i] = VRCUrl.Empty;

                syncTrackCount -= (short)nextIndex;
                syncQueueUpdate += 1;

                _UpdateHandlers(EVENT_LIST_CHANGE);
            }

            nextIndex -= 1;
            if (nextIndex >= syncTrackCount)
                nextIndex = -1;

            if (nextIndex != originalIndex)
                CurrentIndex = (short)nextIndex;
            else
                _UpdateHandlers(EVENT_TRACK_CHANGE);

            syncTrackChangeUpdate += 1;

            RequestSerialization();

            return CurrentIndex >= 0;
        }

        public override bool _MovePrev()
        {
            if (!_TakeControl())
                return false;

            if (!removeTracks)
            {
                TXLRepeatMode repeat = RepeatMode;
                if (CurrentIndex > 0)
                    CurrentIndex -= 1;
                else if (repeat == TXLRepeatMode.Single)
                    CurrentIndex = CurrentIndex;
                else if (repeat == TXLRepeatMode.All)
                    CurrentIndex = (short)(syncTrackCount - 1);
                else
                    CurrentIndex = -1;

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

                CurrentIndex = (short)index;

                RequestSerialization();
                return CurrentIndex >= 0;
            }

            return false;
        }

        public override short CurrentIndex
        {
            get { return syncCurrentIndex; }
            protected set
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

            bool isPlaying = videoPlayer && (videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_LOADING || videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING);

            if (Networking.IsOwner(gameObject))
                _UpdateHandlers(EVENT_LIST_CHANGE);

            if (CurrentIndex == -1 && !isPlaying)
                CurrentIndex = 0;

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

        TXLRepeatMode RepeatMode
        {
            get
            {
                if (videoPlayer)
                    return videoPlayer.RepeatMode;

                return TXLRepeatMode.None;
            }
        }
    }
}
