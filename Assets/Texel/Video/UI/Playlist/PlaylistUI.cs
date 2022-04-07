
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistUI : UdonSharpBehaviour
    {
        public Playlist playlist;
        public GameObject playlistEntryTemplate;

        public bool showTrackNames = true;

        public ScrollRect scrollRect;
        public GameObject layoutGroup;
        public Text titleText;

        VideoPlayerProxy dataProxy;
        PlaylistData data;
        PlaylistUIEntry[] entries;
        RectTransform[] entriesRT;

        void Start()
        {
            entries = new PlaylistUIEntry[0];

            if (Utilities.IsValid(playlist))
                _InitFromPlaylist(playlist);
        }

        public void _InitFromPlayer(SyncPlayer player)
        {
            if (!Utilities.IsValid(player) || !Utilities.IsValid(player.playlist))
                return;

            _InitFromPlaylist(player.playlist);
        }

        public void _InitFromPlaylist(Playlist playlist)
        {
            if (Utilities.IsValid(playlist))
            {
                this.playlist = playlist;

                playlist._RegisterListChange(this, "_OnListChange");
                playlist._RegisterTrackChange(this, "_OnTrackChange");
                playlist._RegisterOptionChange(this, "_OnOptionChange");

                if (Utilities.IsValid(playlist.syncPlayer))
                {
                    dataProxy = playlist.syncPlayer.dataProxy;
                    dataProxy._RegisterEventHandler(this, "_VideoTrackingUpdate");
                }
            }

            SendCustomEventDelayedFrames("_InitUI", 1);
        }

        public void _InitUI()
        {
            _OnListChange();
        }

        public void _OnTrackChange()
        {
            _UnselectEntries();

            int track = playlist.CurrentIndex;
            if (track < 0 || track >= entries.Length)
                return;

            PlaylistUIEntry entry = entries[track];
            if (!Utilities.IsValid(entry))
                return;

            entry.Selected = true;

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
            _ClearList();

            if (!Utilities.IsValid(playlist))
                return;

            data = playlist.playlistData;

            if (Utilities.IsValid(titleText))
            {
                if (!Utilities.IsValid(data))
                    titleText.text = "";
                else
                    titleText.text = data.name;
            }

            _BuildList();
            _OnTrackChange();
        }

        public void _OnOptionChange()
        {
            if (!playlist.PlaylistEnabled)
                _UnselectEntries();
        }

        public void _VideoTrackingUpdate()
        {
            int track = playlist.CurrentIndex;
            if (track < 0 || track >= entries.Length)
                return;

            PlaylistUIEntry entry = entries[track];
            if (!Utilities.IsValid(entry))
                return;

            if (dataProxy.trackDuration > 0)
                entry.TrackProgress = dataProxy.trackPosition / dataProxy.trackDuration;
            else
                entry.TrackProgress = 0;
        }

        public void _SelectTrack(int track)
        {
            Debug.Log($"Pressed {track}");

            if (playlist._MoveTo(track))
            {
                playlist._SetEnabled(true);
                SyncPlayer videoPlayer = playlist.syncPlayer;
                if (playlist.holdOnReady)
                    videoPlayer._HoldNextVideo();
                videoPlayer._ChangeUrl(playlist._GetCurrent());
            }
        }

        void _UnselectEntries()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (Utilities.IsValid(entries[i]))
                    entries[i].Selected = false;
            }
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
            if (!Utilities.IsValid(data))
                return;

            entries = new PlaylistUIEntry[data.playlist.Length];
            entriesRT = new RectTransform[data.playlist.Length];

            for (int i = 0; i < data.playlist.Length; i++)
            {
                GameObject entry = VRCInstantiate(playlistEntryTemplate);

                PlaylistUIEntry script = (PlaylistUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                script.playlistUI = this;
                script.track = i;

                string url = "";
                string title = "";

                if (Utilities.IsValid(playlist))
                {
                    url = playlist._GetTrackURL(i).ToString();
                    title = playlist._GetTrackName(i);
                }
                else
                {
                    url = data.playlist[i].ToString();
                    title = data.trackNames[i];
                }

                if (!showTrackNames)
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
