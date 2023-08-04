using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Texel;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;

namespace Texel
{
    public class VideoSourcePreprocess : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            GameObject[] objects = scene.GetRootGameObjects();
            foreach (GameObject obj in objects)
                ProcessHierarchy(obj.transform);
        }

        public static void ProcessHierarchy(Transform root)
        {
            VideoSource[] sources = root.GetComponentsInChildren<VideoSource>(true);
            foreach (var source in sources)
            {
                if (!source.videoMux || !source.videoMux.videoPlayer)
                    return;
                if (!source.videoMux.videoPlayer.runBuildHooks)
                    return;

                CheckAndUpdateSource(source);
            }
        }

        public static void CheckAndUpdateSource(VideoSource source)
        {
            VRCAVProVideoPlayer avp = (VRCAVProVideoPlayer)source.GetComponent("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            if (avp != null)
            {
                UpdateLowLatency(source, avp.UseLowLatency);
                UpdateResolution(source, avp.MaximumResolution);
            }

            VRCUnityVideoPlayer unity = (VRCUnityVideoPlayer)source.GetComponent("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (unity != null)
            {
                try
                {
                    int maxRes = (int)typeof(VRCUnityVideoPlayer).GetField("maximumResolution", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(unity);
                    UpdateResolution(source, maxRes);
                }
                catch { }
            }
        }

        private static void UpdateResolution(VideoSource source, int value)
        {
            if (source.maxResolution != value)
            {
                Debug.Log($"Updated {source} maxResolution from {source.maxResolution} to {value}");
                source.maxResolution = value;
            }
        }

        private static void UpdateLowLatency(VideoSource source, bool value)
        {
            if (source.lowLatency != value)
            {
                Debug.Log($"Updated {source} lowLatency from {source.lowLatency} to {value}");
                source.lowLatency = value;
            }
        }
    }
}
