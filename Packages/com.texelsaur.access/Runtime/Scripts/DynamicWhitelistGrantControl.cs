
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicWhitelistGrantControl : ControlBase
    {
        public DynamicWhitelistGrant whitelistGrant;

        [Header("UI")]
        public GameObject requestButton;
        public GameObject grantButton;
        public GameObject denyButton;
        public Text userText;

        const int UI_BUTTON_REQUEST = 0;
        const int UI_BUTTON_GRANT = 1;
        const int UI_BUTTON_DENY = 2;
        const int UI_BUTTON_COUNT = 3;

        private void Start()
        {
            _EnsureInit();
        }

        protected override int ButtonCount => UI_BUTTON_COUNT;

        protected override void _Init()
        {
            _DiscoverButton(UI_BUTTON_REQUEST, requestButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_GRANT, grantButton, COLOR_GREEN);
            _DiscoverButton(UI_BUTTON_DENY, denyButton, COLOR_RED);

            if (whitelistGrant)
            {
                whitelistGrant._Register(DynamicWhitelistGrant.EVENT_REQUEST_CHANGE, this, nameof(_OnRequestChange));
                if (whitelistGrant.grantACL)
                    whitelistGrant.grantACL._Register(AccessControl.EVENT_VALIDATE, this, nameof(_OnAccessValidate));
            }

            _UpdateButtonState();
            _UpdateUserText();
        }

        public void _OnRequestChange()
        {
            _UpdateButtonState();
            _UpdateUserText();
        }

        public void _OnAccessValidate()
        {
            _UpdateButtonState();
        }

        public void _HandleRequestButton()
        {
            if (!whitelistGrant || !whitelistGrant.request)
                return;

            if (!whitelistGrant.grantUsersCanRequest)
            {
                bool admin = true;
                if (whitelistGrant.grantACL)
                    admin = whitelistGrant.grantACL._LocalHasAccess();

                if (admin)
                    return;
            }

            whitelistGrant.request._Request();
        }

        public void _HandleGrantButton()
        {
            if (whitelistGrant)
                whitelistGrant._Grant();
        }

        public void _HandleDenyButton()
        {
            if (whitelistGrant)
                whitelistGrant._Deny();
        }

        void _UpdateUserText()
        {
            userText.text = "";

            if (!whitelistGrant)
                return;

            if (whitelistGrant.CurrentRequest > -1)
            {
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(whitelistGrant.CurrentRequest);
                if (Utilities.IsValid(player))
                    userText.text = player.displayName;
            }
        }

        public void _UpdateButtonState()
        {
            if (!whitelistGrant)
            {
                _SetButton(UI_BUTTON_REQUEST, false);
                _SetButton(UI_BUTTON_GRANT, false);
                _SetButton(UI_BUTTON_DENY, false);

                return;
            }

            bool admin = true;
            if (whitelistGrant.grantACL)
                admin = whitelistGrant.grantACL._LocalHasAccess();

            bool activeRequest = false;
            bool latchRequest = whitelistGrant.request ? whitelistGrant.request.latchRequest : false;

            int reqId = whitelistGrant.CurrentRequest;
            if (reqId > -1)
            {
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(reqId);
                if (Utilities.IsValid(player))
                    activeRequest = true;
            }

            bool adminButtonState = admin && activeRequest;
            _SetButton(UI_BUTTON_GRANT, adminButtonState);
            _SetButton(UI_BUTTON_DENY, adminButtonState);

            bool playerInList = false;
            if (whitelistGrant.dynamicList)
            {
                if (Networking.LocalPlayer != null)
                    playerInList = whitelistGrant.dynamicList._ContainsName(Networking.LocalPlayer.displayName);
                else
                    playerInList = false;
            }

            bool requestButtonState = !playerInList && (!latchRequest || !activeRequest) && (!admin || whitelistGrant.grantUsersCanRequest);
            _SetButton(UI_BUTTON_REQUEST, requestButtonState);
        }
    }
}
