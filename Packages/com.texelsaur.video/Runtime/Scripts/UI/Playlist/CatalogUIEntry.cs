
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CatalogUIEntry : UdonSharpBehaviour
    {
        public CatalogUI catalogtUI;
        public int index = 0;

        public Text selectedText;
        public Text unselectedText;

        string title;
        bool selected;

        public void _Select()
        {
            catalogtUI._SelectIndex(index);
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
    }
}