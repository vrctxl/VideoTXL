
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistQueueUI : VideoSourceUIBase
    {
        public PlaylistQueue queue;
        public GameObject queueEntryTemplate;

        public GameObject layoutGroup;
        public VRCUrlInputField urlInput;

        bool loadActive = false;
        VRCUrl pendingSubmit;
        bool pendingFromLoadOverride = false;

        QueueUIEntry[] entries;

        private void Start()
        {
            entries = new QueueUIEntry[0];

            _ClearEntries();
            _BindQueue(queue);
        }

        public override bool _CompatibleSource(VideoUrlSource source)
        {
            if (source == null)
                return false;

            PlaylistQueue queue = source.GetComponent<PlaylistQueue>();
            return queue != null;
        }

        public override void _SetSource(VideoUrlSource source)
        {
            if (source == null)
                return;

            PlaylistQueue listSource = source.GetComponent<PlaylistQueue>();
            if (source != listSource)
                return;

            _BindQueue(listSource);
        }

        public void _BindQueue(PlaylistQueue queue)
        {
            if (entries == null)
            {
                entries = new QueueUIEntry[0];
                _ClearEntries();
            }

            this.queue = queue;
            if (queue)
            {
                queue._Register(PlaylistQueue.EVENT_LIST_CHANGE, this, nameof(_OnListChange));
            }

            SendCustomEventDelayedFrames(nameof(_OnListChange), 1);
        }

        public bool HasPriorityAccess
        {
            get { return queue && queue.HasPriorityAccess; }
        }

        public bool HasDeleteAccess
        {
            get { return queue && queue.HasDeleteAccess; }
        }

        public void _HandleUrlInput()
        {
            if (!queue || !urlInput)
                return;

            pendingFromLoadOverride = loadActive;
            pendingSubmit = urlInput.GetUrl();

            SendCustomEventDelayedSeconds(nameof(_HandleUrlInputDelay), 0.5f);
        }

        public void _HandleUrlInputDelay()
        {
            if (!queue || !urlInput)
                return;

            VRCUrl url = urlInput.GetUrl();
            urlInput.SetUrl(VRCUrl.Empty);

            // Hack to get around Unity always firing OnEndEdit event for submit and lost focus
            // If loading override was on, but it's off immediately after submit, assume user closed override
            // instead of submitting.  Half second delay is a crude defense against a UI race.
            if (pendingFromLoadOverride && !loadActive)
                return;

            queue._AddTrack(url);
            loadActive = false;
        }

        public void _HandleUrlInputClick()
        {
            //if (!videoPlayer._CanTakeControl())
            //    _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleUrlInputChange()
        {
            if (!queue)
                return;

            //VRCUrl url = urlInput.GetUrl();
            //if (url.Get().Length > 0)
            //    queue._AddTrack(urlInput.GetUrl());
        }

        public void _HandlePriority(int index)
        {
            if (!queue)
                return;

            queue._MoveTrack(index, 0);
        }

        public void _HandleDelete(int index)
        {
            if (!queue)
                return;

            queue._RemoveTrack(index);
        }

        public void _OnListChange()
        {
            if (!queue)
            {
                _HideEntries();
                return;
            }

            _BuildList();
            Canvas.ForceUpdateCanvases();
        }

        void _ClearEntries()
        {
            int entryCount = layoutGroup.transform.childCount;
            for (int i = 0; i < entryCount; i++)
            {
                GameObject child = layoutGroup.transform.GetChild(i).gameObject;
                Destroy(child);
            }

            entries = new QueueUIEntry[0];
        }

        void _HideEntries()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i])
                    entries[i].gameObject.SetActive(false);
            }
        }

        void _BuildList()
        {
            if (!Utilities.IsValid(queue))
            {
                _HideEntries();
                return;
            }

            int count = queue.Count;
            entries = (QueueUIEntry[])UtilityTxl.ArrayMinSize(entries, count, typeof(UdonBehaviour));

            for (int i = 0; i < entries.Length; i++)
            {
                if (i >= count)
                {
                    if (entries[i])
                        entries[i].gameObject.SetActive(false);
                    continue;
                }

                if (entries[i])
                    entries[i].gameObject.SetActive(true);
                else
                {
                    GameObject entry = Instantiate(queueEntryTemplate);

                    QueueUIEntry script = (QueueUIEntry)entry.GetComponent(typeof(UdonBehaviour));
                    script.queueUI = this;
                    script.Track = i;

                    entry.transform.SetParent(layoutGroup.transform);

                    RectTransform rt = (RectTransform)entry.GetComponent(typeof(RectTransform));
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.localPosition = Vector3.zero;

                    entries[i] = script;
                }
            }

            for (int i = 0; i < count; i++)
            {
                QueueUIEntry script = entries[i];
                if (!script)
                    continue;

                string urlStr = "";
                VRCUrl url = queue._GetTrackURL(i);
                if (url != null)
                    urlStr = url.Get();

                string title = queue._GetTrackName(i);
                if (title == null || title == "")
                {
                    title = urlStr;
                    urlStr = "";
                }

                script.Title = title;
                script.Url = urlStr;
                script.PlayerName = queue._GetTrackPlayer(i);
            }
        }
    }
}
