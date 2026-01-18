
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InputProxy : UdonSharpBehaviour
    {
        [SerializeField] internal SyncPlayer videoPlayer;
        [SerializeField] internal bool alwaysUseQueue = false;

        [SerializeField] internal VRCUrlInputField urlInputField;
        [SerializeField] internal InputField urlNameInputField;
        [SerializeField] internal InputField urlAuthorInputField;
        [SerializeField] internal VRCUrlInputField queueInputField;
        [SerializeField] internal InputField queueNameInputField;
        [SerializeField] internal InputField queueAuthorInputField;

        [SerializeField] internal UdonBehaviour youtubeSearchManager;
        [SerializeField] internal bool youtubeSearchEnabled;

        void Start()
        {

        }

        public void _UrlInput ()
        {
            _UrlInput(urlInputField.GetUrl(), urlNameInputField.text, urlAuthorInputField.text);
        }

        public void _UrlInput (VRCUrl url, string name = null, string author = null)
        {
            if (!videoPlayer)
                return;

            if (videoPlayer.urlInfoResolver)
                videoPlayer.urlInfoResolver._AddInfo(url, name != "" ? name : null, author != "" ? author : null);

            bool loadOnQueue = alwaysUseQueue;
            VideoUrlSource addSource = null;

            if (!videoPlayer.sourceManager)
                loadOnQueue = false;
            else
                addSource = videoPlayer.sourceManager._GetSource(videoPlayer.sourceManager._GetCanAddTrack());

            if (!addSource)
                loadOnQueue = false;

            if (loadOnQueue)
                addSource._AddTrackFromProxy(url, VRCUrl.Empty, name != null ? name : "", author != null ? author : "");
            else
                videoPlayer._ChangeUrl(url);
        }

        public void _QueueInput()
        {
            _QueueInput(queueInputField.GetUrl(), queueNameInputField.text, queueAuthorInputField.text);
        }

        public void _QueueInput (VRCUrl url, string name = null, string author = null)
        {
            if (!videoPlayer || !videoPlayer.sourceManager)
                return;

            VideoUrlSource addSource = videoPlayer.sourceManager._GetSource(videoPlayer.sourceManager._GetCanAddTrack());
            if (!addSource)
                return;

            if (videoPlayer.urlInfoResolver)
                videoPlayer.urlInfoResolver._AddInfo(url, name != "" ? name : null, author != "" ? author : null);

            addSource._AddTrackFromProxy(url, VRCUrl.Empty, name != null ? name : "", author != null ? author : "");
        }
    }
}
