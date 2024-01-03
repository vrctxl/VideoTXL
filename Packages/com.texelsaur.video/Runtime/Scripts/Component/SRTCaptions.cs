
using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SRTCaptions : UdonSharpBehaviour
    {
        public TXLVideoPlayer videoPlayer;
        public float maxDuration = 3;

        [Header("UI")]
        public Text captionText;

        int count = 0;
        int index = 0;
        float[] startTimes;
        float[] endTimes;
        string[] captions;

        bool active = false;
        float position = 0;
        float positionSyncTime = 0;
        float positionSyncOffset = 0;

        void Start()
        {
            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_OnVideoStateUpdate));
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_TRACKING_UPDATE, this, nameof(_OnVideoTrackingUpdate));
            }

            _Load(@"1
00:00:00,320 --> 00:00:05,360
Here are three simple steps for turning on
subtitles or captions in a YouTube video.

2
00:00:06,340 --> 00:00:12,240
First, in any video, look for the ""CC"" icon
in the lower right corner of the screen.

3
00:00:12,240 --> 00:00:14,929
CC stands for ""closed captioning.""

4
00:00:14,929 --> 00:00:19,640
Just click the icon, and text will appear
in the video's original language.

5
00:00:19,980 --> 00:00:25,160
Second. Once the captions are turned on, you'll be
able to tell if they were added by the video's

6
00:00:25,160 --> 00:00:29,020
creator or generated automatically by YouTube AI.

7
00:00:29,020 --> 00:00:31,839
In the latter case, you can expect a few mistakes.

8
00:00:32,240 --> 00:00:33,080
And third.

9
00:00:33,080 --> 00:00:37,420
If you're looking for captions in Spanish
-- or another language besides English-- click

10
00:00:37,420 --> 00:00:40,840
on the gear icon to access the video settings menu.

11
00:00:40,840 --> 00:00:47,280
Click on ""Subtitles/CC"" and then ""Auto-translate""
to select the language of your choice from

12
00:00:47,280 --> 00:00:48,640
the menu provided.

13
00:00:49,460 --> 00:00:54,280
For more tips to fit your family, become a member
at commonsensemedia.org.

");
        }

        public void _Load(string data)
        {
            if (data == null)
            {
                count = 0;
                return;
            }

            string[] chunks = data.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log($"SRT chunks {chunks.Length}");

            count = chunks.Length;
            startTimes = new float[count];
            endTimes = new float[count];
            captions = new string[count];

            index = 0;
            for (int i = 0; i < count; i++)
            {
                string[] parts = chunks[i].Split(new string[] { "\r\n", "\n" }, 3, StringSplitOptions.None);
                string[] timeParts = parts[1].Split(new string[] { " --> " }, StringSplitOptions.None);

                TimeSpan start;
                if (TimeSpan.TryParseExact(timeParts[0], @"hh\:mm\:ss\,fff", null, TimeSpanStyles.None, out start))
                    startTimes[index] = (float)start.TotalSeconds;
                else
                    continue;
                
                if (timeParts.Length == 2)
                {
                    TimeSpan end;
                    if (TimeSpan.TryParseExact(timeParts[1], @"hh\:mm\:ss\,fff", null, TimeSpanStyles.None, out end))
                        endTimes[index] = (float)end.TotalSeconds;
                    else
                        continue;
                }
                else
                    endTimes[index] = startTimes[index] + maxDuration;
                
                if (parts.Length == 3)
                    captions[index] = parts[2];
                else
                    continue;

                index += 1;
            }

            count = index;
            index = 0;

            if (videoPlayer && videoPlayer.playerState == TXLVideoPlayer.VIDEO_STATE_PLAYING)
                _StartPlayback();
        }

        public void _Load(VRCUrl url)
        {
            if (url == null || url.Get() == string.Empty)
            {
                count = 0;
                return;
            }

            VRCStringDownloader.LoadUrl(url, (UdonBehaviour)(Component)this);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.Log(result.Error);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            Debug.Log($"Received data {result.Result.Length} characters");

            _Load(result.Result);
        }

        public void _OnVideoStateUpdate()
        {
            Debug.Log($"SRT video state update {videoPlayer.playerState}");
            switch (videoPlayer.playerState)
            {
                case TXLVideoPlayer.VIDEO_STATE_PLAYING:
                    _StartPlayback();
                    break;
                default:
                    _StopPlayback();
                    break;
            }
        }

        public void _OnVideoTrackingUpdate()
        {
            position = videoPlayer.trackPosition;
            positionSyncTime = Time.time;
            positionSyncOffset = positionSyncTime - position;

            _UpdateIndex();
            _UpdateDisplay();
        }

        void _UpdateIndex()
        {
            if (count == 0)
                return;

            // Search forward
            if (index < count)
            {
                float indexEnd = endTimes[index];
                while (position >= indexEnd)
                {
                    if (index >= count)
                        break;

                    index += 1;
                    indexEnd = endTimes[index];
                }
            }

            // Search backward
            if (index > 0)
            {
                if (index == count && position < startTimes[index - 1])
                    index -= 1;

                float indexStart = startTimes[index];
                while (position < indexStart)
                {
                    if (index == 0)
                        break;

                    index -= 1;
                    indexStart = startTimes[index];
                }
            }
        }

        void _UpdateDisplay()
        {
            if (count == 0 || position < startTimes[index] || position >= endTimes[index])
            {
                captionText.enabled = false;
                return;
            }

            captionText.enabled = true;
            captionText.text = captions[index];
        }

        void _StopPlayback()
        {
            active = false;

            captionText.enabled = false;
            captionText.text = "";
        }

        void _StartPlayback()
        {
            Debug.Log($"SRT start playback {active} {count}");
            if (active)
                return;

            if (count > 0)
            {
                active = true;
                _PlaybackLoop();
            }
        }

        public void _PlaybackLoop()
        {
            if (!active)
                return;

            position = Time.time - positionSyncOffset;

            _UpdateIndex();
            _UpdateDisplay();

            SendCustomEventDelayedSeconds(nameof(_PlaybackLoop), 0.1f);
        }
    }
}
