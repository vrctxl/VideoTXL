
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/UI/Basic Player Controls")]
    public class BasicPlayerControls : UdonSharpBehaviour
    {
        public BasicSyncPlayer videoPlayer;

        public VRCUrlInputField urlInput;
        public GameObject urlInputControl;
        public GameObject progressSliderControl;

        public GameObject stopButton;
        public GameObject stopButtonDisabled;
        public GameObject lockButtonOpen;
        public GameObject lockButtonClosed;
        public GameObject lockButtonDenied;

        public Slider progressSlider;
        public Text statusText;
        public Text urlText;
        public Text placeholderText;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PAUSED = 2;
        const int PLAYER_STATE_PLAYING = 3;
        const int PLAYER_STATE_ERROR = 4;

        string statusOverride = null;

        public void _HandleUrlInput()
        {
            if (Utilities.IsValid(videoPlayer))
                videoPlayer._ChangeUrl(urlInput.GetUrl());
            urlInput.SetUrl(VRCUrl.Empty);
        }

        public void _HandleStop()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerStop();
            else
                _SetStatusOverride("Locked by instance owner or master", 3);
        }

        public void _HandleLock()
        {
            if (!Utilities.IsValid(videoPlayer))
                return;

            if (videoPlayer._CanTakeControl())
                videoPlayer._TriggerLock();
            else
                _SetStatusOverride("Locked by instance owner or master", 3);
        }

        bool _draggingProgressSlider = false;

        public void _HandleProgressBeginDrag()
        {
            Debug.Log("[VideoTXL] Drag Start");
            _draggingProgressSlider = true;
        }

        public void _HandleProgressEndDrag()
        {
            Debug.Log("[VideoTXL] Drag Stop");
            _draggingProgressSlider = false;
        }

        public void _HandleProgressSliderChanged()
        {
            if (!_draggingProgressSlider)
                return;

            if (float.IsInfinity(videoPlayer.trackDuration) || videoPlayer.trackDuration <= 0)
                return;

            float targetTime = videoPlayer.trackDuration * progressSlider.value;
            videoPlayer._SetTargetTime(targetTime);
            Debug.Log("[VideoTXL] Drag Change");
        }

        void _SetStatusOverride(string msg, float timeout)
        {
            statusOverride = msg;
            SendCustomEventDelayedSeconds("_ClearStatusOverride", timeout);
        }

        public void _ClearStatusOverride()
        {
            statusOverride = null;
        }

        private void Update()
        {
            bool canControl = videoPlayer._CanTakeControl();

            if (videoPlayer.localPlayerState == PLAYER_STATE_PLAYING)
            {
                urlInput.readOnly = true;
                urlInputControl.SetActive(false);
                stopButton.SetActive(true);
                stopButtonDisabled.SetActive(false);

                if (!videoPlayer.seekableSource)
                {
                    SetStatusText("Streaming...");
                    progressSliderControl.SetActive(false);


                }
                else if (_draggingProgressSlider)
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration * progressSlider.value).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    progressSliderControl.SetActive(true);
                }
                else
                {
                    string durationStr = System.TimeSpan.FromSeconds(videoPlayer.trackDuration).ToString(@"hh\:mm\:ss");
                    string positionStr = System.TimeSpan.FromSeconds(videoPlayer.trackPosition).ToString(@"hh\:mm\:ss");
                    SetStatusText(positionStr + "/" + durationStr);
                    progressSliderControl.SetActive(true);
                    progressSlider.value = Mathf.Clamp01(videoPlayer.trackPosition / videoPlayer.trackDuration);
                }
                progressSlider.interactable = canControl;
            }
            else
            {
                urlInput.readOnly = false;
                urlInputControl.SetActive(true);
                stopButton.SetActive(false);
                stopButtonDisabled.SetActive(true);

                SetStatusText("");
                progressSliderControl.SetActive(false);

                if (videoPlayer.localPlayerState == PLAYER_STATE_LOADING)
                {
                    placeholderText.text = "Loading...";
                    urlInput.readOnly = true;
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_ERROR)
                {
                    switch (videoPlayer.localLastErrorCode)
                    {
                        case VideoError.RateLimited:
                            placeholderText.text = "Rate limited, wait and try again";
                            break;
                        case VideoError.PlayerError:
                            placeholderText.text = "Video player error";
                            break;
                        case VideoError.InvalidURL:
                            placeholderText.text = "Invalid URL or source offline";
                            break;
                        case VideoError.AccessDenied:
                            placeholderText.text = "Video blocked, enable untrusted URLs";
                            break;
                        case VideoError.Unknown:
                        default:
                            placeholderText.text = "Failed to load video";
                            break;
                    }

                    urlInput.readOnly = false;
                }
                else if (videoPlayer.localPlayerState == PLAYER_STATE_STOPPED)
                {
                    placeholderText.text = "Enter Video URL...";
                    urlInput.readOnly = false;
                }
            }

            lockButtonClosed.SetActive(videoPlayer.locked && canControl);
            lockButtonDenied.SetActive(videoPlayer.locked && !canControl);
            lockButtonOpen.SetActive(!videoPlayer.locked);
        }

        void SetStatusText(string msg)
        {
            if (statusOverride != null)
                statusText.text = statusOverride;
            else
                statusText.text = msg;
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(BasicPlayerControls))]
    internal class BasicPlayerControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        SerializedProperty videoPlayerProperty;

        SerializedProperty urlInputProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;
        SerializedProperty stopButtonProperty;
        SerializedProperty stopButtonDisabledProperty;
        SerializedProperty lockButtonOpenProperty;
        SerializedProperty lockButtonClosedProperty;
        SerializedProperty lockButtonDeniedProperty;

        SerializedProperty progressSliderProperty;
        SerializedProperty statusTextProperty;
        SerializedProperty urlTextProperty;
        SerializedProperty placeholderTextProperty;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.videoPlayer));
            urlInputProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlInput));

            progressSliderControlProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.progressSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlInputControl));
            stopButtonProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.stopButton));
            stopButtonDisabledProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.stopButtonDisabled));
            lockButtonOpenProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.lockButtonOpen));
            lockButtonClosedProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.lockButtonClosed));
            lockButtonDeniedProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.lockButtonDenied));

            statusTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.statusText));
            placeholderTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.placeholderText));
            urlTextProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.urlText));
            progressSliderProperty = serializedObject.FindProperty(nameof(BasicPlayerControls.progressSlider));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(stopButtonProperty);
                EditorGUILayout.PropertyField(stopButtonDisabledProperty);
                EditorGUILayout.PropertyField(lockButtonOpenProperty);
                EditorGUILayout.PropertyField(lockButtonClosedProperty);
                EditorGUILayout.PropertyField(lockButtonDeniedProperty);
                EditorGUILayout.PropertyField(progressSliderProperty);
                EditorGUILayout.PropertyField(statusTextProperty);
                EditorGUILayout.PropertyField(urlTextProperty);
                EditorGUILayout.PropertyField(placeholderTextProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
