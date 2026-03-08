
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
        public GameObject buttonContainer;
        public Button priorityButton;
        public Button deleteButton;
        public Button upButton;
        public Button downButton;

        string title;
        string url;
        string playerName;

        private void Start()
        {
            _PointerExit();
        }

        public void _SelectPriority()
        {
            queueUI._HandlePriority(track);
        }

        public void _SelectDelete()
        {
            queueUI._HandleDelete(track);
        }

        public void _SelectMoveUp()
        {
            queueUI._HandleMoveUp(track);
        }

        public void _SelectMoveDown()
        {
            queueUI._HandleMoveDown(track);
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
            if (deleteButton && queueUI._HasDeleteAccessFor(track))
                deleteButton.gameObject.SetActive(true);

            if (queueUI.HasMoveAccess)
            {
                if (upButton)
                    upButton.gameObject.SetActive(true);
                if (downButton)
                    downButton.gameObject.SetActive(true);
            }

            if (buttonContainer)
                buttonContainer.SetActive(true);
        }

        public void _PointerExit()
        {
            if (buttonContainer)
                buttonContainer.SetActive(false);

            if (priorityButton)
                priorityButton.gameObject.SetActive(false);
            if (deleteButton)
                deleteButton.gameObject.SetActive(false);
            if (upButton)
                upButton.gameObject.SetActive(false);
            if (downButton)
                downButton.gameObject.SetActive(false);
        }
    }
}