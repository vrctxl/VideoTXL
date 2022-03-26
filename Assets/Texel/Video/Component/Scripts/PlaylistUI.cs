
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

        PlaylistUIEntry[] entries;

        void Start()
        {
            SendCustomEventDelayedFrames("_InitUI", 1);
        }

        public void _InitUI()
        {
            entries = new PlaylistUIEntry[playlist.playlist.Length];

            for (int i = 0; i < playlist.playlist.Length; i++)
            {
                GameObject entry = VRCInstantiate(playlistEntryTemplate);

                PlaylistUIEntry script = (PlaylistUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                script.playlistUI = this;
                script.track = i;

                string url = playlist.playlist[i].ToString();
                string title = playlist.trackNames[i];
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

        public void _SelectTrack(int track)
        {
            Debug.Log($"Pressed {track}");

            int curTrack = playlist.currentIndex;
            if (Utilities.IsValid(entries[curTrack]))
            {
                PlaylistUIEntry entry = entries[curTrack];
                entry.Selected = false;
            }

            if (playlist._MoveTo(track))
            {
                playlist._SetEnabled(true);
                SyncPlayer videoPlayer = playlist.syncPlayer;
                if (videoPlayer.playlist.holdOnReady)
                    videoPlayer._HoldNextVideo();
                videoPlayer._ChangeUrl(playlist._GetCurrent());

                PlaylistUIEntry entry = entries[track];
                entry.Selected = true;
            }
        }
    }
}
