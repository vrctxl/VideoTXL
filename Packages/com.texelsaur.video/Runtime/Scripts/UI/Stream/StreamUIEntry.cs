
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum StreamUIEntryType
    {
        Default,
        Additional,
        Custom,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StreamUIEntry : UdonSharpBehaviour
    {
        [HideInInspector] public StreamUIEntryType entryType;
        [HideInInspector] public StreamUI streamUI;
        [HideInInspector] public int track = 0;

        public Text selectedText;
        public Text unselectedText;
        public Text urlText;
        public VRCUrlInputField urlInput;

        string title;
        string url;
        bool selected;

        void Start()
        {
            Selected = false;
        }

        public void _Select()
        {
            if (entryType == StreamUIEntryType.Default)
                streamUI._SelectDefault();
            else if (entryType == StreamUIEntryType.Additional)
                streamUI._SelectAdditional(track);
            else if (entryType == StreamUIEntryType.Custom)
                streamUI._SelectCustom();
        }

        public int Track
        {
            get { return track; }
            set { track = value; }
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
            }
        }

        public void _HandleUrlInput()
        {
            if (!streamUI || !urlInput)
                return;

            streamUI._SetCustomUrl(urlInput.GetUrl());
        }

        public void _SetUrlDisplay(VRCUrl url)
        {
            if (urlInput)
                urlInput.SetUrl(url);
        }
    }
}
