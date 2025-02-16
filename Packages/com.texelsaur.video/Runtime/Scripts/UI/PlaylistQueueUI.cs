
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistQueueUI : UdonSharpBehaviour
    {
        public PlaylistQueue queue;

        public VRCUrlInputField urlInput;

        bool loadActive = false;
        VRCUrl pendingSubmit;
        bool pendingFromLoadOverride = false;

        public void _HandleUrlInput()
        {
            Debug.Log("_HandleUrlInput");
            if (!Utilities.IsValid(queue))
                return;

            pendingFromLoadOverride = loadActive;
            pendingSubmit = urlInput.GetUrl();

            SendCustomEventDelayedSeconds(nameof(_HandleUrlInputDelay), 0.5f);
        }

        public void _HandleUrlInputDelay()
        {
            Debug.Log("_HandleUrlInputDelay");
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
            Debug.Log("_HandleUrlInputClick");
            //if (!videoPlayer._CanTakeControl())
            //    _SetStatusOverride(MakeOwnerMessage(), 3);
        }

        public void _HandleUrlInputChange()
        {
            Debug.Log("_HandleUrlInputChange");
            if (!queue)
                return;

            //VRCUrl url = urlInput.GetUrl();
            //if (url.Get().Length > 0)
            //    queue._AddTrack(urlInput.GetUrl());
        }
    }
}
