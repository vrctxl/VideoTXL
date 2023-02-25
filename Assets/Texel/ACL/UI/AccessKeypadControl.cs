
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum KeypadDisplayType
    {
        Digits,
        Bullets,
        Empty,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AccessKeypadControl : EventBase
    {
        public KeypadDisplayType keypadDisplay = KeypadDisplayType.Bullets;
        public int maxDigits = 6;
        public bool autoSubmit = false;

        [Header("UI")]
        public Text displayText;

        public const int EVENT_SUBMIT = 0;
        public const int EVENT_INPUT = 1;
        public const int EVENT_CLEAR = 2;
        public const int EVENT_COUNT = 3;

        int digitCount = 0;
        string codeBuffer = "";
        string codeSubmitted = "";

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            base._Init();
        }

        public string LastSubmittedCode
        {
            get { return codeSubmitted; }
        }

        public void _DisplayMessage(string message, float seconds)
        {
            if (displayText)
                displayText.text = message;

            SendCustomEventDelayedSeconds(nameof(_UpdateReadout), seconds);
        }

        public void _Handle0Button()
        {
            _HandleDigit(0);
        }

        public void _Handle1Button()
        {
            _HandleDigit(1);
        }

        public void _Handle2Button()
        {
            _HandleDigit(2);
        }

        public void _Handle3Button()
        {
            _HandleDigit(3);
        }

        public void _Handle4Button()
        {
            _HandleDigit(4);
        }

        public void _Handle5Button()
        {
            _HandleDigit(5);
        }

        public void _Handle6Button()
        {
            _HandleDigit(6);
        }

        public void _Handle7Button()
        {
            _HandleDigit(7);
        }

        public void _Handle8Button()
        {
            _HandleDigit(8);
        }

        public void _Handle9Button()
        {
            _HandleDigit(9);
        }

        void _HandleDigit(int digit)
        {
            if (digitCount >= maxDigits)
                return;

            digitCount += 1;
            codeBuffer += digit.ToString();

            _UpdateReadout();
            _UpdateHandlers(EVENT_INPUT);

            if (autoSubmit && digitCount == maxDigits)
                _HandleEnterButton();
        }

        public void _HandleEnterButton()
        {
            codeSubmitted = codeBuffer;

            _ResetNumeric();
            _UpdateHandlers(EVENT_SUBMIT, codeSubmitted);
        }

        public void _HandleClearButton()
        {
            _ResetNumeric();
            _UpdateHandlers(EVENT_CLEAR);
        }

        void _ResetNumeric()
        {
            digitCount = 0;
            codeBuffer = string.Empty;

            _UpdateReadout();
        }

        public void _UpdateReadout()
        {
            string text = string.Empty;
            switch (keypadDisplay)
            {
                case KeypadDisplayType.Digits:
                    text = codeBuffer;
                    break;
                case KeypadDisplayType.Bullets:
                    text = "".PadLeft(digitCount, '•');
                    break;
                case KeypadDisplayType.Empty:
                    break;
            }

            if (displayText)
                displayText.text = text;
        }
    }
}
