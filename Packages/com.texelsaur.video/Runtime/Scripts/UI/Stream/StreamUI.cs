
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StreamUI : VideoSourceUIBase
    {
        public StreamSource streamSource;
        public GameObject streamEntryTemplate;

        public GameObject layoutGroup;
        public GameObject defaultHeading;
        public GameObject defaultEntry;
        public GameObject additionalHeading;
        public GameObject customUrlEntry;

        public VRCUrlInputField urlInput;

        TXLVideoPlayer videoPlayer;
        StreamUIEntry defaultEntryScript;
        StreamUIEntry customEntryScript;
        StreamUIEntry[] additionalEntries;

        private void Start()
        {
            additionalEntries = new StreamUIEntry[0];

            if (defaultEntry)
            {
                defaultEntryScript = defaultEntry.GetComponentInChildren<StreamUIEntry>();
                if (defaultEntryScript)
                {
                    defaultEntryScript.streamUI = this;
                    defaultEntryScript.entryType = StreamUIEntryType.Default;
                }
            }

            if (customUrlEntry)
            {
                customEntryScript = customUrlEntry.GetComponentInChildren<StreamUIEntry>();
                if (customEntryScript)
                {
                    customEntryScript.streamUI = this;
                    customEntryScript.entryType = StreamUIEntryType.Custom;
                }
            }

            _ClearAdditionalEntries();
            _BindStreamSource(streamSource);
        }

        public void _BindStreamSource(StreamSource source)
        {
            if (additionalEntries == null)
                _ClearAdditionalEntries();

            if (streamSource)
            {
                streamSource._Unregister(StreamSource.EVENT_BIND_VIDEOPLAYER, this, nameof(_InternalOnBindVideoPlayer));
                streamSource._Unregister(StreamSource.EVENT_CUSTOM_URL_CHANGE, this, nameof(_InternalOnCustomUrlChanged));
            }

            streamSource = source;
            if (streamSource)
            {
                streamSource._Register(StreamSource.EVENT_BIND_VIDEOPLAYER, this, nameof(_InternalOnBindVideoPlayer));
                streamSource._Register(StreamSource.EVENT_CUSTOM_URL_CHANGE, this, nameof(_InternalOnCustomUrlChanged));
            }

            _InternalOnBindVideoPlayer();
            _BuildList();
        }

        public void _InternalOnBindVideoPlayer()
        {
           
            if (videoPlayer)
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_InternalOnUrlChanged));

            if (streamSource)
            {
                videoPlayer = streamSource.VideoPlayer;
                if (videoPlayer)
                    videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_INFO_UPDATE, this, nameof(_InternalOnUrlChanged));
            }
            else
                videoPlayer = null;
        }

        void _ClearAdditionalEntries()
        {
            if (!additionalHeading)
                return;

            int entryCount = additionalHeading.transform.parent.childCount;
            int headingIndex = additionalHeading.transform.GetSiblingIndex();

            for (int i = headingIndex + 1; i < entryCount; i++)
            {
                GameObject child = additionalHeading.transform.parent.GetChild(i).gameObject;
                if (child != customUrlEntry)
                    Destroy(child);
            }

            additionalEntries = new StreamUIEntry[0];
        }

        void _HideEntries()
        {
            for (int i = 0; i < additionalEntries.Length; i++)
            {
                if (additionalEntries[i])
                    additionalEntries[i].gameObject.SetActive(false);
            }

            if (customUrlEntry)
                customUrlEntry.SetActive(false);
        }

        public void _SelectDefault()
        {
            if (streamSource)
                streamSource._SetUrl(streamSource.DefaulStreamtUrl);
            else
                Debug.LogWarning("[VideoTXL] Tried to select stream entry track, but the UI is not associated with a stream source!");
        }

        public void _SelectCustom()
        {
            if (streamSource)
            {
                if (streamSource.allowCustomUrl)
                    streamSource._SetUrl(streamSource.CustomStreamUrl, streamSource.CustomStreamQuestUrl);
            }
            else
                Debug.LogWarning("[VideoTXL] Tried to select stream entry track, but the UI is not associated with a stream source!");
        }

        public void _SelectAdditional(int track)
        {
            if (streamSource)
            {
                if (track < 0 || track >= streamSource.AdditionalUrlCount)
                    return;

                VRCUrl url = streamSource._GetAdditionalStreamUrl(track);
                streamSource._SetUrl(url);
            }
            else
                Debug.LogWarning("[VideoTXL] Tried to select stream entry track, but the UI is not associated with a stream source!");
        }

        public void _SetCustomUrl(VRCUrl url)
        {
            if (streamSource)
                streamSource.CustomStreamUrl = url;
        }

        public void _InternalOnCustomUrlChanged()
        {
            if (customEntryScript)
                customEntryScript._SetUrlDisplay(streamSource.CustomStreamUrl);
        }

        public void _InternalOnUrlChanged()
        {

            VRCUrl url = VRCUrl.Empty;
            if (videoPlayer && streamSource.VideoPlayer.currentUrlSource == streamSource)
                url = videoPlayer.currentUrl;

            Debug.Log($"XX Stream UI On Url Change {url}");

            if (defaultEntryScript)
                defaultEntryScript.Selected = streamSource.DefaulStreamtUrl == url;
            if (customEntryScript)
                customEntryScript.Selected = streamSource.CustomStreamUrl == url;

            for (int i = 0; i < additionalEntries.Length; i++)
            {
                if (additionalEntries[i])
                    additionalEntries[i].Selected = streamSource._GetAdditionalStreamUrl(i) == url;
            }
        }

        void _BuildList()
        {
            if (!Utilities.IsValid(streamSource))
            {
                _HideEntries();
                return;
            }

            bool hasAdditional = streamSource.allowCustomUrl || streamSource.AdditionalUrlCount > 0;
            if (additionalHeading)
            {
                additionalHeading.SetActive(hasAdditional);
                if (hasAdditional)
                    additionalHeading.transform.SetAsFirstSibling();
            }

            if (defaultEntry)
                defaultEntry.transform.SetAsFirstSibling();
            if (defaultHeading)
                defaultHeading.transform.SetAsFirstSibling();

            if (hasAdditional)
            {
                int count = streamSource.AdditionalUrlCount;
                additionalEntries = (StreamUIEntry[])UtilityTxl.ArrayMinSize(additionalEntries, count, typeof(UdonBehaviour));

                for (int i = 0; i < additionalEntries.Length; i++)
                {
                    if (i >= count)
                    {
                        if (additionalEntries[i])
                            additionalEntries[i].gameObject.SetActive(false);
                        continue;
                    }

                    if (additionalEntries[i])
                        additionalEntries[i].gameObject.SetActive(true);
                    else
                    {
                        GameObject entry = Instantiate(streamEntryTemplate);

                        StreamUIEntry script = (StreamUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                        script.streamUI = this;
                        script.Track = i;
                        script.entryType = StreamUIEntryType.Additional;

                        entry.transform.SetParent(layoutGroup.transform);

                        RectTransform rt = (RectTransform)entry.GetComponent(typeof(RectTransform));
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                        rt.localPosition = Vector3.zero;

                        additionalEntries[i] = script;
                    }
                }

                if (customUrlEntry)
                {
                    if (streamSource.allowCustomUrl)
                    {
                        customUrlEntry.SetActive(streamSource.allowCustomUrl);

                        customUrlEntry.transform.SetParent(null);
                        customUrlEntry.transform.SetParent(layoutGroup.transform);

                        RectTransform rt = (RectTransform)customUrlEntry.GetComponent(typeof(RectTransform));
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                        rt.localPosition = Vector3.zero;
                    }
                    else
                        customUrlEntry.SetActive(false);
                }

                for (int i = 0; i < count; i++)
                {
                    StreamUIEntry script = additionalEntries[i];
                    if (!script)
                        continue;

                    string urlStr = "";
                    VRCUrl url = streamSource._GetAdditionalStreamUrl(i);
                    if (url != null)
                        urlStr = url.Get();

                    string title = streamSource._GetAdditionalStreamName(i);
                    if (title == null || title == "")
                    {
                        title = urlStr;
                        urlStr = "";
                    }

                    script.Title = title;
                    script.Url = urlStr;
                }
            }
        }
    }
}
