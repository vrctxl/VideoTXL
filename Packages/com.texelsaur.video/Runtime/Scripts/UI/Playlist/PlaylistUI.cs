
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistUI : VideoSourceUIBase
    {
        //public Playlist playlist;
        public Playlist playlist;
        public GameObject playlistEntryTemplate;

        public bool showTrackNames = true;

        public ScrollRect scrollRect;
        public GameObject layoutGroup;
        public Text titleText;

        TXLVideoPlayer backingVideoPlayer;
        //PlaylistData data;
        PlaylistUIEntry[] entries;
        RectTransform[] entriesRT;
        int lastSelectedEntry = -1;

        void Start()
        {
            entries = new PlaylistUIEntry[0];

            if (Utilities.IsValid(playlist))
                _InitFromPlaylist(playlist);
        }

        public override bool _CompatibleSource(VideoUrlSource source)
        {
            if (source == null)
                return false;

            Playlist playlist = source.GetComponent<Playlist>();
            return playlist != null;
        }

        public override void _SetSource(VideoUrlSource source)
        {
            if (source == null)
                return;

            Playlist listSource = source.GetComponent<Playlist>();
            if (source != listSource)
                return;

            _InitFromPlaylist(listSource);
        }

        /*public void _InitFromPlayer(SyncPlayer player)
        {
            if (!Utilities.IsValid(player) || !Utilities.IsValid(player.playlist))
                return;

            _InitFromPlaylist(player.playlist);
        }*/

        public void _InitFromPlaylist(Playlist playlist)
        {
            if (entries == null)
                entries = new PlaylistUIEntry[0];

            if (Utilities.IsValid(playlist))
            {
                this.playlist = playlist;

                playlist._Register(VideoUrlSource.EVENT_BIND_VIDEOPLAYER, this, nameof(_OnBindVideoPlayer));
                playlist._Register(Playlist.EVENT_LIST_CHANGE, this, nameof(_OnListChange));
                playlist._Register(Playlist.EVENT_TRACK_CHANGE, this, nameof(_OnTrackChange));
                playlist._Register(VideoUrlSource.EVENT_OPTION_CHANGE, this, nameof(_OnOptionChange));

                if (Utilities.IsValid(playlist.VideoPlayer))
                {
                    backingVideoPlayer = playlist.VideoPlayer;
                    backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
                    backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_OnVideoInfo));
                }
            }

            SendCustomEventDelayedFrames("_InitUI", 1);
        }

        public void _InitUI()
        {
            _RebuildList();
        }

        public void _OnBindVideoPlayer()
        {
            backingVideoPlayer = playlist.VideoPlayer;
            backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
            backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_OnVideoInfo));
        }

        public void _OnTrackChange()
        {
            _UnselectLastEntry();

            int track = playlist.CurrentIndex;
            if (track < 0 || track >= entries.Length)
                return;

            PlaylistUIEntry entry = entries[track];
            if (!Utilities.IsValid(entry))
                return;

            entry.Selected = true;
            lastSelectedEntry = track;

            Canvas.ForceUpdateCanvases();
            ScrollReposition(entriesRT[track]);
        }

        public void _ScrollToCurrentTrack()
        {
            int track = playlist.CurrentIndex;
            if (track < 0 || track >= entries.Length)
                return;

            ScrollReposition(entriesRT[track]);
        }

        void ScrollReposition(RectTransform target)
        {
            RectTransform contentPanel = (RectTransform)layoutGroup.transform.parent.gameObject.GetComponent(typeof(RectTransform));

            float scrollHeight = ((RectTransform)scrollRect.GetComponent(typeof(RectTransform))).rect.height;
            var targetHeight = target.rect.height;

            Vector2 contentPos = scrollRect.transform.InverseTransformPoint(contentPanel.position);
            Vector2 targetPos = scrollRect.transform.InverseTransformPoint(target.position);

            if (targetPos.y + (targetHeight / 2) > scrollHeight / 2)
                contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, contentPos.y - targetPos.y - (targetHeight / 2));
            if (targetPos.y - (targetHeight / 2) < -scrollHeight / 2)
                contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, contentPos.y - targetPos.y + (targetHeight / 2) - scrollHeight);
        }

        public void _OnListChange()
        {
            Debug.Log("TXL Event _OnListChange");
            _RebuildList();
        }

        void _RebuildList()
        {
            _ClearList();

            if (!Utilities.IsValid(playlist))
                return;

            //data = playlist.playlistData;

            if (Utilities.IsValid(titleText))
            {
                titleText.text = playlist.ListName;
                //if (!Utilities.IsValid(data))
                //    titleText.text = "";
                //else
                //    titleText.text = data.playlistName;
            }

            _BuildList();
            _OnTrackChange();
        }

        public void _OnOptionChange()
        {
            if (!playlist.IsEnabled)
                _UnselectLastEntry();
        }

        public void _OnVideoInfo()
        {
            if (backingVideoPlayer && backingVideoPlayer.currentUrlSource != playlist)
                _UnselectLastEntry();
        }

        public void _OnVideoTrackingUpdate()
        {
            if (backingVideoPlayer && backingVideoPlayer.SourceManager && backingVideoPlayer.SourceManager.ReadySource != playlist)
                return;

            int track = playlist.CurrentIndex;
            if (track < 0 || track >= entries.Length)
                return;

            PlaylistUIEntry entry = entries[track];
            if (!Utilities.IsValid(entry))
                return;

            if (backingVideoPlayer && backingVideoPlayer.trackDuration > 0)
                entry.TrackProgress = backingVideoPlayer.trackPosition / backingVideoPlayer.trackDuration;
            else
                entry.TrackProgress = 0;
        }

        public void _SelectTrack(int track)
        {
            if (backingVideoPlayer)
            {
                //playlist._SetEnabled(true);
                //SyncPlayer videoPlayer = playlist.syncPlayer;
                //if (playlist.holdOnReady)
                //    videoPlayer._HoldNextVideo();
                if (playlist._MoveTo(track))
                    backingVideoPlayer._ChangeUrl(playlist._GetCurrentUrl(), playlist._GetCurrentQuestUrl());
            }
            else
                Debug.LogWarning("[VideoTXL] Tried to select playlist track, but the playlist is not associated with a video player!");
        }

        public void _EnqueueTrack(int track)
        {
            playlist._Enqueue(track);
        }

        void _UnselectEntries()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (Utilities.IsValid(entries[i]))
                    entries[i].Selected = false;
            }
        }

        void _UnselectLastEntry()
        {
            if (lastSelectedEntry > -1 && lastSelectedEntry < entries.Length)
            {
                PlaylistUIEntry entry = entries[lastSelectedEntry];
                if (Utilities.IsValid(entry))
                    entry.Selected = false;
            }

            lastSelectedEntry = -1;
        }

        void _ClearList()
        {
            int entryCount = layoutGroup.transform.childCount;
            for (int i = 0; i < entryCount; i++)
            {
                GameObject child = layoutGroup.transform.GetChild(i).gameObject;
                Destroy(child);
            }

            entries = new PlaylistUIEntry[0];
        }

        void _BuildList()
        {
            if (!Utilities.IsValid(playlist))
                return;

            int count = playlist.Count;
            entries = new PlaylistUIEntry[count];
            entriesRT = new RectTransform[count];

            for (int i = 0; i < count; i++)
            {
                GameObject entry = Instantiate(playlistEntryTemplate);

                PlaylistUIEntry script = (PlaylistUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                script.playlistUI = this;
                script.Track = i;

                string url = "";
                string title = "";

                if (Utilities.IsValid(playlist))
                {
                    url = playlist._GetTrackURL(i).ToString();
                    title = playlist._GetTrackName(i);
                }
                //else
                //{
                //    url = data.playlist[i].ToString();
                //    title = data.trackNames[i];
                //}

                if (!showTrackNames || title == null || title == "")
                    title = url;

                script.Title = title;
                script.Url = url;
                script.Selected = false;

                entry.transform.SetParent(layoutGroup.transform);

                RectTransform rt = (RectTransform)entry.GetComponent(typeof(RectTransform));
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.localPosition = Vector3.zero;

                entries[i] = script;
                entriesRT[i] = rt;
            }
        }
    }
}
