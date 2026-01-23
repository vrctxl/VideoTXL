/*
MIT License

Copyright (c) 2020 Merlin
Modifications Copyright (c) 2022 Texel

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDKBase;

namespace Texel.Video.Internal
{
    public class VideoMeta
    {
        public string id;
        public Double duration;
    }

    /// <summary>
    /// Allows people to put in links to YouTube videos and other supported video services and have links just work
    /// Hooks into VRC's video player URL resolve callback and uses the VRC installation of YouTubeDL to resolve URLs in the editor.
    /// </summary>
    public static class EditorUrlResolver
    {
        private static string _youtubeDLPath = "";
        private static string _ffmpegPath = "";
        private static string _ytdlResolvedURL = "";
        private static VideoMeta _ytdlJson;
        private static string _ffmpegError;
        private const string _ffmpegCache = "Video Cache";
        private const string _ffErrorIdentifier = ", from 'http";

#if UNITY_EDITOR_WIN
        private const int _ytdlArgsCount = 7;
#else
        private const int _ytdlArgsCount = 8;
#endif
        private static System.Diagnostics.Process _ffmpegProcess;
        private static HashSet<System.Diagnostics.Process> _runningYtdlProcesses = new HashSet<System.Diagnostics.Process>();
        private static HashSet<MonoBehaviour> _registeredBehaviours = new HashSet<MonoBehaviour>();

        private static System.Diagnostics.Process ResolvingProcess(string resolverPath, string[] args)
        {
            System.Diagnostics.Process resolver = new System.Diagnostics.Process();

            resolver.EnableRaisingEvents = true;

            resolver.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            resolver.StartInfo.CreateNoWindow = true;
            resolver.StartInfo.UseShellExecute = false;
            resolver.StartInfo.RedirectStandardInput = true;
            resolver.StartInfo.RedirectStandardOutput = true;
            resolver.StartInfo.RedirectStandardError = true;

            resolver.StartInfo.FileName = resolverPath;

            foreach (string argument in args)
                resolver.StartInfo.Arguments += argument + " ";

            return resolver;
        }

        private static string SanitizeURL(string url, string identifier, char seperator)
        {
            if (url.StartsWith(identifier) && url.Contains(seperator))
                url = url.Substring(0, url.IndexOf(seperator));

            return url;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SetupURLResolveCallback()
        {
            // If another script has hooked StartResolveURLCoroutine (like the original USharp shim), defer to it
            if (VRCUnityVideoPlayer.StartResolveURLCoroutine != null)
                return;

            string[] splitPath = Application.persistentDataPath.Split('/', '\\');
            _youtubeDLPath = string.Join("\\", splitPath.Take(splitPath.Length - 2)) + "\\VRChat\\VRChat\\Tools\\yt-dlp.exe";

            if (!File.Exists(_youtubeDLPath))
            {
                _youtubeDLPath = string.Join("\\", splitPath.Take(splitPath.Length - 2)) + "\\VRChat\\VRChat\\Tools\\youtube-dl.exe";
            }

#if UNITY_EDITOR_LINUX
            if (!File.Exists(_youtubeDLPath))
                _youtubeDLPath = "/usr/bin/yt-dlp";

            _ffmpegPath = "/usr/bin/ffmpeg";
            if (!File.Exists(_ffmpegPath))
                Debug.LogWarning("[<color=#A7D147>VideoTXL FFMPEG</color>] Unable to find FFmpeg installation, URLs will not be transcoded in editor test your videos in game.");
#endif

            if (!File.Exists(_youtubeDLPath))
            {
                Debug.LogWarning("[<color=#A7D147>VideoTXL YTDL</color>] Unable to find VRC YouTube-DL or YT-DLP installation, URLs will not be resolved in editor test your videos in game.");
                return;
            }

            VRCUnityVideoPlayer.StartResolveURLCoroutine = ResolveURLCallback;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        /// <summary>
        /// Cleans up any remaining YTDL processes from this play.
        /// In some cases VRC's YTDL has hung indefinitely eating CPU so this is a precaution against that potentially happening.
        /// </summary>
        /// <param name="change"></param>
        private static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                foreach (var process in _runningYtdlProcesses)
                {
                    if (!process.HasExited)
                    {
                        //Debug.Log("Closing YTDL process");
                        process.Close();
                    }
                }

                _runningYtdlProcesses.Clear();

                // Apparently the URLResolveCoroutine will run after this method in some cases magically. So don't because the process will throw an exception.
                foreach (MonoBehaviour behaviour in _registeredBehaviours)
                    behaviour.StopAllCoroutines();

                _registeredBehaviours.Clear();
            }
        }

        private static void ResolveURLCallback(VRCUrl url, int resolution, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            // Broken for some unknown reason, when multiple rate limits fire off, only fires the first callback.
            //if ((System.DateTime.UtcNow - lastRequestTime).TotalSeconds < 5.0)
            //{
            //    Debug.LogWarning("Rate limited " + videoPlayer, videoPlayer);
            //    errorCallback(VideoError.RateLimited);
            //    return;
            //}

            // Catch playlist runaway
            string urls = SanitizeURL(url.ToString(), "https://www.youtube.com/", '&');
            urls = SanitizeURL(urls, "https://youtu.be/", '?');

            if (_ffmpegProcess != null)
            {
                _ffmpegProcess.StandardInput.Write('q');
                _ffmpegProcess.StandardInput.Flush();
            }

            string[] ytdlpArgs = new string[_ytdlArgsCount] {
                "--no-check-certificate",
                "--no-cache-dir",
                "--rm-cache-dir",
#if !UNITY_EDITOR_WIN
                "--dump-json",
#endif

                "-f", $"\"mp4[height<=?{resolution}][protocol^=http]/best[height<=?{resolution}][protocol^=http]\"",

                "--get-url", $"\"{urls}\""
            };

            System.Diagnostics.Process ytdlProcess = ResolvingProcess(_youtubeDLPath, ytdlpArgs);

            Debug.Log($"[<color=#A7D147>VideoTXL YTDL</color>] Attempting to resolve URL '{urls}'");

            ytdlProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    if (args.Data.StartsWith("{"))
                    {
#if UNITY_EDITOR_WIN
                        _ytdlJson = new VideoMeta();
                        _ytdlJson.id = urls;
                        _ytdlJson.duration = 0;
#else
                        _ytdlJson = JsonUtility.FromJson<VideoMeta>(args.Data);
#endif
                    }
                    else
                    {
                        _ytdlResolvedURL = args.Data;
                    }
                }
            };

            ytdlProcess.Start();
            ytdlProcess.BeginOutputReadLine();

            _runningYtdlProcesses.Add(ytdlProcess);

            ((MonoBehaviour)videoPlayer).StartCoroutine(URLResolveCoroutine(urls, ytdlProcess, videoPlayer, urlResolvedCallback, errorCallback));

            _registeredBehaviours.Add((MonoBehaviour)videoPlayer);
        }

        private static IEnumerator URLTranscodeCoroutine(string resolvedURL, string outputURL, string originalUrl, System.Diagnostics.Process ffmpegProcess, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            while (!ffmpegProcess.HasExited)
                yield return new WaitForSeconds(0.1f);

            if (File.Exists(outputURL))
            {
                Debug.Log($"[<color=#A7D147>VideoTXL FFMPEG</color>] Successfully transcoded URL '{originalUrl}'");

#if UNITY_EDITOR_WIN
                urlResolvedCallback($"file:\\\\{outputURL}");
#else
                urlResolvedCallback($"file://{outputURL}");
#endif
            }
            else
            {
                Debug.LogWarning($"[<color=#A7D147>VideoTXL FFMPEG</color>] Unable to transcode URL, '{originalUrl}' will not be played in editor test your videos in game.\n{_ffmpegError}");

                errorCallback(VideoError.InvalidURL);
            }

            _ffmpegProcess.Dispose();
            _ffmpegProcess = null;
        }

        private static IEnumerator URLResolveCoroutine(string originalUrl, System.Diagnostics.Process ytdlProcess, UnityEngine.Object videoPlayer, Action<string> urlResolvedCallback, Action<VideoError> errorCallback)
        {
            while (!ytdlProcess.HasExited)
                yield return new WaitForSeconds(0.1f);

            _runningYtdlProcesses.Remove(ytdlProcess);

            string resolvedURL = _ytdlResolvedURL;

            // If a URL fails to resolve, YTDL will send error to stderror and nothing will be output to stdout
            if (string.IsNullOrEmpty(resolvedURL))
                errorCallback(VideoError.InvalidURL);
            else
            {
                string debugStdout = resolvedURL;
                if (resolvedURL.Contains("ip="))
                {
                    int filterStart = resolvedURL.IndexOf("ip=");
                    int filterEnd = resolvedURL.Substring(filterStart).IndexOf("&");

                    debugStdout = resolvedURL.Replace(resolvedURL.Substring(filterStart + 3, filterEnd - 3), "[REDACTED]");
                }
                Debug.Log($"[<color=#A7D147>VideoTXL YTDL</color>] Successfully resolved URL '{originalUrl}' to '{debugStdout}'");

#if !UNITY_EDITOR_LINUX
                urlResolvedCallback(resolvedURL);
#else

                if (File.Exists(_ffmpegPath))
                {
                    string tempPath = Path.GetFullPath(Path.Combine("Temp", _ffmpegCache));

                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    string urlHash = Hash128.Compute(originalUrl).ToString();
                    string fullUrlHash = Path.Combine(tempPath, urlHash + ".webm");

                    if (File.Exists(fullUrlHash))
                    {
                        Debug.Log($"[<color=#A7D147>VideoTXL FFMPEG</color>] Loaded cached video '{originalUrl}'");
                        urlResolvedCallback(fullUrlHash);
                    }
                    else
                    {
                        string[] ffmpegArgs = new string[13] {
                            "-hide_banner",

                            "-y",

                            "-hwaccel auto",

                            "-i", $"\"{resolvedURL}\"",

                            "-c:a", $"{ "libvorbis" }",

                            "-c:v", $"{ "vp8" }",

                            "vp8" == "vp8" ? "-cpu-used 6 -deadline realtime -qmin 0 -qmax 50 -crf 5 -minrate 1M -maxrate 1M -b:v 1M" : "",

                            "-f", $"{ "webm" }",

                            $"\"{fullUrlHash}\""
                        };

                        _ffmpegError = "";

                        _ffmpegProcess = ResolvingProcess(_ffmpegPath, ffmpegArgs);

                        _ffmpegProcess.ErrorDataReceived += (sender, args) =>
                        {
                            if (args.Data != null)
                            {
                                if (args.Data == "Press [q] to stop, [?] for help")
                                    Debug.Log($"[<color=#A7D147>VideoTXL FFMPEG</color>] Starting transcode '{originalUrl}'");
                                else if (args.Data.StartsWith("frame="))
                                {
                                    string progressTimeString = args.Data;
                                    int progressTimeIndex = progressTimeString.IndexOf("time=") + 5;
                                    int progressTimeLength = progressTimeString.IndexOf("bitrate=") - progressTimeIndex;

                                    string progressTime = progressTimeString.Substring(progressTimeIndex, progressTimeLength);
                                    TimeSpan ffmpegProgress = TimeSpan.Parse(progressTime);

                                    string progressSeconds = ffmpegProgress.ToString();
                                    progressSeconds = progressSeconds.Contains('.') ? progressSeconds.Substring(0, progressSeconds.IndexOf('.')) : progressSeconds;
                                    progressSeconds += "s";
                                    string progressPercent = _ytdlJson.duration == 0.0 ? "" : $"- {Mathf.FloorToInt((float)(ffmpegProgress.TotalSeconds / _ytdlJson.duration) * 100f)}%";

                                    Debug.Log($"[<color=#A7D147>VideoTXL FFMPEG</color>] Transcode progress '{_ytdlJson.id}': {progressSeconds} {progressPercent}");
                                }
                                else
                                {
                                    if (args.Data.Contains(_ffErrorIdentifier))
                                    {
                                        _ffmpegError += args.Data.Substring(0, args.Data.IndexOf(_ffErrorIdentifier)) + "\n";
                                    }
                                    else
                                    {
                                        _ffmpegError += args.Data + "\n";
                                    }
                                }
                            }
                        };

                        _ffmpegProcess.Start();
                        _ffmpegProcess.BeginErrorReadLine();

                        ((MonoBehaviour)videoPlayer).StartCoroutine(URLTranscodeCoroutine(resolvedURL, fullUrlHash, originalUrl, _ffmpegProcess, videoPlayer, urlResolvedCallback, errorCallback));
                    }
                }
                else errorCallback(VideoError.Unknown);
#endif
            }
        }
    }
}