
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

        public GameObject layoutGroup;

        VideoPlayerProxy dataProxy;
        PlaylistData data;
        PlaylistUIEntry[] entries;

        void Start()
        {
            entries = new PlaylistUIEntry[0];

            SendCustomEventDelayedFrames("_InitUI", 1);

            playlist._RegisterListChange(this, "_OnListChange");
            playlist._RegisterTrackChange(this, "_OnTrackChange");
            playlist._RegisterOptionChange(this, "_OnOptionChange");

            dataProxy = playlist.syncPlayer.dataProxy;
            dataProxy._RegisterEventHandler(this, "_VideoTrackingUpdate");
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
        }

        public void _OnListChange()
        {
            _ClearList();

            data = playlist.playlistData;
            _BuildList();
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
                if (videoPlayer.playlist.holdOnReady)
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

            for (int i = 0; i < data.playlist.Length; i++)
            {
                GameObject entry = VRCInstantiate(playlistEntryTemplate);

                PlaylistUIEntry script = (PlaylistUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                script.playlistUI = this;
                script.track = i;

                string url = data.playlist[i].ToString();
                string title = data.trackNames[i];
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
            }
        }
    }
}
