
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Texel
{
    public enum VideoSourceBackend
    {
        AVPro = 1,
        Unity = 2,
    }

    public enum VideoSourceLatency
    {
        Standard = 1,
        LowLatency = 2,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSource : UdonSharpBehaviour
    {
        [Tooltip("The main video source manager")]
        public VideoManager videoMux;
        [Tooltip("Internal object for capturing the video source's render texture.")]
        public MeshRenderer captureRenderer;
        [Tooltip("If multiple resolutions are available for a given URL, the video source will attempt to load the video with the largest resolution that is equal to or smaller than this limit.")]
        public int maxResolution = 720;

        [Tooltip("The audio group definitions associated with this source.  This list is usually auto-generated from the update components button in the main video player inspector.")]       
        public VideoSourceAudioGroup[] audioGroups;

        [Tooltip("A special audio source for AVPro video sources that's enabled for all audio groups and required for proper functioning of audio group switching.  This source should usually be used for AudioLink, and will be used if no override is specified in an audio group.")]
        public AudioSource avproReservedChannel;
        [Tooltip("Whether this source has AVPro's low latency option enabled.  Low latency is necessary for some sources like VRCDN RSTP URLs.")]
        public bool lowLatency = false;

        int id = 0;
        BaseVRCVideoPlayer videoPlayer;

        public const short VIDEO_SOURCE_NONE = 0;
        public const short VIDEO_SOURCE_AVPRO = 1;
        public const short VIDEO_SOURCE_UNITY = 2;

        public const short LOW_LATENCY_UNKNOWN = 0;
        public const short LOW_LATENCY_DISABLE = 1;
        public const short LOW_LATENCY_ENABLE = 2;

        public short VideoSourceType { get; private set; }

        public int ID
        {
            get { return id; }
        }

        public BaseVRCVideoPlayer VideoPlayer
        {
            get { return videoPlayer; }
        }

        public void _Register(VideoManager mux, int muxId)
        {
            videoMux = mux;
            id = muxId;

            _CheckIntegrity();
            _AutoDetect();
            _InitVideoPlayer();
        }

        void _CheckIntegrity()
        {
            if (!videoMux)
            {
                Debug.LogError($"Video source {id} registered without a valid video manager.");
                videoMux = gameObject.transform.parent.GetComponentInParent<VideoManager>();
                if (videoMux)
                    _DebugLog($"Found video manager on parent: {videoMux.gameObject.name}");
                else
                    Debug.LogError("Could not find parent video manager.  Video playback via this source will not work.");
            }

            // Try to repair missing required components
            if (!captureRenderer)
            {
                Debug.LogError($"Video source {id} missing captureRenderer.");

                captureRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
                if (captureRenderer)
                    _DebugLog($"Found child renderer on: {captureRenderer.gameObject.name}");
                else
                    Debug.LogError("Could not find child renderer.  Video playback via this source will not work.");
            }

            if (audioGroups.Length == 0)
                Debug.LogError($"Video source {id} has no audio groups.  Try updating connected components on the main video player object.");
        }

        void _AutoDetect()
        {
            // The type-based lookup was actually finding and incorrectly casting VRCUnityVideoPlayer components ._.
            VRCAVProVideoPlayer avp = (VRCAVProVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            if (avp != null)
            {
                videoPlayer = avp;
                VideoSourceType = VIDEO_SOURCE_AVPRO;

                if (!avproReservedChannel)
                {
                    Debug.LogError($"Video source {id} is an AVPro video source, but has no audio source set on its reserved channel.");

                    Transform reserved = transform.Find("ReservedAudioSource");
                    if (reserved)
                    {
                        avproReservedChannel = transform.GetComponent<AudioSource>();
                        if (avproReservedChannel)
                            _DebugLog($"Found suspected reserved audio source on: {reserved.name}");
                        else
                            _DebugLog($"Could not infer reserved audio source.  Some audio playback functions may not work correctly.");
                    }
                }

                /*if (avp.UseLowLatency != lowLatency)
                {
                    lowLatency = avp.UseLowLatency;
                    _DebugLog($"Low latency mismatch, using = {lowLatency}");
                }*/

                /*if (avp.MaximumResolution != maxResolution)
                {
                    maxResolution = avp.MaximumResolution;
                    _DebugLog($"Max resolution mismatch, using = {maxResolution}");
                }*/
                return;
            }

            VRCUnityVideoPlayer unity = (VRCUnityVideoPlayer)gameObject.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (unity != null)
            {
                videoPlayer = unity;
                VideoSourceType = VIDEO_SOURCE_UNITY;

                /*if (unity.MaximumResolution != maxResolution)
                {
                    maxResolution = unity.MaximumResolution;
                    _DebugLog($"Max resolution mismatch, using = {maxResolution}");
                }*/
                return;
            }

            Debug.LogError($"Video source {id} has no attached VRCUnityVideoPlayer or VRCAVProVideoPlayer component.");

            VideoSourceType = VIDEO_SOURCE_NONE;
        }

        void _InitVideoPlayer()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.Loop = false;
            videoPlayer.EnableAutomaticResync = false;
            videoPlayer.Stop();
        }

        public string _FormattedAttributes()
        {
            string str = "None";
            if (VideoSourceType == VIDEO_SOURCE_AVPRO)
                str = $"AVPro, res={maxResolution}, ll={lowLatency}";
            else if (VideoSourceType == VIDEO_SOURCE_UNITY)
                str = $"Unity, res={maxResolution}";
            return str;
        }


        public override void OnVideoReady()
        {
            if (videoMux)
                videoMux._OnVideoReady(id);
        }

        public override void OnVideoStart()
        {
            if (videoMux)
                videoMux._OnVideoStart(id);
        }

        public override void OnVideoEnd()
        {
            if (videoMux)
                videoMux._OnVideoEnd(id);
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (videoMux)
                videoMux._OnVideoError(id, videoError);
        }

        public override void OnVideoLoop()
        {
            if (videoMux)
                videoMux._OnVideoLoop(id);
        }

        public override void OnVideoPause()
        {
            if (videoMux)
                videoMux._OnVideoPause(id);
        }

        public override void OnVideoPlay()
        {
            if (videoMux)
                videoMux._OnVideoPlay(id);
        }

        public void _VideoPlay()
        {
            if (videoPlayer)
                videoPlayer.Play();
        }

        public void _VideoPause()
        {
            if (videoPlayer)
                videoPlayer.Pause();
        }

        public void _VideoStop()
        {
            if (videoPlayer)
                videoPlayer.Stop();
        }

        public void _VideoStop(int frameDelay)
        {
            SendCustomEventDelayedFrames("_VideoStop", frameDelay);
        }

        public void _VideoLoadURL(VRCUrl url)
        {
            if (videoPlayer)
                videoPlayer.LoadURL(url);
        }

        public void _VideoSetTime(float time)
        {
            if (videoPlayer)
                videoPlayer.SetTime(time);
        }

        public void _SetAVSync(bool state)
        {
            if (videoPlayer)
                videoPlayer.EnableAutomaticResync = state;
        }

        void _DebugLog(string message)
        {
            if (videoMux)
                videoMux._DownstreamDebugLog(this, message);
        }
    }
}
