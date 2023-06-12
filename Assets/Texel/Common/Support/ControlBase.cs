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
        public const int COLOR_WHTIE = 4;

        Color activeYellow = Color.HSVToRGB(60 / 360f, .8f, .9f);
        Color activeRed = Color.HSVToRGB(0, .7f, .9f);
        Color activeGreen = Color.HSVToRGB(100 / 360f, .8f, .9f);
        Color activeCyan = Color.HSVToRGB(180 / 360f, .8f, .9f);
        Color activeWhite = Color.HSVToRGB(0, 0, .9f);

        Color activeYellowLabel = Color.HSVToRGB(60 / 360f, .8f, .5f);
        Color activeRedLabel = Color.HSVToRGB(0, .7f, .5f);
        Color activeGreenLabel = Color.HSVToRGB(110 / 360f, .8f, .5f);
        Color activeCyanLabel = Color.HSVToRGB(180 / 360f, .8f, .5f);
        Color activeWhiteLabel = Color.HSVToRGB(0, 0, .5f);

        Color inactiveYellow = Color.HSVToRGB(60 / 360f, .35f, .5f);
        Color inactiveRed = Color.HSVToRGB(0, .35f, .5f);
        Color inactiveGreen = Color.HSVToRGB(110 / 360f, .35f, .5f);
        Color inactiveCyan = Color.HSVToRGB(180 / 360f, .40f, .5f);
        Color inactiveWhite = Color.HSVToRGB(0, 0, .5f);

        Color inactiveYellowLabel = Color.HSVToRGB(60 / 360f, .35f, .2f);
        Color inactiveRedLabel = Color.HSVToRGB(0, .35f, .2f);
        Color inactiveGreenLabel = Color.HSVToRGB(110 / 360f, .35f, .2f);
        Color inactiveCyanLabel = Color.HSVToRGB(180 / 360f, .35f, .2f);
        Color inactiveWhiteLabel = Color.HSVToRGB(0, 0, .2f);

        Color[] colorLookupActive;
        Color[] colorLookupInactive;
        Color[] colorLookupDisabled;
        Color[] colorLookupActiveLabel;
        Color[] colorLookupInactiveLabel;

        Image[] buttonBackground;
        Image[] buttonIcon;
        Text[] buttonText;
        int[] buttonColorIndex;

        Slider[] sliders;
        InputField[] inputFields;

        bool init = false;
        bool controlsInit = false;

        protected virtual int ButtonCount { get; }
        protected virtual int SliderCount { get; }
        protected virtual int InputFieldCount { get; }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;
            _InitControls();
            _Init();
        }

        protected virtual void _Init() { }

        protected void _InitControls()
        {
            if (controlsInit)
                return;

            controlsInit = true;

            colorLookupActive = new Color[] { activeRed, activeYellow, activeGreen, activeCyan, activeWhite };
            colorLookupInactive = new Color[] { inactiveRed, inactiveYellow, inactiveGreen, inactiveCyan, inactiveWhite };
            colorLookupDisabled = new Color[] { inactiveRed, inactiveYellow, inactiveGreen, inactiveCyan, inactiveWhite };

            colorLookupActiveLabel = new Color[] { activeRedLabel, activeYellowLabel, activeGreenLabel, activeCyanLabel, activeWhiteLabel };
            colorLookupInactiveLabel = new Color[] { inactiveRedLabel, inactiveYellowLabel, inactiveGreenLabel, inactiveCyanLabel, inactiveWhiteLabel };

            int buttonCount = ButtonCount;

            buttonColorIndex = new int[ButtonCount];
            buttonBackground = new Image[ButtonCount];
            buttonIcon = new Image[ButtonCount];
            buttonText = new Text[ButtonCount];

            sliders = new Slider[SliderCount];
            inputFields = new InputField[InputFieldCount];
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

        protected void _SetButtonText(int buttonIndex, string value)
        {
            if (buttonIndex < 0 || buttonIndex >= ButtonCount)
                return;

            Text text = buttonText[buttonIndex];
            if (text)
                text.text = value;
        }

        protected void _DiscoverSlider(int index, GameObject slider)
        {
            if (!slider)
                return;

            sliders[index] = slider.GetComponent<Slider>();
        }

        protected Slider _GetSlider(int sliderIndex)
        {
            if (sliderIndex < 0 || sliderIndex >= SliderCount)
                return null;

            return sliders[sliderIndex];
        }

        protected void _DiscoverInputField(int index, GameObject inputField)
        {
            if (!inputField)
                return;

            inputFields[index] = inputField.GetComponent<InputField>();
        }

        protected InputField _GetInputField(int index)
        {
            if (index < 0 || index >= InputFieldCount)
                return null;

            return inputFields[index];
        }
    }
}
