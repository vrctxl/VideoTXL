
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DependentSource : UdonSharpBehaviour
    {
        [SerializeField] internal TXLVideoPlayer primaryVideoPlayer;
        [SerializeField] internal VRCUrl[] primaryUrls;
        [SerializeField] internal VRCUrl[] dependentUrls;

        private LocalPlayer dependentVideoPlayer;
        private int state = -1;
        private VRCUrl boundUrl = null;

        void Start()
        {
            _BindPrimaryVideoPlayer(primaryVideoPlayer);
        }

        public void _BindPrimaryVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            if (primaryVideoPlayer)
            {
                primaryVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoUpdate));
                primaryVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_InternalOnTrackingUpdate));
                state = -1;
            }

            primaryVideoPlayer = videoPlayer;
            if (primaryVideoPlayer)
            {
                primaryVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoUpdate));
                primaryVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_InternalOnTrackingUpdate));
                state = primaryVideoPlayer.playerState;
            }
        }

        public void _BindDepdendentVideoPlayer(LocalPlayer videoPlayer)
        {
            if (dependentVideoPlayer)
            {
                dependentVideoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnDepVideoUpdate));
            }

            dependentVideoPlayer = videoPlayer;
            if (dependentVideoPlayer)
            {
                dependentVideoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnDepVideoUpdate));
            }
        }

        public TXLVideoPlayer PrimaryVideoPlayer
        {
            get { return primaryVideoPlayer; }
        }

        public LocalPlayer DependentVideoPlayer
        {
            get { return dependentVideoPlayer; }
        }

        public void _InternalOnVideoUpdate()
        {
            int newState = primaryVideoPlayer.playerState;
            if (state != newState)
            {
                if (newState == TXLVideoPlayer.VIDEO_STATE_LOADING)
                {
                    VRCUrl match = null;
                    VRCUrl url = primaryVideoPlayer.currentUrl;

                    if (url != null)
                    {
                        for (int i = 0; i < primaryUrls.Length; i++)
                        {
                            if (primaryUrls[i] != null && primaryUrls[i].Get() == url.Get())
                            {
                                match = dependentUrls[i];
                                break;
                            }
                        }
                    }

                    if (match == null || match.Get() == "")
                    {
                        if (dependentVideoPlayer)
                            dependentVideoPlayer._TriggerStop();
                        return;
                    }

                    boundUrl = match;
                    SendCustomEventDelayedSeconds(nameof(_InternalStartDependent), 6);
                }
                else if (newState == TXLVideoPlayer.VIDEO_STATE_STOPPED)
                {
                    if (dependentVideoPlayer)
                        dependentVideoPlayer._TriggerStop();
                }

                state = newState;
            }
        }

        public void _InternalOnTrackingUpdate()
        {
            if (dependentVideoPlayer)
                _InternalOnDepVideoUpdate();
        }

        public void _InternalOnDepVideoUpdate()
        {
            if (dependentVideoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING)
            {
                if (primaryVideoPlayer && primaryVideoPlayer.VideoManager && dependentVideoPlayer.VideoManager)
                {
                    if (primaryVideoPlayer.seekableSource && dependentVideoPlayer.seekableSource)
                        dependentVideoPlayer.VideoManager._VideoSetTime(primaryVideoPlayer.VideoManager.VideoTime);
                }
            }
        }

        public void _InternalStartDependent()
        {
            if (dependentVideoPlayer)
            {
                dependentVideoPlayer._ChangeUrl(boundUrl);
                dependentVideoPlayer._TriggerPlay();
            }
        }
    }
}
