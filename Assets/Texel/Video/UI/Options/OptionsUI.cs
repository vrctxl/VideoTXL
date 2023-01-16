
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OptionsUI : UdonSharpBehaviour
    {
        public PlayerControls mainControls;
        public TXLVideoPlayer videoPlayer;
        public AudioManager audioManager;

        public bool infoPanel = true;
        public bool videoPanel = true;
        public bool audioPanel = true;
        public bool localOptionsOnly = false;

        [Header("Internal")]
        public GameObject optionsPanel;
        public GameObject optionsInfoPanel;
        public GameObject optionsVideoPanel;
        public GameObject optionsAudioPanel;
        public GameObject optionsInfoNav;
        public GameObject optionsVideoNav;
        public GameObject optionsAudioNav;
        public GameObject optionsInfoNavActive;
        public GameObject optionsVideoNavActive;
        public GameObject optionsAudioNavActive;

        public Dropdown videoModeDropdown;
        public Dropdown videoFitDropdown;
        public Dropdown videoResolutionDropdown;
        public Dropdown videoLatencyDropdown;
        public Dropdown audioProfileDropdown;

        public Text infoMasterText;
        public Text infoInstanceOwnerText;
        public Text infoObjectOwnerText;
        public InputField infoCurrentVideoInput;
        public InputField infoLastVideoInput;
        public Text infoCurrentVideoText;
        public Text infoLastVideoText;
        public Image infoPlayCurrentVideoImage;
        public Image infoPlayLastVideoImage;

        const int OPTIONS_TAB_NONE = 0;
        const int OPTIONS_TAB_INFO = 1;
        const int OPTIONS_TAB_VIDEO = 2;
        const int OPTIONS_TAB_AUDIO = 3;

        int optionsTabOpen = OPTIONS_TAB_NONE;
        bool registeredVideo = false;
        bool registeredAudio = false;

        Color normalColor = new Color(1f, 1f, 1f, .8f);
        Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        Color activeColor = new Color(0f, 1f, .5f, .7f);
        Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        void Start()
        {
            _SetControls(mainControls);

            if (!infoPanel && optionsInfoNav)
                optionsInfoNav.SetActive(false);
            if (!videoPanel && optionsVideoNav)
                optionsVideoNav.SetActive(false);
            if (!audioPanel && optionsAudioNav)
                optionsAudioNav.SetActive(false);

            if (localOptionsOnly)
            {
                if (optionsInfoNav)
                    optionsInfoNav.SetActive(false);
                if (videoModeDropdown)
                    videoModeDropdown.transform.parent.gameObject.SetActive(false);
                if (videoFitDropdown)
                    videoFitDropdown.transform.parent.gameObject.SetActive(false);
            }

            if (optionsInfoNav && !optionsInfoNav.activeSelf)
                infoPanel = false;
            if (optionsVideoNav && !optionsVideoNav.activeSelf)
                videoPanel = false;
            if (optionsAudioNav && !optionsAudioNav.activeSelf)
                audioPanel = false;
        }

        public void _SetControls(PlayerControls controls)
        {
            mainControls = controls;

            if (mainControls)
            {
                if (!videoPlayer)
                    videoPlayer = mainControls.videoPlayer;
                if (!audioManager)
                    audioManager = mainControls.audioManager;
            }

            _UpdateComponents();
        }

        void _UpdateComponents()
        {
            if (videoPlayer && !registeredVideo)
            {
                registeredVideo = true;
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, "_OnVideoInfoUpdate");
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_OnVideoStateUpdate");

                VideoManager mux = videoPlayer.videoMux;
                if (mux)
                {
                    mux._Register(VideoManager.SETTINGS_CHANGE_EVENT, this, "_OnMuxSettingsChange");
                    _OnMuxSettingsChange();
                }
            }

            if (audioManager && !registeredAudio)
            {
                registeredAudio = true;
                audioManager._Register(AudioManager.EVENT_CHANNEL_GROUP_CHANGED, this, "_OnChannelGroupChanged");
            }
        }



        public void _HandleOptions()
        {
            if (optionsTabOpen == OPTIONS_TAB_NONE)
            {
                int tab = OPTIONS_TAB_NONE;
                if (infoPanel)
                    tab = OPTIONS_TAB_INFO;
                else if (videoPanel)
                    tab = OPTIONS_TAB_VIDEO;
                else if (audioPanel)
                    tab = OPTIONS_TAB_AUDIO;

                _OpenOptionsTab(tab);
                _OnVideoInfoUpdate();
            }
            else
                _OpenOptionsTab(OPTIONS_TAB_NONE);
        }

        public void _HandleInfoTab()
        {
            _OpenOptionsTab(OPTIONS_TAB_INFO);
        }

        public void _HandleVideoTab()
        {
            _OpenOptionsTab(OPTIONS_TAB_VIDEO);
        }

        public void _HandleAudioTab()
        {
            _OpenOptionsTab(OPTIONS_TAB_AUDIO);
        }

        void _OpenOptionsTab(int tab)
        {
            optionsInfoPanel.SetActive(tab == OPTIONS_TAB_INFO);
            optionsInfoNavActive.SetActive(tab == OPTIONS_TAB_INFO);
            optionsVideoPanel.SetActive(tab == OPTIONS_TAB_VIDEO);
            optionsVideoNavActive.SetActive(tab == OPTIONS_TAB_VIDEO);
            optionsAudioPanel.SetActive(tab == OPTIONS_TAB_AUDIO);
            optionsAudioNavActive.SetActive(tab == OPTIONS_TAB_AUDIO);
            optionsPanel.SetActive(tab != OPTIONS_TAB_NONE);

            optionsTabOpen = tab;
        }

        public void _OnMuxSettingsChange()
        {
            // videoModeDropdown.SetValueWithoutNotify(videoPlayer.videoMux.VideoType);
            videoLatencyDropdown.SetValueWithoutNotify(videoPlayer.videoMux.Latency);
            videoResolutionDropdown.SetValueWithoutNotify(videoPlayer.videoMux.ResolutionIndex);
        }

        public void _OnVideoStateUpdate()
        {
            videoFitDropdown.SetValueWithoutNotify(videoPlayer.screenFit);
            videoModeDropdown.SetValueWithoutNotify(videoPlayer.playerSourceOverride);
        }

        // Info Panel

        public void _OnVideoInfoUpdate()
        {
            if (!optionsInfoPanel.activeInHierarchy)
                return;

            bool canControl = videoPlayer._CanTakeControl();
            bool enableControl = !videoPlayer.locked || canControl;

            string currentUrl = videoPlayer.currentUrl.Get();
            string lastUrl = videoPlayer.lastUrl.Get();

            infoPlayCurrentVideoImage.color = (enableControl && currentUrl != "") ? normalColor : disabledColor;
            infoPlayLastVideoImage.color = (enableControl && lastUrl != "") ? normalColor : disabledColor;

            if (currentUrl != null && currentUrl.Length > 36)
                infoCurrentVideoText.text = currentUrl.Substring(0, 16) + "..." + currentUrl.Substring(currentUrl.Length - 16, 16);
            else
                infoCurrentVideoText.text = currentUrl;

            if (lastUrl != null && lastUrl.Length > 36)
                infoLastVideoText.text = lastUrl.Substring(0, 16) + "..." + lastUrl.Substring(lastUrl.Length - 16, 16);
            else
                infoLastVideoText.text = lastUrl;

            if (!videoPlayer.IsQuest)
            {
                infoCurrentVideoInput.text = currentUrl;
                infoLastVideoInput.text = lastUrl;
            }

            if (mainControls)
            {
                infoInstanceOwnerText.text = mainControls.instanceOwner;
                infoMasterText.text = mainControls.instanceMaster;
            }

            VRCPlayerApi owner = Networking.GetOwner(videoPlayer.gameObject);
            if (Utilities.IsValid(owner) && owner.IsValid())
                infoObjectOwnerText.text = owner.displayName;
            else
                infoObjectOwnerText.text = "";

        }

        public void _HandlePlayCurrent()
        {
            if (!videoPlayer)
                return;
            if (videoPlayer.currentUrl == VRCUrl.Empty)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._ChangeUrl(videoPlayer.currentUrl);
            else if (mainControls)
                mainControls._SetStatusOverride(mainControls._MakeOwnerMessage(), 3);
        }

        public void _HandlePlayLast()
        {
            if (!videoPlayer)
                return;
            if (videoPlayer.lastUrl == VRCUrl.Empty)
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._ChangeUrl(videoPlayer.lastUrl);
            else if (mainControls)
                mainControls._SetStatusOverride(mainControls._MakeOwnerMessage(), 3);
        }

        // Video Panel

        public void _HandleModeChangedUI()
        {
            _UpdateSource((short)videoModeDropdown.value);
        }

        void _UpdateSource(short mode)
        {
            if (!videoPlayer)
                return;

            if (!videoPlayer._CanTakeControl())
            {
                if (mainControls)
                    mainControls._SetStatusOverride(mainControls._MakeOwnerMessage(), 3);
                return;
            }

            videoPlayer._SetSourceMode(mode);
        }

        public void _HandleLatencyChangedUI()
        {
            if (!videoPlayer)
                return;

            int mode = videoLatencyDropdown.value;
            if (mode == 0)
                mode = VideoSource.LOW_LATENCY_ENABLE;
            else if (mode == 1)
                mode = VideoSource.LOW_LATENCY_DISABLE;

            videoPlayer._SetSourceLatency(mode);
        }

        public void _HandleScreenFitChangedUI()
        {
            if (!videoPlayer)
                return;

            if (!videoPlayer._CanTakeControl())
            {
                if (mainControls)
                    mainControls._SetStatusOverride(mainControls._MakeOwnerMessage(), 3);
                return;
            }

            videoPlayer._SetScreenFit((short)videoFitDropdown.value);
        }

        public void _HandleResolutionChangedUI()
        {
            if (!videoPlayer)
                return;

            videoPlayer._SetSourceResolution(videoResolutionDropdown.value);
        }

        // Audio Panel

        public void _OnChannelGroupChanged()
        {
            int channelGroup = 0;
            for (int i = 0; i < audioManager.channelGroups.Length; i++)
            {
                if (audioManager.channelGroups[i] == audioManager.SelectedChannelGroup)
                {
                    channelGroup = i;
                    break;
                }
            }

            audioProfileDropdown.SetValueWithoutNotify(channelGroup);
        }

        public void _HandleAudioProfileChangedUI()
        {
            _UpdateAudioProfile(audioProfileDropdown.value);
        }

        void _UpdateAudioProfile(int profile)
        {
            if (!audioManager)
                return;

            AudioChannelGroup[] groups = audioManager.channelGroups;
            if (profile < 0 || profile >= groups.Length)
                return;

            audioManager._SelectChannelGroup(groups[profile]);
        }
    }
}
