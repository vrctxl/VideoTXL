using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistUIEntry : UdonSharpBehaviour
    {
        [HideInInspector] public PlaylistUI playlistUI;
        [HideInInspector] public int track = 0;

        public Text trackNoText;
        public Text titleText;
        public Text urlText;
        public Image tracker;
        public Image trackerFill;
        public Button playButton;
        public Button addQueueButton;

        public Color selectedActiveColor;
        public Color selectedInactiveColor;
        public Color unselectedColor;

        string title;
        string url;
        bool selected;
        bool playback;
        float trackProgress;

        Image playImage;
        Image addQueueImage;

        private void Start()
        {
            if (playButton)
            {
                playImage = playButton.GetComponentInChildren<Image>();
                playButton.gameObject.SetActive(false);
            }

            if (addQueueButton)
            {
                addQueueImage = addQueueButton.GetComponentInChildren<Image>();
                addQueueButton.gameObject.SetActive(false);
            }
        }

        public void _Select()
        {
            playlistUI._SelectTrack(track);
        }

        public void _Enqueue()
        {
            playlistUI._EnqueueTrack(track);
        }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                playback = value;

                _UpdateRow();
            }
        }

        public bool Playback
        {
            get { return playback; }
            set
            {
                playback = value;

                _UpdateRow();
            }
        }

        void _UpdateRow()
        {
            if (Utilities.IsValid(titleText))
                titleText.color = selected ? (playback ? selectedActiveColor : selectedInactiveColor) : unselectedColor;

            if (Utilities.IsValid(tracker))
                tracker.gameObject.SetActive(selected && playback);
            if (Utilities.IsValid(trackerFill))
            {
                trackerFill.gameObject.SetActive(selected && playback);
                trackerFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            }
        }

        public int Track
        {
            get { return track; }
            set
            {
                track = value;

                if (trackNoText)
                    trackNoText.text = (track + 1).ToString();
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;

                if (Utilities.IsValid(titleText))
                    titleText.text = title;
            }
        }

        public string Url
        {
            get { return url; }
            set
            {
                url = value;

                if (Utilities.IsValid(urlText))
                    urlText.text = url;
            }
        }

        public float TrackProgress
        {
            get { return trackProgress; }
            set
            {
                trackProgress = value;

                if (Utilities.IsValid(tracker) && Utilities.IsValid(trackerFill))
                {
                    float w = tracker.rectTransform.rect.width;
                    trackerFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * trackProgress);
                }
            }
        }

        public void _PointerEnter()
        {
            if (playButton)
                playButton.gameObject.SetActive(true);

            if (addQueueButton && playlistUI && playlistUI.playlist.TargetQueue)
                addQueueButton.gameObject.SetActive(true);
        }

        public void _PointerExit()
        {
            if (playButton)
                playButton.gameObject.SetActive(false);

            if (addQueueButton)
                addQueueButton.gameObject.SetActive(false);
        }
    }
}