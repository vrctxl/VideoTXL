using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Texel
{
    public abstract class ControlBase : UdonSharpBehaviour
    {
        public const int COLOR_RED = 0;
        public const int COLOR_YELLOW = 1;
        public const int COLOR_GREEN = 2;
        public const int COLOR_CYAN = 3;

        Color activeYellow = Color.HSVToRGB(60 / 360f, .8f, .9f);
        Color activeRed = Color.HSVToRGB(0, .7f, .9f);
        Color activeGreen = Color.HSVToRGB(100 / 360f, .8f, .9f);

        Color activeYellowLabel = Color.HSVToRGB(60 / 360f, .8f, .5f);
        Color activeRedLabel = Color.HSVToRGB(0, .7f, .5f);
        Color activeGreenLabel = Color.HSVToRGB(110 / 360f, .8f, .5f);

        Color inactiveYellow = Color.HSVToRGB(60 / 360f, .35f, .35f);
        Color inactiveRed = Color.HSVToRGB(0, .35f, .35f);
        Color inactiveGreen = Color.HSVToRGB(110 / 360f, .35f, .35f);

        Color inactiveYellowLabel = Color.HSVToRGB(60 / 360f, .35f, .2f);
        Color inactiveRedLabel = Color.HSVToRGB(0, .35f, .2f);
        Color inactiveGreenLabel = Color.HSVToRGB(110 / 360f, .35f, .2f);

        Color[] colorLookupActive;
        Color[] colorLookupInactive;
        Color[] colorLookupDisabled;
        Color[] colorLookupActiveLabel;
        Color[] colorLookupInactiveLabel;

        Image[] buttonBackground;
        Image[] buttonIcon;
        Text[] buttonText;
        int[] buttonColorIndex;

        bool init = false;
        bool controlsInit = false;

        protected virtual int ButtonCount { get; }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;
            _InitControls();
            _Init();
        }

        protected virtual void _Init() { }

        protected void _InitControls() {
            if (controlsInit)
                return;

            controlsInit = true;

            colorLookupActive = new Color[] { activeRed, activeYellow, activeGreen };
            colorLookupInactive = new Color[] { inactiveRed, inactiveYellow, inactiveGreen };
            colorLookupDisabled = new Color[] { inactiveRed, inactiveYellow, inactiveGreen };

            colorLookupActiveLabel = new Color[] { activeRedLabel, activeYellowLabel, activeGreenLabel };
            colorLookupInactiveLabel = new Color[] { inactiveRedLabel, inactiveYellowLabel, inactiveGreenLabel };

            int buttonCount = ButtonCount;

            buttonColorIndex = new int[ButtonCount];
            buttonBackground = new Image[ButtonCount];
            buttonIcon = new Image[ButtonCount];
            buttonText = new Text[ButtonCount];
        }

        protected void _DiscoverButton(int index, GameObject button, int colorIndex)
        {
            if (!button)
                return;

            buttonColorIndex[index] = colorIndex;
            buttonBackground[index] = button.GetComponent<Image>();
            int childCount = button.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = button.transform.GetChild(i);
                if (!buttonIcon[index])
                    buttonIcon[index] = child.GetComponent<Image>();
                if (!buttonText[index])
                    buttonText[index] = child.GetComponent<Text>();
            }

            _SetButton(index, false);
        }

        protected void _SetButton(int buttonIndex, bool state)
        {
            if (buttonIndex < 0 || buttonIndex >= ButtonCount)
                return;

            int colorIndex = buttonColorIndex[buttonIndex];
            Image bg = buttonBackground[buttonIndex];
            if (bg)
                bg.color = state ? colorLookupActive[colorIndex] : colorLookupInactive[colorIndex];

            Image icon = buttonIcon[buttonIndex];
            if (icon)
                icon.color = state ? colorLookupActiveLabel[colorIndex] : colorLookupInactiveLabel[colorIndex];

            Text text = buttonText[buttonIndex];
            if (text)
                text.color = state ? colorLookupActiveLabel[colorIndex] : colorLookupInactiveLabel[colorIndex];
        }
    }
}
