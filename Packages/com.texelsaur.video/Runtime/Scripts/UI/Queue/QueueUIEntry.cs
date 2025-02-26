
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueUIEntry : UdonSharpBehaviour
    {
        [HideInInspector] public PlaylistQueueUI queueUI;
        [HideInInspector] public int track = 0;

        public Text trackNoText;
        public Text titleText;
        public Text infoText;
        public Button priorityButton;
        public Button deleteButton;

        string title;
        string url;
        string playerName;

        Image priorityImage;
        Image deleteImage;

        private void Start()
        {
            if (priorityButton)
            {
                priorityImage = priorityButton.GetComponentInChildren<Image>();
                priorityButton.gameObject.SetActive(false);
            }

            if (deleteButton)
            {
                deleteImage = deleteButton.GetComponentInChildren<Image>();
                deleteButton.gameObject.SetActive(false);
            }
        }

        public void _SelectPriority()
        {
            queueUI._HandlePriority(track);
        }

        public void _SelectDelete()
        {
            queueUI._HandleDelete(track);
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
                _UpdateInfoText();
            }
        }

        public string PlayerName
        {
            get { return playerName; }
            set
            {
                playerName = value;
                _UpdateInfoText();
            }
        }

        void _UpdateInfoText()
        {
            if (!infoText)
                return;

            bool hasName = playerName != null && playerName.Length > 0;
            bool hasUrl = url != null && url.Length > 0;

            if (hasName && hasUrl)
                infoText.text = $"Submitted By: {playerName}  -  {url}";
            else if (hasName)
                infoText.text = $"Submitted By: {playerName}";
            else if (hasUrl)
                infoText.text = url;
            else
                infoText.text = "";
        }

        public void _PointerEnter()
        {
            if (priorityButton && queueUI.HasPriorityAccess)
                priorityButton.gameObject.SetActive(true);

            if (deleteButton && queueUI.HasDeleteAccess)
                deleteButton.gameObject.SetActive(true);
        }

        public void _PointerExit()
        {
            if (priorityButton)
                priorityButton.gameObject.SetActive(false);

            if (deleteButton)
                deleteButton.gameObject.SetActive(false);
        }
    }
}