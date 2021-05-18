
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace VideoTXL
{
    [AddComponentMenu("VideoTXL/Component/Static URL Source")]
    public class StaticUrlSource : UdonSharpBehaviour
    {
        [Tooltip("If enabled, specify separate URLs for 720 and 1080 video sources")]
        public bool multipleResolutions;
        public int defaultResolution; 

        public VRCUrl staticUrl;
        public VRCUrl staticUrl720;
        public VRCUrl staticUrl1080;
        public VRCUrl staticUrlAudio;

        UdonBehaviour _videoPlayer;
        GameObject[] _controls;

        int _selectedResolution = 0;

        const int RESOLUTION_720 = 0;
        const int RESOLUTION_1080 = 1;
        const int RESOLUTION_AUDIO = 2;

        void Start()
        {
            _selectedResolution = defaultResolution;
            if (!Utilities.IsValid(_controls))
                _controls = new GameObject[0];
        }

        public void _RegisterPlayer(UdonBehaviour player)
        {
            _videoPlayer = player;

        }

        public void _RegisterControls(GameObject controls)
        {
            if (!Utilities.IsValid(controls))
                return;

            if (!Utilities.IsValid(_controls))
                _controls = new GameObject[0];

            foreach (GameObject c in _controls)
            {
                if (c == controls)
                    return;
            }

            GameObject[] newControls = new GameObject[_controls.Length + 1];
            for (int i = 0; i < _controls.Length; i++)
                newControls[i] = _controls[i];

            newControls[_controls.Length] = controls;
            _controls = newControls;

            Debug.Log("[VideoTXL:StaticUrlSource] registering new controls set");
        }

        public VRCUrl _GetUrl()
        {
            if (!multipleResolutions)
                return staticUrl;

            if (_selectedResolution == RESOLUTION_720)
                return staticUrl720;
            if (_selectedResolution == RESOLUTION_1080)
                return staticUrl1080;
            if (_selectedResolution == RESOLUTION_AUDIO)
                return staticUrlAudio;

            return VRCUrl.Empty;
        }

        public bool _IsQuality720()
        {
            return multipleResolutions && _selectedResolution == RESOLUTION_720;
        }

        public bool _IsQuality1080()
        {
            return multipleResolutions && _selectedResolution == RESOLUTION_1080;
        }

        public bool _IsQualityAudio()
        {
            return multipleResolutions && _selectedResolution == RESOLUTION_AUDIO;
        }

        public void _SetQuality720()
        {
            if (_selectedResolution != RESOLUTION_720)
            {
                _selectedResolution = RESOLUTION_720;
                if (Utilities.IsValid(_videoPlayer))
                    _videoPlayer.SendCustomEvent("_UrlChanged");
                UpdateControls();
            }
        }

        public void _SetQuality1080()
        {
            if (_selectedResolution != RESOLUTION_1080)
            {
                _selectedResolution = RESOLUTION_1080;
                if (Utilities.IsValid(_videoPlayer))
                    _videoPlayer.SendCustomEvent("_UrlChanged");
                UpdateControls();
            }
        }

        public void _SetQualityAudio()
        {
            if (_selectedResolution != RESOLUTION_AUDIO)
            {
                _selectedResolution = RESOLUTION_AUDIO;
                if (Utilities.IsValid(_videoPlayer))
                    _videoPlayer.SendCustomEvent("_UrlChanged");
                UpdateControls();
            }
        }

        private void UpdateControls()
        {
            foreach (var panel in _controls)
            {
                if (Utilities.IsValid(panel))
                {
                    UdonBehaviour script = (UdonBehaviour)panel.GetComponent(typeof(UdonBehaviour));
                    if (Utilities.IsValid(script))
                        script.SendCustomEvent("_UrlChanged");
                }
            }
        }
    }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(StaticUrlSource))]
    internal class StaticUrlSourceInspector : Editor
    {
        SerializedProperty multipleResolutionsProperty;
        SerializedProperty defaultResolutionProperty;

        SerializedProperty staticUrlProperty;
        SerializedProperty staticUrl720Property;
        SerializedProperty staticUrl1080Property;
        SerializedProperty staticUrlAudioProperty;

        private void OnEnable()
        {
            multipleResolutionsProperty = serializedObject.FindProperty(nameof(StaticUrlSource.multipleResolutions));
            defaultResolutionProperty = serializedObject.FindProperty(nameof(StaticUrlSource.defaultResolution));

            staticUrlProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl));
            staticUrl720Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl720));
            staticUrl1080Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl1080));
            staticUrlAudioProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrlAudio));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawConvertToUdonBehaviourButton(target) ||
                UdonSharpGUI.DrawProgramSource(target))
                return;

            EditorGUILayout.PropertyField(multipleResolutionsProperty);
            if (!multipleResolutionsProperty.boolValue)
            {
                EditorGUILayout.PropertyField(staticUrlProperty);
            } else
            {
                int defaultResolution = EditorGUILayout.Popup("Default Resolution", defaultResolutionProperty.intValue, new string[] { "720p", "1080p", "Audio" });
                defaultResolutionProperty.intValue = defaultResolution;

                EditorGUILayout.PropertyField(staticUrl720Property);
                EditorGUILayout.PropertyField(staticUrl1080Property);
                EditorGUILayout.PropertyField(staticUrlAudioProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}