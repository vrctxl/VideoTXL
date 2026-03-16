using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistUI : VideoSourceUIBase
    {
        //public Playlist playlist;
        public Playlist playlist;
        public CatalogUI catalogUI;
        public GameObject playlistEntryTemplate;

        public bool showCatalog = true;
        public bool showTrackNames = true;

        public ScrollRect scrollRect;
        public GameObject layoutGroup;
        public Text titleText;

        bool initialized = false;
        TXLVideoPlayer backingVideoPlayer;
        //PlaylistData data;
        PlaylistUIEntry[] entries;
        RectTransform[] entriesRT;
        int lastListChangeSerial = -1;
        int lastSelectedEntry = -1;
        int catalogPreviewIndex = -1;

        void Start()
        {
            _Init();

            if (Utilities.IsValid(playlist))
                _InitFromPlaylist(playlist);
        }

        void _Init()
        {
            if (initialized)
                return;

            initialized = true;
            entries = new PlaylistUIEntry[0];
            entriesRT = new RectTransform[0];
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
            _Init();
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
            _Init();
            this.playlist = playlist;

            if (catalogUI)
                catalogUI._InitFromPlaylist(playlist, this);

            if (gameObject.activeInHierarchy)
            {
                _RegisterListeners();

                SendCustomEventDelayedFrames("_InitUI", 1);
            }
        }

        public void _InitUI()
        {
            _RebuildList();
        }

        private void OnEnable()
        {
            _Init();
            _RegisterListeners();

            if (playlist)
            {
                if (lastListChangeSerial < playlist.ListChangeSerial)
                {
                    lastListChangeSerial = playlist.ListChangeSerial;
                    _RebuildList();
                }
                else if (lastSelectedEntry != playlist.CurrentIndex)
                    _OnTrackChange();
            }
        }

        private void OnDisable()
        {
            _UnregisterListeners();
        }

        public int CatalogPreviewIndex
        {
            get { return catalogPreviewIndex; }
            set 
            {
                if (catalogPreviewIndex != value)
                {
                    catalogPreviewIndex = value;
                    _RebuildList();

                    if (catalogUI)
                        catalogUI._OnPreviewChange();
                }
            }
        }

        public void _OnBindVideoPlayer()
        {
            if (backingVideoPlayer)
            {
                backingVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
                backingVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_OnVideoInfo));
            }

            backingVideoPlayer = null;
            if (playlist && gameObject.activeInHierarchy)
            {
                backingVideoPlayer = playlist.VideoPlayer;
                backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
                backingVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_OnVideoInfo));
            }
        }

        void _RegisterListeners()
        {
            if (Utilities.IsValid(playlist))
            {
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
        }

        void _UnregisterListeners()
        {
            if (Utilities.IsValid(playlist))
            {
                playlist._Unregister(VideoUrlSource.EVENT_BIND_VIDEOPLAYER, this, nameof(_OnBindVideoPlayer));
                playlist._Unregister(Playlist.EVENT_LIST_CHANGE, this, nameof(_OnListChange));
                playlist._Unregister(Playlist.EVENT_TRACK_CHANGE, this, nameof(_OnTrackChange));
                playlist._Unregister(VideoUrlSource.EVENT_OPTION_CHANGE, this, nameof(_OnOptionChange));

                if (Utilities.IsValid(playlist.VideoPlayer))
                {
                    backingVideoPlayer = playlist.VideoPlayer;
                    backingVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
                    backingVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_OnVideoInfo));
                }
            }
        }

        public void _OnTrackChange()
        {
            _UnselectLastEntry();
            if (catalogPreviewIndex >= 0 && catalogPreviewIndex != playlist.CatalogueIndex)
                return;

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
            if (lastListChangeSerial < playlist.ListChangeSerial)
                _RebuildList();

            lastListChangeSerial = playlist.ListChangeSerial;
        }

        void _RebuildList()
        {
            layoutGroup.SetActive(false);
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
            layoutGroup.SetActive(true);

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
            {
                if (lastSelectedEntry > -1 && lastSelectedEntry < entries.Length)
                {
                    PlaylistUIEntry entry = entries[lastSelectedEntry];
                    if (Utilities.IsValid(entry))
                    {
                        entry.Playback = false;
                        if (!playlist._CanMoveNext())
                            entry.Selected = false;
                    }
                }
            }
            //_UnselectLastEntry();
        }

        public void _OnVideoTrackingUpdate()
        {
            if (backingVideoPlayer && backingVideoPlayer.currentUrlSource != playlist)
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
                bool selectResult = false;
                if (catalogPreviewIndex >= 0 && catalogPreviewIndex != playlist.CatalogueIndex)
                    selectResult = playlist._SelectTrackFromCatalog(catalogPreviewIndex, track);
                else
                    selectResult = playlist._SelectTrack(track);

                if (selectResult)
                    backingVideoPlayer._ChangeUrl(playlist._GetCurrentUrl(), playlist._GetCurrentQuestUrl());

                if (playlist.CatalogueIndex == catalogPreviewIndex)
                    catalogPreviewIndex = -1;
            }
            else
                Debug.LogWarning("[VideoTXL] Tried to select playlist track, but the playlist is not associated with a video player!");
        }

        public void _EnqueueTrack(int track)
        {
            if (catalogPreviewIndex >= 0 && catalogPreviewIndex != playlist.CatalogueIndex)
                playlist._EnqueueFromCatalog(catalogPreviewIndex, track);
            else
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
                child.SetActive(false);
                //Destroy(child);
            }

            //entries = new PlaylistUIEntry[0];
        }

        void _BuildList()
        {
            if (!Utilities.IsValid(playlist))
                return;

            int count = playlist.Count;
            PlaylistData pdata = null;

            if (playlist.Catalog && catalogPreviewIndex >= 0) {
                pdata = playlist.Catalog._GetPlaylist(catalogPreviewIndex);
                if (pdata)
                    count = pdata.playlist.Length;
            }

            entries = (PlaylistUIEntry[])UtilityTxl.ArrayMinSize(entries, count, typeof(UdonBehaviour));
            entriesRT = (RectTransform[])UtilityTxl.ArrayMinSize(entriesRT, count, typeof(RectTransform));

            for (int i = 0; i < count; i++)
            {
                PlaylistUIEntry script = entries[i];
                if (!script)
                {
                    GameObject newEntry = Instantiate(playlistEntryTemplate);
                    script = newEntry.GetComponent<PlaylistUIEntry>();
                    script.playlistUI = this;
                    script.Track = i;

                    entries[i] = script;

                    newEntry.transform.SetParent(layoutGroup.transform);
                    RectTransform rt = (RectTransform)newEntry.GetComponent(typeof(RectTransform));
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.localPosition = Vector3.zero;

                    entriesRT[i] = rt;
                }

                GameObject entry = script.gameObject;
                entry.SetActive(true);

                string url = "";
                string title = "";

                if (pdata)
                {
                    url = pdata.playlist[i].ToString();
                    title = pdata.trackNames[i];
                }
                else
                {
                    url = playlist._GetTrackURL(i).ToString();
                    title = playlist._GetTrackName(i);
                }

                if (!showTrackNames || title == null || title == "")
                    title = url;

                script.Title = title;
                script.Url = url;
                script.Selected = false;
            }
        }
    }
}
