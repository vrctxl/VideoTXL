
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

        public Text titleText;

        public Color selectedColor;
        public Color unselectedColor;

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
                _UpdateRow();
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

        void _UpdateRow()
        {
            if (Utilities.IsValid(titleText))
                titleText.color = selected ? selectedColor : unselectedColor;
        }
    }
}