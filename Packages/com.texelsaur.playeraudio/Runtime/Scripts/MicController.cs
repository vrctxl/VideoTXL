
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MicController : UdonSharpBehaviour
    {
        [Tooltip("ACL for the control box settings")]
        public AccessControl controlAccess;
        [Tooltip("ACL for using and holding the microphone, overwrites any ACL set directly on the microphone PickupTrigger script")]
        public AccessControl micAccess;
        public PickupTrigger microphone;
        public AudioPlayerOverrideList overrideList;
        public Collider[] microphoneCollider;
        public MicStandToggle standToggle;

        public AudioOverrideZone baseZone;
        public AudioOverrideZone aoeZone;
        public AudioOverrideZone[] targetZones;
        public bool[] targetControlLocal;
        public bool[] targetControlDefault;
        public bool[] targetControlLinkBase;
        public bool[] targetControlLinkAOE;

        public AudioOverrideZone[] targetLinkDest;
        public AudioOverrideZone[] targetLinkSource;
        public AudioOverrideSettings[] targetLinkBroadcastProfile;
        public AudioOverrideSettings[] targetLinkSuppressProfile;

        public AudioOverrideSettings defaultSettings;
        public AudioOverrideSettings broadcastSettings;
        public AudioOverrideSettings suppressSettings;

        [Header("System Defaults")]
        public bool defaultZoneEnabled = false;
        public bool defaultLocked = true;
        public bool defaultRaise = true;
        public bool defaultLower = false;
        public string zoneName = "ZONE";

        [Header("Microphone Defaults")]
        public bool defaultMicMute = false;
        public bool defaultMicGrab = true;
        public bool defaultMicPTT = true;
        public bool defaultMicAOE = false;

        [Header("UI")]
        public GameObject lockButton;
        public GameObject zoneButton;
        public GameObject raiseButton;
        public GameObject lowerButton;
        public GameObject micGrabButton;
        public GameObject micMuteButton;
        public GameObject micAoeButton;
        public GameObject micPttButton;
        public GameObject micResetButton;
        public Image accessIcon;
        public Image micTxIcon;
        public Text micUserText;

        [UdonSynced, FieldChangeCallback("Locked")]
        bool syncLocked = false;
        [UdonSynced, FieldChangeCallback("MuteEnabled")]
        bool syncMute = false;
        [UdonSynced, FieldChangeCallback("PTTEnabled")]
        bool syncPTT = false;
        [UdonSynced, FieldChangeCallback("GrabEnabled")]
        bool syncGrab = false;
        [UdonSynced, FieldChangeCallback("ZoneEnabled")]
        bool syncZone = false;
        [UdonSynced, FieldChangeCallback("AOEEnabled")]
        bool syncAOE = false;
        [UdonSynced, FieldChangeCallback("RaiseEnabled")]
        bool syncRaise = false;
        [UdonSynced, FieldChangeCallback("LowerEnabled")]
        bool syncLower = false;
        [UdonSynced, FieldChangeCallback("BoundPlayerID")]
        int syncBoundPlayerID = -1;

        AudioOverrideSettings[] targetDefaultSettings;
        AudioOverrideSettings[] targetLocalSettings;
        AudioOverrideSettings[] targetLinkCaptureProfile;

        Vector3 startLocation;
        Quaternion startRotation;

        const int COLOR_RED = 0;
        const int COLOR_YELLOW = 1;

        Color activeYellow = Color.HSVToRGB(60 / 360f, .8f, .9f);
        Color activeRed = Color.HSVToRGB(0, .7f, .9f);

        Color activeYellowLabel = Color.HSVToRGB(60 / 360f, .8f, .5f);
        Color activeRedLabel = Color.HSVToRGB(0, .7f, .5f);

        Color inactiveYellow = Color.HSVToRGB(60 / 360f, .35f, .35f);
        Color inactiveRed = Color.HSVToRGB(0, .35f, .35f);

        Color inactiveYellowLabel = Color.HSVToRGB(60 / 360f, .35f, .2f);
        Color inactiveRedLabel = Color.HSVToRGB(0, .35f, .2f);

        const int UI_BUTTON_LOCK = 0;
        const int UI_BUTTON_ZONE = 1;
        const int UI_BUTTON_MICGRAB = 2;
        const int UI_BUTTON_MICMUTE = 3;
        const int UI_BUTTON_MICAOE = 4;
        const int UI_BUTTON_MICPTT = 5;
        const int UI_BUTTON_MICRESET = 6;
        const int UI_BUTTON_RAISE = 7;
        const int UI_BUTTON_LOWER = 8;
        const int UI_BUTTON_COUNT = 9;

        Image[] buttonBackground;
        Image[] buttonIcon;
        Text[] buttonText;
        int[] buttonColorIndex;

        Color[] colorLookupActive;
        Color[] colorLookupInactive;
        Color[] colorLookupDisabled;
        Color[] colorLookupActiveLabel;
        Color[] colorLookupInactiveLabel;

        void Start()
        {
            colorLookupActive = new Color[] { activeRed, activeYellow };
            colorLookupInactive = new Color[] { inactiveRed, inactiveYellow };
            colorLookupDisabled = new Color[] { inactiveRed, inactiveYellow };

            colorLookupActiveLabel = new Color[] { activeRedLabel, activeYellowLabel };
            colorLookupInactiveLabel = new Color[] { inactiveRedLabel, inactiveYellowLabel };

            buttonColorIndex = new int[UI_BUTTON_COUNT];
            buttonBackground = new Image[UI_BUTTON_COUNT];
            buttonIcon = new Image[UI_BUTTON_COUNT];
            buttonText = new Text[UI_BUTTON_COUNT];

            _DiscoverButton(UI_BUTTON_LOCK, lockButton, COLOR_RED);
            _DiscoverButton(UI_BUTTON_ZONE, zoneButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_MICGRAB, micGrabButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_MICMUTE, micMuteButton, COLOR_RED);
            _DiscoverButton(UI_BUTTON_MICAOE, micAoeButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_MICPTT, micPttButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_MICRESET, micResetButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_RAISE, raiseButton, COLOR_YELLOW);
            _DiscoverButton(UI_BUTTON_LOWER, lowerButton, COLOR_RED);

            if (buttonText[UI_BUTTON_ZONE])
                buttonText[UI_BUTTON_ZONE].text = zoneName;

            micTxIcon.color = inactiveYellow;
            micUserText.text = "";

            targetDefaultSettings = new AudioOverrideSettings[targetZones.Length];
            targetLocalSettings = new AudioOverrideSettings[targetZones.Length];
            for (int i = 0; i < targetZones.Length; i++)
            {
                if (targetZones[i])
                {
                    targetDefaultSettings[i] = targetZones[i]._GetDefaultSettings();
                    targetLocalSettings[i] = targetZones[i]._GetLocalSettings();
                }
            }

            targetControlLocal = (bool[])_EnforceArrayLength(targetControlLocal, targetZones, typeof(bool));
            targetControlDefault = (bool[])_EnforceArrayLength(targetControlDefault, targetZones, typeof(bool));
            targetControlLinkBase = (bool[])_EnforceArrayLength(targetControlLinkBase, targetZones, typeof(bool));
            targetControlLinkAOE = (bool[])_EnforceArrayLength(targetControlLinkAOE, targetZones, typeof(bool));

            targetLinkCaptureProfile = new AudioOverrideSettings[targetLinkDest.Length];
            for (int i = 0; i < targetLinkDest.Length; i++)
            {
                if (targetLinkDest[i])
                    targetLinkCaptureProfile[i] = targetLinkDest[i]._GetLinkedZoneSettings(targetLinkSource[i]);
            }

            if (controlAccess)
                controlAccess._Register(AccessControl.EVENT_VALIDATE, this, "_ValidateAccess");
            else
                _SetButton(UI_BUTTON_LOCK, false);

            if (Utilities.IsValid(microphone))
            {
                startLocation = microphone.transform.position;
                startRotation = microphone.transform.rotation;

                microphone._Register(PickupTrigger.EVENT_PICKUP, this, "_OnPickup");
                microphone._Register(PickupTrigger.EVENT_DROP, this, "_OnDrop");
                microphone._Register(PickupTrigger.EVENT_TRIGGER_ON, this, "_OnTriggerOn");
                microphone._Register(PickupTrigger.EVENT_TRIGGER_OFF, this, "_OnTriggerOff");
                overrideList._Register(AudioPlayerOverrideList.EVENT_BOUND_PLAYER_CHANGED, this, "_OnBoundPlayerChanged");

                if (micAccess)
                    microphone.accessControl = micAccess;

                if (Networking.IsOwner(gameObject) && microphone.TriggerOnUse)
                {
                    PTTEnabled = true;
                    RequestSerialization();
                }

                //if (!controlAccess && microphone.accessControl)
                //    microphone.accessControl._RegisterValidateHandler(this, "_ValidateAccess");

                if (Networking.IsOwner(gameObject))
                {
                    Locked = defaultLocked;
                    ZoneEnabled = defaultZoneEnabled;
                    RaiseEnabled = defaultRaise;
                    LowerEnabled = defaultLower;
                    GrabEnabled = defaultMicGrab;
                    PTTEnabled = defaultMicPTT;
                    MuteEnabled = defaultMicMute;
                    PTTEnabled = defaultMicPTT;
                    RequestSerialization();
                }
            }

            _ValidateAccess();
        }

        Array _EnforceArrayLength(Array arr, Array reference, Type type)
        {
            int length = reference.Length;
            if (arr != null && arr.Length == length)
                return arr;

            Array copy = Array.CreateInstance(type, length);
            if (arr != null)
                Array.Copy(arr, copy, Math.Min(arr.Length, length));

            return copy;
        }

        void _DiscoverButton(int index, GameObject button, int colorIndex)
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

        public bool Locked
        {
            set
            {
                syncLocked = value;

                //microphone.enforceACL = syncLocked;
                //microphone._ValidateACL();

                if (controlAccess)
                {
                    controlAccess.enforce = syncLocked;
                    controlAccess._Validate();
                }
                else
                    syncLocked = false;

                _SetButton(UI_BUTTON_LOCK, syncLocked);
                _ValidateAccess();
            }
            get { return syncLocked; }
        }

        public bool MuteEnabled
        {
            set
            {
                syncMute = value;

                _SetButton(UI_BUTTON_MICMUTE, syncMute);
                _UpdateMicOverride();
            }
            get { return syncMute; }
        }

        public bool PTTEnabled
        {
            set
            {
                syncPTT = value;

                if (Utilities.IsValid(microphone))
                    microphone.TriggerOnUse = value;

                _SetButton(UI_BUTTON_MICPTT, syncPTT);
            }
            get { return syncPTT; }
        }

        public bool GrabEnabled
        {
            set
            {
                syncGrab = value;

                if (Utilities.IsValid(microphone) && !syncGrab)
                    microphone._Drop();

                foreach (var collider in microphoneCollider)
                {
                    if (Utilities.IsValid(collider))
                        collider.enabled = syncGrab;
                }

                _SetButton(UI_BUTTON_MICGRAB, syncGrab);
            }
            get { return syncGrab; }
        }

        public bool ZoneEnabled
        {
            set
            {
                syncZone = value;

                _SetButton(UI_BUTTON_ZONE, syncZone);
                _UpdateZoneOverride();
                _UpdateSupression();
            }
            get { return syncZone; }
        }

        public bool AOEEnabled
        {
            set
            {
                syncAOE = value;

                _SetButton(UI_BUTTON_MICAOE, syncAOE);
                _UpdateAOEOverride();
                _UpdateSupression();
            }
            get { return syncAOE; }
        }

        public bool RaiseEnabled
        {
            set
            {
                syncRaise = value;

                _SetButton(UI_BUTTON_RAISE, syncRaise);
                _UpdateMicOverride();
                _UpdateZoneOverride();
                _UpdateSupression();
            }
            get { return syncRaise; }
        }

        public bool LowerEnabled
        {
            set
            {
                syncLower = value;

                _SetButton(UI_BUTTON_LOWER, syncLower);
                _UpdateMicOverride();
                _UpdateZoneOverride();
                _UpdateSupression();
            }
            get { return syncLower; }
        }

        public int BoundPlayerID
        {
            set
            {
                syncBoundPlayerID = value;

                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(syncBoundPlayerID);
                if (player != null && player.IsValid())
                    micUserText.text = player.displayName;
                else
                    micUserText.text = "";

                _UpdateSupression();
            }
            get { return syncBoundPlayerID; }
        }

        public void _OnBoundPlayerChanged()
        {
            if (_MicTransmitting())
                micTxIcon.color = activeYellow;
            else
                micTxIcon.color = inactiveYellow;

            _UpdateAOEOverride();
            _UpdateSupression();
        }

        public void _OnPickup()
        {
            if (!_MicAccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }

        public void _OnDrop()
        {
            if (!_MicAccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BoundPlayerID = -1;
            RequestSerialization();
        }

        public void _OnTriggerOn()
        {
            _UpdateAOEOverride();
        }

        public void _OnTriggerOff()
        {
            _UpdateAOEOverride();
        }

        public void _ValidateAccess()
        {
            accessIcon.color = _AccessCheck() ? inactiveRed : activeRed;
        }

        public void _ToggleZone()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            ZoneEnabled = !ZoneEnabled;
            RequestSerialization();
        }

        public void _ToggleLock()
        {
            if (!_AccessCheck())
                return;

            AccessControl acl = controlAccess;
            if (acl && acl._LocalHasAccess())
            {
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);

                Locked = !Locked;
                RequestSerialization();
            }
        }

        public void _ToggleRaise()
        {
            if (!_AccessCheck())
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            RaiseEnabled = !RaiseEnabled;
            RequestSerialization();
        }

        public void _ToggleLower()
        {
            if (!_AccessCheck())
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            LowerEnabled = !LowerEnabled;
            RequestSerialization();
        }

        public void _ToggleMute()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            MuteEnabled = !MuteEnabled;
            RequestSerialization();
        }

        public void _ToggleAOE()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            AOEEnabled = !AOEEnabled;
            RequestSerialization();
        }

        public void _ToggleGrab()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            GrabEnabled = !GrabEnabled;
            RequestSerialization();

            if (standToggle)
                standToggle._SetEnabled(GrabEnabled);
        }

        public void _TogglePTT()
        {
            if (!_AccessCheck())
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            PTTEnabled = !PTTEnabled;
            RequestSerialization();
        }

        void _UpdateMicOverride()
        {
            overrideList._SetZoneActive(!MuteEnabled && (RaiseEnabled || LowerEnabled));
        }

        void _UpdateZoneOverride()
        {
            bool state = ZoneEnabled && (RaiseEnabled || LowerEnabled);

            if (Utilities.IsValid(baseZone))
            {
                foreach (var zone in targetZones)
                {
                    if (Utilities.IsValid(zone))
                        zone._SetLinkedZoneActive(baseZone, state);
                }
            }
        }

        void _UpdateAOEOverride()
        {
            bool state = AOEEnabled && _MicTransmitting() && (RaiseEnabled || LowerEnabled);
            if (Utilities.IsValid(aoeZone))
            {
                foreach (var zone in targetZones)
                {
                    if (Utilities.IsValid(zone))
                        zone._SetLinkedZoneActive(aoeZone, state);
                }
            }
        }

        void _UpdateSupression()
        {
            bool active = _AnyOverrideActive();
            bool state = LowerEnabled && active;
            bool linkState = LowerEnabled && !RaiseEnabled;

            for (int i = 0; i < targetZones.Length; i++)
            {
                AudioOverrideZone zone = targetZones[i];
                if (Utilities.IsValid(zone))
                {
                    if (targetControlDefault[i])
                        zone._SetDefaultSettings(state ? suppressSettings : targetDefaultSettings[i]);
                    if (targetControlLocal[i])
                        zone._SetLocalSettings(state ? suppressSettings : targetLocalSettings[i]);

                    AudioOverrideSettings linkedProfile = linkState ? defaultSettings : broadcastSettings;
                    if (baseZone && targetControlLinkBase[i])
                        zone._SetLinkedZoneSettings(baseZone, linkedProfile);
                    if (aoeZone && targetControlLinkAOE[i])
                        zone._SetLinkedZoneSettings(aoeZone, linkedProfile);

                    overrideList._SetZoneSettings(zone, linkedProfile);
                }
            }

            for (int i = 0; i < targetLinkDest.Length; i++)
            {
                AudioOverrideZone zone = targetLinkDest[i];
                if (!zone)
                    continue;

                AudioOverrideSettings profile = null;
                if (active)
                {
                    if (RaiseEnabled)
                        profile = targetLinkBroadcastProfile[i];
                    if (LowerEnabled && !profile)
                        profile = targetLinkSuppressProfile[i];
                }

                if (!profile)
                    profile = targetLinkCaptureProfile[i];

                zone._SetLinkedZoneSettings(targetLinkSource[i], profile);
            }
        }

        bool _AnyOverrideActive()
        {
            if (ZoneEnabled)
                return true;
            if (_MicTransmitting() && !MuteEnabled)
                return true;
            return false;
        }

        bool _MicTransmitting()
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(overrideList.BoundPlayerID);
            return player != null && player.IsValid();
        }

        public void _Respawn()
        {
            if (!_AccessCheck())
                return;

            if (Utilities.IsValid(microphone))
            {
                microphone._Drop();
                Networking.SetOwner(Networking.LocalPlayer, microphone.gameObject);
                microphone.transform.SetPositionAndRotation(startLocation, startRotation);
            }

            if (standToggle)
                standToggle._Reset();

            _SetButton(UI_BUTTON_MICRESET, true);
            SendCustomEventDelayedSeconds("_ResetRespawn", 0.5f);
        }

        public void _ResetRespawn()
        {
            _SetButton(UI_BUTTON_MICRESET, false);
        }

        bool _AccessCheck()
        {
            if (!Locked)
                return true;

            if (!Utilities.IsValid(controlAccess))
                return true;

            return controlAccess._LocalHasAccess();
        }

        bool _MicAccessCheck()
        {
            if (!Locked)
                return true;

            if (!Utilities.IsValid(microphone))
                return true;

            AccessControl acl = microphone.accessControl;
            if (!Utilities.IsValid(acl))
                return true;

            return acl._LocalHasAccess();
        }

        void _SetButton(int buttonIndex, bool state)
        {
            if (buttonIndex < 0 || buttonIndex >= UI_BUTTON_COUNT)
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
