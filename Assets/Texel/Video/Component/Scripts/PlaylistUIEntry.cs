
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistUIEntry : UdonSharpBehaviour
    {
        public PlaylistUI playlistUI;
        public int track = 0;

        public Text selectedText;
        public Text unselectedText;
        public Text urlText;
        public Image tracker;
        public Image trackerFill;

        string title;
        string url;
        bool selected;
        float trackProgress;

        public void _Select()
        {
            playlistUI._SelectTrack(track);
        }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;

                if (Utilities.IsValid(selectedText))
                    selectedText.gameObject.SetActive(selected);
                if (Utilities.IsValid(unselectedText))
                    unselectedText.gameObject.SetActive(!selected);

                if (Utilities.IsValid(tracker))
                    tracker.gameObject.SetActive(selected);
                if (Utilities.IsValid(trackerFill))
                {
                    trackerFill.gameObject.SetActive(selected);
                    trackerFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
                }
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;

                if (Utilities.IsValid(selectedText))
                    selectedText.text = title;
                if (Utilities.IsValid(unselectedText))
                    unselectedText.text = title;
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
    }
}